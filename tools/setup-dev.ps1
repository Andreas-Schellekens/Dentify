<#
.SYNOPSIS
    One-time setup of the Dentify API on a new PC: database, and optionally the real Azure services.

.DESCRIPTION
    - Installs the dotnet-ef tool when it is missing.
    - Creates or updates the LocalDB database "Dentify" with all migrations and seed data.
    - With -UseAzure: stores the Azure settings in the API user-secrets. Keys are asked
      interactively (hidden input) and never written to a file or to git.
    Without -UseAzure the API uses the fake Azure services, which is enough to build the app.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1 -UseAzure
#>
param(
    [switch]$UseAzure
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "Backend\Dentify.Backend\Dentify.Api"
$infrastructureProject = Join-Path $repoRoot "Backend\Dentify.Backend\Dentify.Infrastructure"

function Invoke-Checked([string]$description, [scriptblock]$command) {
    Write-Host "`n> $description" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Failed: $description" }
}

function Read-Secret([string]$prompt) {
    $secure = Read-Host -Prompt $prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer).Trim() }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

function Set-Secret([string]$name, [string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) {
        Write-Host "  skipped $name (empty)" -ForegroundColor Yellow
        return
    }
    dotnet user-secrets set $name $value --project $apiProject | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set $name" }
    Write-Host "  set $name"
}

# 1. Tools
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK is not installed. Install .NET 10 first."
}
dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) {
    Invoke-Checked "Installing dotnet-ef" { dotnet tool install --global dotnet-ef }
}

# 2. Database
Invoke-Checked "Creating/updating the Dentify database (LocalDB)" {
    dotnet ef database update --project $infrastructureProject --startup-project $apiProject
}

# 3. Azure settings (optional)
if ($UseAzure) {
    Write-Host "`n> Azure settings (ask Andreas for the keys, input is hidden)" -ForegroundColor Cyan

    # Not secret: endpoints, ids and names
    Set-Secret "Azure:UseFakeServices" "false"
    Set-Secret "Azure:Vision:Endpoint" "https://dentify-vision-as.cognitiveservices.azure.com/"
    Set-Secret "Azure:CustomVision:PredictionEndpoint" "https://asbigfive-prediction.cognitiveservices.azure.com/"
    Set-Secret "Azure:CustomVision:ProjectId" "a773d3ef-b91a-4acb-b895-7745b48ebf39"
    Set-Secret "Azure:CustomVision:PublishedName" "Iteration1"
    Set-Secret "Azure:CustomVision:ProjectType" "ObjectDetection"

    # Secret: typed by the user
    Set-Secret "Azure:Vision:Key" (Read-Secret "Azure AI Vision key (Enter to skip)")
    Set-Secret "Azure:CustomVision:PredictionKey" (Read-Secret "Custom Vision prediction key (Enter to skip)")
    Set-Secret "AutoMapper:LicenseKey" (Read-Secret "AutoMapper license key (Enter to skip)")
}
else {
    Write-Host "`nNo -UseAzure: the API will use the fake Azure services." -ForegroundColor Yellow
}

Write-Host "`nDone. Start the API with:" -ForegroundColor Green
Write-Host "  dotnet run --project `"$apiProject`" --launch-profile http"
Write-Host "Swagger: http://localhost:5152/swagger"
