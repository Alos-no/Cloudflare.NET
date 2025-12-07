<#
.SYNOPSIS
  Builds the latest version of each NuGet package and pushes only new versions to NuGet.org.
.DESCRIPTION
  This script automates the publishing process for the Cloudflare.NET solution.
  1. Sets the working directory to the repository root.
  2. Loads or interactively prompts for the NUGET_API_KEY from a local .env file.
  3. Rebuilds the packages using `dotnet pack` in the Release configuration.
  4. Scans the output directories for all generated .nupkg files.
  5. For each package ID (e.g., Cloudflare.NET.Api), it intelligently selects only the latest version found.
  6. Before pushing, it queries the official NuGet Registration API to see if that specific version already exists.
  7. If the version is new, it pushes the package; otherwise, it skips it.
.NOTES
  Supports multi-targeted packages (net8.0, net9.0, net10.0) and SemVer pre-release versions.
.USAGE
  # Run from the repository root or the scripts directory
  ./scripts/publish.ps1
#>

# Stop script execution on any error
$ErrorActionPreference = "Stop"


# --- Helper Function: Parse SemVer for Sorting ---
# This function creates a sortable representation of SemVer versions,
# handling pre-release tags correctly (pre-release < release).
function Get-SemVerSortKey {
    param([string]$VersionString)
    
    # Split version and pre-release parts
    $parts = $VersionString -split '-', 2
    $versionPart = $parts[0]
    $preReleasePart = if ($parts.Length -gt 1) { $parts[1] } else { $null }
    
    # Parse the version numbers
    $versionNumbers = $versionPart -split '\.'
    $major = [int]($versionNumbers[0])
    $minor = if ($versionNumbers.Length -gt 1) { [int]($versionNumbers[1]) } else { 0 }
    $patch = if ($versionNumbers.Length -gt 2) { [int]($versionNumbers[2]) } else { 0 }
    
    # For sorting: pre-release versions come before release versions
    # Use 'zzzz' for release (sorts after any pre-release string) and the actual pre-release string otherwise
    $preReleaseSort = if ($null -eq $preReleasePart) { "zzzzzzzzzz" } else { $preReleasePart.PadRight(20, '0') }
    
    # Return a sortable tuple-like object
    return [PSCustomObject]@{
        Major = $major
        Minor = $minor
        Patch = $patch
        PreRelease = $preReleaseSort
        Original = $VersionString
    }
}


# --- 1. Set Working Directory to Repository Root ---
# This ensures the script can be run from anywhere and paths will be correct.
try {
    # This command finds the root of the Git repository from the script's current location.
    $repoRoot = Git rev-parse --show-toplevel
    Set-Location -Path $repoRoot
    Write-Host "Working directory set to repository root: '$repoRoot'"
}
catch {
    Write-Host -ForegroundColor Red "Error: Could not determine the Git repository root. Please run this script from within the repository."
    exit 1
}


# --- 2. Load or Prompt for NuGet API Key ---
$envFile = ".\.env" # This path is now correctly relative to the repository root
$apiKey = $null

# Try to load the key if the .env file exists
if (Test-Path $envFile) {
    # Get the line containing the key, split it by '=', and take the value part.
    $apiKey = (Get-Content $envFile | Where-Object { $_ -match "^NUGET_API_KEY=" }) -split '=', 2 | Select-Object -Last 1
}

# If the key is still missing (file didn't exist, line was missing, or value was empty), start the interactive prompt.
while ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host -ForegroundColor Yellow "NuGet API Key not found in .env file."
    $apiKey = Read-Host -Prompt "Please enter your NuGet API Key (starts with 'oy')"
    
    if ([string]::IsNullOrWhiteSpace($apiKey)) {
        Write-Warning "API Key cannot be empty. Please try again."
        # The loop will continue
    } elseif (-not $apiKey.StartsWith("oy")) {
        Write-Warning "The key doesn't look like a valid NuGet API key (should start with 'oy'). Please check and re-enter."
        $apiKey = $null # Reset apiKey to null to force the loop to repeat
    } else {
        # The key is valid, so save it for next time.
        Write-Host "API Key received. Saving to '$envFile' for future use."
        Set-Content -Path $envFile -Value "NUGET_API_KEY=$apiKey"
        
        # As a safety measure, check if .gitignore is correctly configured
        $gitIgnorePath = ".\.gitignore"
        if ((Test-Path $gitIgnorePath) -and ((Get-Content $gitIgnorePath) -notcontains ".env")) {
            Write-Warning "Your .gitignore file does not seem to ignore the '.env' file. It's highly recommended to add '.env' to it!"
        }
        Write-Host -ForegroundColor Green "API Key saved successfully."
    }
}

Write-Host -ForegroundColor Green "Successfully loaded NuGet API Key."


# --- 3. Build the Packages ---
Write-Host ""
Write-Host "Building packages in Release mode..."
Write-Host "Target frameworks: net8.0, net9.0, net10.0"
Write-Host ""

dotnet pack --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host -ForegroundColor Red "Error: 'dotnet pack' failed. Please check the build output."
    exit 1
}

Write-Host -ForegroundColor Green "Packages built successfully."


# --- 4. Find, Filter, and Push Packages ---
Write-Host ""
Write-Host "Scanning for packages to publish..."

$nugetSource = "https://api.nuget.org/v3/index.json"

# Regex to parse 'PackageId.Version.nupkg'. 
# Captures the package ID and the full SemVer version string (including pre-release tags like -beta1, -rc.1).
$packageRegex = '^(?<PackageId>.+?)\.(?<Version>\d+\.\d+\.\d+(?:-[a-zA-Z0-9.-]+)?)\.nupkg$'

