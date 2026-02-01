#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs Cloudflare Tunnel (cloudflared) on Windows.
.DESCRIPTION
    Downloads and installs cloudflared for exposing the ASU Dorms API to the internet
    without port forwarding. Works behind CGNAT and firewalls.
.EXAMPLE
    .\Install-CloudflareTunnel.ps1
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\cloudflared",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$CloudflaredUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
$CloudflaredExe = Join-Path $InstallPath "cloudflared.exe"
$ConfigPath = "$env:USERPROFILE\.cloudflared"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Cloudflare Tunnel Installer" -ForegroundColor Cyan
Write-Host "  ASU Dorms Management System" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if already installed
if ((Test-Path $CloudflaredExe) -and -not $Force) {
    $version = & $CloudflaredExe --version 2>&1
    Write-Host "[INFO] cloudflared already installed" -ForegroundColor Yellow
    Write-Host "  Version: $version" -ForegroundColor Gray
    $response = Read-Host "Reinstall/Update? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Installation skipped." -ForegroundColor Gray
        exit 0
    }
}

# Step 1: Create directories
Write-Host "[1/4] Creating directories..." -ForegroundColor Green
@($InstallPath, $ConfigPath, "$ConfigPath\logs") | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
        Write-Host "  Created: $_" -ForegroundColor Gray
    }
}

# Step 2: Download cloudflared
Write-Host "[2/4] Downloading cloudflared..." -ForegroundColor Green
try {
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $CloudflaredUrl -OutFile $CloudflaredExe -UseBasicParsing
    Write-Host "  Downloaded successfully!" -ForegroundColor Gray
}
catch {
    Write-Host "[ERROR] Download failed: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Verify installation
Write-Host "[3/4] Verifying installation..." -ForegroundColor Green
try {
    $version = & $CloudflaredExe --version 2>&1
    Write-Host "  $version" -ForegroundColor Gray
}
catch {
    Write-Host "[ERROR] Verification failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Add to PATH
Write-Host "[4/4] Adding to system PATH..." -ForegroundColor Green
$currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($currentPath -notlike "*$InstallPath*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$InstallPath", "Machine")
    $env:Path = "$env:Path;$InstallPath"
    Write-Host "  Added to PATH" -ForegroundColor Gray
    Write-Host "  NOTE: Restart terminal for PATH changes in new sessions" -ForegroundColor Yellow
}
else {
    Write-Host "  Already in PATH" -ForegroundColor Gray
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Start your API: dotnet run" -ForegroundColor White
Write-Host "  2. Run: .\Start-QuickTunnel.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Or use one command:" -ForegroundColor Cyan
Write-Host "  .\Start-DevEnvironment.ps1" -ForegroundColor White
Write-Host ""
