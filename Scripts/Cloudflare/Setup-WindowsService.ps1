#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Sets up Cloudflare tunnel as a Windows service for auto-start.
.PARAMETER TunnelName
    Name for the tunnel (default: asu-dorms-api)
.PARAMETER Port
    API port (default: 5065)
#>

param(
    [string]$TunnelName = "asu-dorms-api",
    [int]$Port = 5065
)

$ErrorActionPreference = "Stop"
$ConfigPath = "$env:USERPROFILE\.cloudflared"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Windows Service Setup" -ForegroundColor Cyan
Write-Host "  Cloudflare Tunnel" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Find cloudflared
$cloudflaredPath = $null
try { $cloudflaredPath = (Get-Command cloudflared -ErrorAction Stop).Source }
catch { $cloudflaredPath = "$env:ProgramFiles\cloudflared\cloudflared.exe" }

if (-not (Test-Path $cloudflaredPath)) {
    Write-Host "[ERROR] cloudflared not installed!" -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] This requires a FREE Cloudflare account" -ForegroundColor Yellow
Write-Host "[INFO] Sign up at: https://dash.cloudflare.com/sign-up" -ForegroundColor Yellow
Write-Host ""
$proceed = Read-Host "Have you created a Cloudflare account? (y/N)"
if ($proceed -ne 'y' -and $proceed -ne 'Y') {
    Write-Host "Please create an account first, then run this script again." -ForegroundColor Yellow
    exit 0
}

# Step 1: Login
Write-Host ""
Write-Host "[1/4] Authenticating with Cloudflare..." -ForegroundColor Green
Write-Host "  A browser will open - log in and authorize." -ForegroundColor Gray
& $cloudflaredPath tunnel login

# Step 2: Create tunnel
Write-Host ""
Write-Host "[2/4] Creating tunnel '$TunnelName'..." -ForegroundColor Green
$existing = & $cloudflaredPath tunnel list --output json 2>$null | ConvertFrom-Json | Where-Object { $_.name -eq $TunnelName }
if ($existing) {
    Write-Host "  Tunnel already exists (ID: $($existing.id))" -ForegroundColor Yellow
}
else {
    & $cloudflaredPath tunnel create $TunnelName
}

# Get tunnel ID
$tunnelInfo = & $cloudflaredPath tunnel list --output json | ConvertFrom-Json | Where-Object { $_.name -eq $TunnelName }
$tunnelId = $tunnelInfo.id

# Step 3: Create config
Write-Host ""
Write-Host "[3/4] Creating configuration..." -ForegroundColor Green

$credFile = Get-ChildItem "$ConfigPath" -Filter "*.json" | Where-Object { $_.Name -match "^[a-f0-9-]+\.json$" } | Select-Object -First 1

$configContent = @"
tunnel: $tunnelId
credentials-file: $($credFile.FullName)
ingress:
  - service: http://localhost:$Port
loglevel: info
logfile: $ConfigPath\logs\service.log
"@

$configFile = "$ConfigPath\config.yml"
$configContent | Out-File $configFile -Encoding UTF8 -Force
Write-Host "  Config: $configFile" -ForegroundColor Gray

# Step 4: Install service
Write-Host ""
Write-Host "[4/4] Installing Windows service..." -ForegroundColor Green

$existingService = Get-Service -Name "cloudflared" -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  Removing existing service..." -ForegroundColor Gray
    Stop-Service "cloudflared" -Force -ErrorAction SilentlyContinue
    & $cloudflaredPath service uninstall 2>$null
}

& $cloudflaredPath service install
Start-Service "cloudflared"

$service = Get-Service "cloudflared"
Write-Host "  Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Service Installed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Tunnel: $TunnelName" -ForegroundColor Cyan
Write-Host "  ID: $tunnelId" -ForegroundColor Gray
Write-Host ""
Write-Host "  Your tunnel URL:" -ForegroundColor Cyan
Write-Host "  https://$tunnelId.cfargotunnel.com" -ForegroundColor Green
Write-Host ""
Write-Host "  Or configure a custom hostname in Cloudflare Dashboard" -ForegroundColor Gray
Write-Host ""
Write-Host "  Commands:" -ForegroundColor Cyan
Write-Host "    Get-Service cloudflared" -ForegroundColor White
Write-Host "    Restart-Service cloudflared" -ForegroundColor White
Write-Host "    Stop-Service cloudflared" -ForegroundColor White
Write-Host ""