# Find all .nupkg files in the release directories, excluding symbol packages (.snupkg).
# Multi-targeted packages output a single .nupkg containing all target frameworks.
$allPackages = Get-ChildItem -Path ".\src\*\bin\Release\*.nupkg" -Recurse -Exclude "*.snupkg"

if ($null -eq $allPackages -or $allPackages.Count -eq 0) {
    Write-Host -ForegroundColor Red "Error: No .nupkg files found. Ensure 'dotnet pack' ran correctly."
    exit 1
}

Write-Host "Found $($allPackages.Count) package file(s) in output directories."


# --- 4.1. Parse and group packages to find the latest version of each one ---
$latestPackages = $allPackages | ForEach-Object {
    if ($_.Name -match $packageRegex) {
        $versionString = $Matches.Version
        $sortKey = Get-SemVerSortKey -VersionString $versionString
        
        # Create a custom object with parsed data
        [PSCustomObject]@{
            PackageId   = $Matches.PackageId
            Version     = $versionString
            SortKey     = $sortKey
            FullName    = $_.FullName
            FileName    = $_.Name
        }
    }
} | Group-Object -Property PackageId | ForEach-Object {
    # For each group of packages (e.g., all versions of Cloudflare.NET.Api),
    # sort them by SemVer and select the latest.
    $_.Group | Sort-Object -Property @{
        Expression = { $_.SortKey.Major }; Descending = $true
    }, @{
        Expression = { $_.SortKey.Minor }; Descending = $true
    }, @{
        Expression = { $_.SortKey.Patch }; Descending = $true
    }, @{
        Expression = { $_.SortKey.PreRelease }; Descending = $true
    } | Select-Object -First 1
}

Write-Host "Identified $($latestPackages.Count) unique package(s) to process:"
foreach ($pkg in $latestPackages) {
    Write-Host "  - $($pkg.PackageId) v$($pkg.Version)"
}
Write-Host ""


# --- 4.2. Loop through latest packages, check if they exist, and push if new ---
$successCount = 0
$skippedCount = 0
$failedCount = 0

foreach ($package in $latestPackages) {
    $packageIdLower = $package.PackageId.ToLower()
    $versionString = $package.Version
    
    # Use the NuGet V3 Registration endpoint to get a list of all published versions for a package.
    # This is the reliable, documented way to check for package existence.
    # Ref: https://learn.microsoft.com/en-us/nuget/api/registration-base-url-resource
    $checkUrl = "https://api.nuget.org/v3/registration5-semver1/$packageIdLower/index.json"
    
    Write-Host -ForegroundColor Cyan "Processing $($package.PackageId) v$versionString..."
    
    $isPublished = $false
    try {
        # Fetch the metadata for the package ID.
        $response = Invoke-RestMethod -Uri $checkUrl -Method Get -ErrorAction Stop
        
        # The response JSON contains pages of version lists. We need to iterate through them.
        # This handles packages with many versions.
        $publishedVersions = foreach ($page in $response.items) {
            # If the page has 'items', it's a catalog page with version details.
            if ($page.items) {
                foreach ($item in $page.items) {
                    $item.catalogEntry.version
                }
            } else {
                # Otherwise, we may need to fetch the page separately (for packages with many versions)
                if ($page.'@id') {
                    try {
                        $pageData = Invoke-RestMethod -Uri $page.'@id' -Method Get -ErrorAction SilentlyContinue
                        if ($pageData.items) {
                            foreach ($item in $pageData.items) {
                                $item.catalogEntry.version
                            }
                        }
                    } catch {
                        # Ignore errors fetching individual pages
                    }
                }
            }
        }

        if ($publishedVersions -contains $versionString) {
            $isPublished = $true
        }
    }
    catch {
        # Check if it's a 404 Not Found (package ID has never been published)
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode -eq 404) {
            $isPublished = $false # The package does not exist, so it is not published.
        }
        elseif ($_.Exception.Message -match "404") {
            $isPublished = $false # Alternative 404 detection for different PowerShell versions
        }
        else {
            # Any other error - log it but continue (assume not published to be safe)
            Write-Host -ForegroundColor Yellow "Warning: Could not verify if package exists on NuGet.org: $($_.Exception.Message)"
            Write-Host -ForegroundColor Yellow "Proceeding with push attempt..."
            $isPublished = $false
        }
    }

    if ($isPublished) {
        Write-Host -ForegroundColor Yellow "  Skipping: Version $versionString is already published on NuGet.org."
        $skippedCount++
    }
    else {
        Write-Host "  Pushing: $($package.FileName)"
        
        # Execute the push command
        dotnet nuget push $package.FullName --api-key $apiKey --source $nugetSource --skip-duplicate
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host -ForegroundColor Red "  Error: Failed to push $($package.PackageId)."
            $failedCount++
            # Continue with other packages instead of stopping entirely
        } else {
            Write-Host -ForegroundColor Green "  Successfully pushed $($package.PackageId) v$versionString."
            $successCount++
        }
    }
    
    Write-Host ""
}


# --- 5. Summary ---
Write-Host "=========================================="
Write-Host "              PUBLISH SUMMARY             "
Write-Host "=========================================="
Write-Host -ForegroundColor Green "  Pushed:  $successCount"
Write-Host -ForegroundColor Yellow "  Skipped: $skippedCount (already published)"
if ($failedCount -gt 0) {
    Write-Host -ForegroundColor Red "  Failed:  $failedCount"
}
Write-Host "=========================================="

if ($failedCount -gt 0) {
    Write-Host -ForegroundColor Red "Some packages failed to publish. Please check the output above."
    exit 1
}

Write-Host -ForegroundColor Green "All packages have been processed successfully."
