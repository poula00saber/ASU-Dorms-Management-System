<#
.SYNOPSIS
    Starts a FREE quick tunnel for ASU Dorms API.
.DESCRIPTION
    Creates a temporary public URL without requiring a Cloudflare account.
    Perfect for development and testing.
.PARAMETER Port
    Port for the .NET API (default: 5065)
.EXAMPLE
    .\Start-QuickTunnel.ps1
    .\Start-QuickTunnel.ps1 -Port 5000
#>

param(
    [int]$Port = 5065
)

$ErrorActionPreference = "Stop"
$ConfigPath = "$env:USERPROFILE\.cloudflared"
$LogPath = "$ConfigPath\logs"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Ensure directories exist
if (-not (Test-Path $LogPath)) {
    New-Item -ItemType Directory -Path $LogPath -Force | Out-Null
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ASU Dorms - Quick Tunnel" -ForegroundColor Cyan
Write-Host "  FREE Public Access" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Find cloudflared
$cloudflaredPath = $null
try {
    $cloudflaredPath = (Get-Command cloudflared -ErrorAction Stop).Source
    Write-Host "[OK] cloudflared found" -ForegroundColor Green
}
catch {
    $directPath = "$env:ProgramFiles\cloudflared\cloudflared.exe"
    if (Test-Path $directPath) {
        $cloudflaredPath = $directPath
        Write-Host "[OK] cloudflared found at: $directPath" -ForegroundColor Green
    }
    else {
        Write-Host "[ERROR] cloudflared not installed!" -ForegroundColor Red
        Write-Host "Run: .\Install-CloudflareTunnel.ps1" -ForegroundColor Yellow
        exit 1
    }
}

# Check if API is running
Write-Host ""
Write-Host "[CHECK] Testing port $Port..." -ForegroundColor Yellow
$apiTest = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue -InformationLevel Quiet
if (-not $apiTest) {
    Write-Host "[WARNING] No service on port $Port" -ForegroundColor Yellow
    Write-Host "Start your API first: dotnet run --urls http://localhost:$Port" -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y' -and $continue -ne 'Y') { exit 0 }
}
else {
    Write-Host "[OK] Service detected on port $Port" -ForegroundColor Green
}

# Start tunnel
Write-Host ""
Write-Host "[STARTING] Creating tunnel..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "$LogPath\tunnel-$timestamp.log"

$process = Start-Process -FilePath $cloudflaredPath `
    -ArgumentList "tunnel", "--url", "http://localhost:$Port", "--logfile", $logFile `
    -PassThru -WindowStyle Hidden

Write-Host "[WAITING] Generating public URL..." -ForegroundColor Gray

# Wait for tunnel URL
$tunnelUrl = $null
for ($i = 0; $i -lt 25; $i++) {
    Start-Sleep -Seconds 2
    if (Test-Path $logFile) {
        $content = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
        if ($content -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
            $tunnelUrl = $Matches[0]
            break
        }
    }
    Write-Host "." -NoNewline -ForegroundColor Gray
}
Write-Host ""

# Display results
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  TUNNEL ACTIVE!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  LOCAL:" -ForegroundColor Cyan
Write-Host "    http://localhost:$Port" -ForegroundColor White
Write-Host "    http://localhost:$Port/swagger" -ForegroundColor White
Write-Host ""

if ($tunnelUrl) {
    Write-Host "  PUBLIC (Cloudflare):" -ForegroundColor Cyan
    Write-Host "    $tunnelUrl" -ForegroundColor Green
    Write-Host "    $tunnelUrl/swagger" -ForegroundColor Green
    Write-Host ""
    
    # Save tunnel info
    @{
        StartTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Url = $tunnelUrl
        Port = $Port
        ProcessId = $process.Id
        LogFile = $logFile
    } | ConvertTo-Json | Out-File "$ConfigPath\active-tunnel.json" -Encoding UTF8

    # Update appsettings.json
    $settingsPath = Join-Path $ScriptDir "..\..\ASU Dorms Management System\appsettings.json"
    if (Test-Path $settingsPath) {
        try {
            $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
            $settings.Cloudflare.Enabled = $true
            $settings.Cloudflare.TunnelUrl = $tunnelUrl
            $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8
            Write-Host "  [UPDATED] appsettings.json" -ForegroundColor Green
            Write-Host "  [NOTE] Restart API to apply CORS" -ForegroundColor Yellow
        }
        catch {
            Write-Host "  [WARNING] Could not update appsettings.json" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "  REACT CONFIG (.env.local):" -ForegroundColor Cyan
    Write-Host "    VITE_API_URL=$tunnelUrl" -ForegroundColor Yellow
    Write-Host "    REACT_APP_API_URL=$tunnelUrl" -ForegroundColor Yellow
}
else {
    Write-Host "  PUBLIC:" -ForegroundColor Cyan
    Write-Host "    URL not detected - check log" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Log: $logFile" -ForegroundColor Gray
Write-Host ""
Write-Host "==========================================" -ForegroundColor Yellow
Write-Host "  Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow
Write-Host ""

# Monitor
try {
    while (-not $process.HasExited) { Start-Sleep -Seconds 5 }
    Write-Host "[STOPPED] Tunnel ended unexpectedly" -ForegroundColor Red
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item "$ConfigPath\active-tunnel.json" -Force -ErrorAction SilentlyContinue
    Write-Host "[CLEANUP] Tunnel stopped." -ForegroundColor Yellow
}
