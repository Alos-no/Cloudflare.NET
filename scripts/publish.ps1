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
.USAGE
  # Run from the repository root
  ./scripts/publish.ps1
#>

# Stop script execution on any error
$ErrorActionPreference = "Stop"

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
Write-Host "Building packages in Release mode..."
dotnet pack --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host -ForegroundColor Red "Error: 'dotnet pack' failed. Please check the build output."
    exit 1
}

Write-Host -ForegroundColor Green "Packages built successfully."


# --- 4. Find, Filter, and Push Packages ---
Write-Host "Scanning for packages to publish..."
$nugetSource = "https://api.nuget.org/v3/index.json"
# Regex to parse 'PackageId.Version.nupkg'. It captures the package ID and the full SemVer version string.
$packageRegex = '^(?<PackageId>.+?)\.(?<Version>\d+\.\d+\.\d+.*)\.nupkg$'

# Find all .nupkg files in the release directories, excluding symbol packages.
$allPackages = Get-ChildItem -Path ".\src\*\bin\Release\*.nupkg" -Recurse -Exclude "*.snupkg"

if ($null -eq $allPackages) {
    Write-Host -ForegroundColor Red "Error: No .nupkg files found. Ensure 'dotnet pack' ran correctly."
    exit 1
}

# --- 4.1. Parse and group packages to find the latest version of each one ---
$latestPackages = $allPackages | ForEach-Object {
    if ($_.Name -match $packageRegex) {
        # Create a custom object with parsed data. Casting to [version] allows for correct sorting.
        [PSCustomObject]@{
            PackageId = $Matches.PackageId
            Version   = [version]$Matches.Version
            FullName  = $_.FullName
        }
    }
} | Group-Object -Property PackageId | ForEach-Object {
    # For each group of packages (e.g., all versions of Cloudflare.NET.Api),
    # sort them by version descending and select the first one (the latest).
    $_.Group | Sort-Object -Property Version -Descending | Select-Object -First 1
}

Write-Host "Found latest versions of $($latestPackages.Count) packages to process."

# --- 4.2. Loop through latest packages, check if they exist, and push if new ---
foreach ($package in $latestPackages) {
    $packageIdLower = $package.PackageId.ToLower()
    $versionString = $package.Version.ToString()
    
    # Use the NuGet V3 Registration endpoint to get a list of all published versions for a package.
    # This is the reliable, documented way to check for package existence.
    # Ref: https://learn.microsoft.com/en-us/nuget/api/registration-base-url-resource
    $checkUrl = "https://api.nuget.org/v3/registration5-semver1/$packageIdLower/index.json"
    
    Write-Host -ForegroundColor Cyan "Processing $($package.PackageId) version $versionString..."
    
    $isPublished = $false
    try {
        # Fetch the metadata for the package ID.
        $response = Invoke-RestMethod -Uri $checkUrl -Method Get
        
        # The response JSON contains pages of version lists. We need to iterate through them.
        # This handles packages with many versions.
        $publishedVersions = foreach ($page in $response.items) {
            # If the page has 'items', it's a catalog page with version details.
            if ($page.items) {
                foreach ($item in $page.items) {
                    $item.catalogEntry.version
                }
            } else {
                # Otherwise, the page itself might be the version entry.
                $page.catalogEntry.version
            }
        }

        if ($publishedVersions -contains $versionString) {
            $isPublished = $true
        }
    }
    catch [System.Net.WebException] {
        # A 404 Not Found error is the expected result if the package ID has never been published.
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::NotFound) {
            $isPublished = $false # The package does not exist, so it is not published.
        }
        else {
            # Any other web exception is a genuine error.
            Write-Host -ForegroundColor Red "An unexpected error occurred while checking the NuGet registry."
            throw
        }
    }

    if ($isPublished) {
        Write-Host -ForegroundColor Yellow "Skipping: Version $versionString of $($package.PackageId) is already published."
    }
    else {
        Write-Host "Pushing new version: $($package.FullName)"
        
        # Execute the push command
        dotnet nuget push $package.FullName --api-key $apiKey --source $nugetSource
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host -ForegroundColor Red "Error: Failed to push $($package.PackageId). Please check the output."
            # Exit on the first failure to prevent pushing a partial release.
            exit 1
        } else {
            Write-Host -ForegroundColor Green "Successfully pushed $($package.PackageId) $versionString."
        }
    }
}

Write-Host -ForegroundColor Green "All packages have been processed successfully."