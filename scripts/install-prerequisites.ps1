param(
    [switch]$InstallGitHubCli,
    [switch]$Force,
    [switch]$SkipPlaywright
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Write-Warn($message) {
    Write-Host "[WARN] $message" -ForegroundColor Yellow
}

function Write-Ok($message) {
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Test-Command($name) {
    return $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

function Install-WingetPackage($id, $displayName, $commandName) {
    if ((-not $Force) -and (Test-Command $commandName)) {
        Write-Ok "$displayName already installed"
        return
    }

    if (-not (Test-Command "winget")) {
        throw "winget is required to install $displayName automatically. Install App Installer from Microsoft Store or install $displayName manually."
    }

    Write-Step "Installing $displayName"
    winget install --id $id --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
}

function Ensure-FileFromExample($targetPath, $examplePath) {
    if (Test-Path $targetPath) {
        Write-Ok "Found $(Split-Path $targetPath -Leaf)"
        return
    }

    if (-not (Test-Path $examplePath)) {
        Write-Warn "Missing example file: $examplePath"
        return
    }

    Copy-Item $examplePath $targetPath
    Write-Warn "Created $(Split-Path $targetPath -Leaf) from example. Fill in real values before using NIM provider."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repoRoot "src\Nim-Cli\Nim-Cli.csproj"
$tuiProject = Join-Path $repoRoot "src\NimTui.App\NimTui.App.csproj"
$cliConfig = Join-Path $repoRoot "src\Nim-Cli\appsettings.secret.json"
$cliConfigExample = Join-Path $repoRoot "src\Nim-Cli\appsettings.secret.example.json"
$tuiConfig = Join-Path $repoRoot "src\NimTui.App\appsettings.secret.json"
$tuiConfigExample = Join-Path $repoRoot "src\NimTui.App\appsettings.secret.example.json"

Write-Step "Checking required Windows tooling"

Install-WingetPackage "Microsoft.DotNet.SDK.10" ".NET 10 SDK" "dotnet"
Install-WingetPackage "Microsoft.PowerShell" "PowerShell 7" "pwsh"
Install-WingetPackage "Git.Git" "Git" "git"

if ($InstallGitHubCli) {
    Install-WingetPackage "GitHub.cli" "GitHub CLI" "gh"
}
elseif (Test-Command "gh") {
    Write-Ok "GitHub CLI already installed"
}
else {
    Write-Warn "GitHub CLI not installed. This is optional, but needed for PR and some GitHub workflows. Re-run with -InstallGitHubCli to install it."
}

Write-Step "Verifying tool versions"
& dotnet --version
& pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
& git --version
if (Test-Command "gh") {
    & gh --version
}

Write-Step "Preparing local secret config files"
Ensure-FileFromExample $cliConfig $cliConfigExample
Ensure-FileFromExample $tuiConfig $tuiConfigExample

Write-Step "Restoring .NET dependencies"
& dotnet restore (Join-Path $repoRoot "Nim-Cli.slnx")

if (-not $SkipPlaywright) {
    Write-Step "Installing Playwright browser runtime"
    & dotnet build $cliProject -c Debug
    & pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "src\Nim-Cli\bin\Debug\net10.0\playwright.ps1") install chromium
}
else {
    Write-Warn "Skipped Playwright installation"
}

Write-Step "Optional sanity build"
& dotnet build (Join-Path $repoRoot "Nim-Cli.slnx")

Write-Ok "Prerequisite setup complete"
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Fill in src/Nim-Cli/appsettings.secret.json with a real NIM API key if needed."
Write-Host "2. Run: dotnet test \"tests/NimCli.Core.Tests/NimCli.Core.Tests.csproj\""
Write-Host "3. Run: dotnet run --project \"src/Nim-Cli/Nim-Cli.csproj\" -- doctor"
