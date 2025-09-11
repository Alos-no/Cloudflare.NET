<#
.SYNOPSIS
  Builds the NuGet packages in Release mode and pushes them to NuGet.org.
.DESCRIPTION
  This script automates the publishing process for the Cloudflare.NET solution.
  1. Sets the working directory to the repository root.
  2. Checks for a NUGET_API_KEY in a local .env file.
  3. If the key is not found, it interactively prompts the user to enter it.
  4. Creates or updates the .env file with the provided key for future use.
  5. Rebuilds the packages using `dotnet pack` in the Release configuration.
  6. Pushes all found .nupkg files to NuGet.org.
.USAGE
  # Run from the repository root or any subfolder
  ./scripts/publish.ps1
#>

# Stop script execution on any error
$ErrorActionPreference = "Stop"

# --- 1. Set Working Directory to Repository Root ---
# $PSScriptRoot is the directory where the script itself is located.
$scriptDir = $PSScriptRoot
# The repository root is one level up from the 'scripts' directory.
$repoRoot = (Get-Item $scriptDir).Parent.FullName
# Change PowerShell's current location to the repository root. All subsequent relative paths will be correct.
Set-Location -Path $repoRoot
Write-Host "Working directory set to repository root: '$repoRoot'"


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
    $apiKey = Read-Host -Prompt "Please enter your NuGet API Key (starts with 'oy_')"
    
    if ([string]::IsNullOrWhiteSpace($apiKey)) {
        Write-Warning "API Key cannot be empty. Please try again."
        # The loop will continue
    } elseif (-not $apiKey.StartsWith("oy_")) {
        Write-Warning "The key doesn't look like a valid NuGet API key (should start with 'oy_'). Please check and re-enter."
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


# --- 4. Find and Push Packages ---
$nugetSource = "https://api.nuget.org/v3/index.json"
# Find all .nupkg files in the release directories, excluding symbol packages.
$packages = Get-ChildItem -Path ".\src\*\bin\Release\*.nupkg" -Recurse -Exclude "*.snupkg"

if ($null -eq $packages) {
    Write-Host -ForegroundColor Red "Error: No .nupkg files found. Ensure 'dotnet pack' ran correctly."
    exit 1
}

foreach ($package in $packages) {
    Write-Host -ForegroundColor Cyan "Pushing package: $($package.FullName)"
    
    # Execute the push command
    dotnet nuget push $package.FullName --api-key $apiKey --source $nugetSource
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host -ForegroundColor Red "Error: Failed to push $($package.Name). Please check the output."
        # Exit on the first failure to prevent pushing a partial release.
        exit 1
    } else {
        Write-Host -ForegroundColor Green "Successfully pushed $($package.Name)."
    }
}

Write-Host -ForegroundColor Green "All packages have been processed successfully."