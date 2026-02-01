<#
.SYNOPSIS
    One-click launcher - starts API and Cloudflare tunnel together.
.DESCRIPTION
    Builds the project, starts the API, creates a public tunnel, and configures React frontend.
.PARAMETER SkipBuild
    Skip building the project
.PARAMETER Port
    API port (default: 5065)
.PARAMETER CloudflaredPath
    Path to cloudflared.exe (default: C:\cloudflared\cloudflared.exe)
.EXAMPLE
    .\Start-DevEnvironment.ps1
    .\Start-DevEnvironment.ps1 -SkipBuild
#>

param(
    [switch]$SkipBuild,
    [int]$Port = 5065,
    [string]$CloudflaredPath = "C:\cloudflared\cloudflared.exe"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Resolve-Path "$ScriptDir\..\..").Path
$ApiProject = "$ProjectRoot\ASU Dorms Management System"
$ReactProject = "$ProjectRoot\asu-dorms-frontend"  # Adjust this to your React folder name
$ConfigPath = "$env:USERPROFILE\.cloudflared"
$LogPath = "$ConfigPath\logs"

# Ensure directories exist
if (-not (Test-Path $LogPath)) {
    New-Item -ItemType Directory -Path $LogPath -Force | Out-Null
}

Clear-Host
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Cyan
Write-Host "    ASU DORMS MANAGEMENT SYSTEM" -ForegroundColor Cyan
Write-Host "    Development Environment Launcher" -ForegroundColor Cyan
Write-Host "  ==========================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "[CHECK] .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  Found: .NET $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] .NET SDK not found!" -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    pause
    exit 1
}

# Check cloudflared - try multiple locations
Write-Host "[CHECK] Cloudflared..." -ForegroundColor Yellow
$cloudflaredLocations = @(
    $CloudflaredPath,
    "C:\cloudflared\cloudflared.exe",
    "$env:ProgramFiles\cloudflared\cloudflared.exe",
    "cloudflared"  # Check PATH
)

$foundCloudflared = $null
foreach ($location in $cloudflaredLocations) {
    try {
        if ($location -eq "cloudflared") {
            $testPath = (Get-Command cloudflared -ErrorAction Stop).Source
            $foundCloudflared = $testPath
            break
        }
        elseif (Test-Path $location) {
            $foundCloudflared = $location
            break
        }
    }
    catch { }
}

if (-not $foundCloudflared) {
    Write-Host "[ERROR] Cloudflared not found!" -ForegroundColor Red
    Write-Host "  Expected location: $CloudflaredPath" -ForegroundColor Yellow
    Write-Host "  Download from: https://github.com/cloudflare/cloudflared/releases" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "  Found: $foundCloudflared" -ForegroundColor Green

# Build project
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "[BUILD] Building project..." -ForegroundColor Yellow
    Push-Location $ApiProject
    try {
        $buildOutput = dotnet build --configuration Debug 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Build failed!" -ForegroundColor Red
            $buildOutput | Where-Object { $_ -match "error" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
            Pop-Location
            pause
            exit 1
        }
        Write-Host "  Build successful!" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

# Start API
Write-Host ""
Write-Host "[API] Starting on port $Port..." -ForegroundColor Yellow

$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--urls", "http://localhost:$Port", "--no-build" `
    -WorkingDirectory $ApiProject `
    -PassThru `
    -WindowStyle Minimized

# Wait for API with progress
Write-Host "  Waiting for API to start..." -ForegroundColor Gray
$apiReady = $false
$maxWaitSeconds = 60
for ($i = 0; $i -lt $maxWaitSeconds; $i++) {
    Start-Sleep -Seconds 1
    
    try {
        $test = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue -InformationLevel Quiet -ErrorAction SilentlyContinue
        if ($test) {
            $apiReady = $true
            Write-Host ""
            Write-Host "  API ready! (took $i seconds)" -ForegroundColor Green
            break
        }
    }
    catch { }
    
    if ($i % 5 -eq 0 -and $i -gt 0) {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $apiReady) {
    Write-Host ""
    Write-Host "  [WARNING] API didn't start within $maxWaitSeconds seconds" -ForegroundColor Yellow
    Write-Host "  Continuing anyway - check if API process started manually" -ForegroundColor Yellow
}

# Start tunnel
Write-Host ""
Write-Host "[TUNNEL] Starting Cloudflare tunnel..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "$LogPath\tunnel-$timestamp.log"

Write-Host "  Log file: $logFile" -ForegroundColor Gray

# Start cloudflared and redirect output to log file
$tunnelProcess = Start-Process -FilePath $foundCloudflared `
    -ArgumentList "tunnel", "--url", "http://localhost:$Port", "--logfile", "$logFile" `
    -PassThru `
    -WindowStyle Hidden `
    -RedirectStandardOutput "$LogPath\tunnel-$timestamp-stdout.txt" `
    -RedirectStandardError "$LogPath\tunnel-$timestamp-stderr.txt"

Write-Host "  Process ID: $($tunnelProcess.Id)" -ForegroundColor Gray
Write-Host "  Generating public URL (this may take 30-60 seconds)..." -ForegroundColor Gray

# Wait for tunnel URL - check multiple sources
$tunnelUrl = $null
$maxAttempts = 60
$attemptCount = 0

while ($attemptCount -lt $maxAttempts -and -not $tunnelUrl) {
    Start-Sleep -Seconds 2
    $attemptCount++
    
    # Try reading from log file
    if (Test-Path $logFile) {
        $logContent = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
        if ($logContent -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
            $tunnelUrl = $Matches[0]
            break
        }
    }
    
    # Try reading from stdout
    $stdoutFile = "$LogPath\tunnel-$timestamp-stdout.txt"
    if (Test-Path $stdoutFile) {
        $stdoutContent = Get-Content $stdoutFile -Raw -ErrorAction SilentlyContinue
        if ($stdoutContent -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
            $tunnelUrl = $Matches[0]
            break
        }
    }
    
    # Try reading from stderr
    $stderrFile = "$LogPath\tunnel-$timestamp-stderr.txt"
    if (Test-Path $stderrFile) {
        $stderrContent = Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue
        if ($stderrContent -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
            $tunnelUrl = $Matches[0]
            break
        }
    }
    
    if ($attemptCount % 5 -eq 0) {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

Write-Host ""

# Save tunnel info
$tunnelInfo = @{
    StartTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Url = if ($tunnelUrl) { $tunnelUrl } else { "URL_NOT_DETECTED" }
    Port = $Port
    ApiProcessId = $apiProcess.Id
    TunnelProcessId = $tunnelProcess.Id
    LogFile = $logFile
    CloudflaredPath = $foundCloudflared
}
$tunnelInfo | ConvertTo-Json | Out-File "$ConfigPath\active-tunnel.json" -Encoding UTF8

# Display results
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Green
Write-Host "    ENVIRONMENT READY!" -ForegroundColor Green
Write-Host "  ==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  LOCAL ACCESS:" -ForegroundColor Cyan
Write-Host "    API:     http://localhost:$Port" -ForegroundColor White
Write-Host "    Swagger: http://localhost:$Port/swagger" -ForegroundColor White
Write-Host ""

if ($tunnelUrl) {
    Write-Host "  PUBLIC ACCESS (Cloudflare):" -ForegroundColor Cyan
    Write-Host "    API:     $tunnelUrl" -ForegroundColor Green
    Write-Host "    Swagger: $tunnelUrl/swagger" -ForegroundColor Green
    Write-Host ""
    
    # Update React frontend configuration
    if (Test-Path $ReactProject) {
        Write-Host "[REACT] Configuring frontend..." -ForegroundColor Yellow
        
        # Create/Update .env.local
        $envLocalPath = Join-Path $ReactProject ".env.local"
        $envContent = @"
# ASU Dorms Management System - Auto-generated Configuration
# Generated by Start-DevEnvironment.ps1 on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

# Cloudflare Tunnel API URL
VITE_API_URL=$tunnelUrl
REACT_APP_API_URL=$tunnelUrl
"@
        Set-Content -Path $envLocalPath -Value $envContent -Encoding UTF8
        Write-Host "  Created: .env.local" -ForegroundColor Green
        Write-Host "  API URL: $tunnelUrl" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  To start React frontend:" -ForegroundColor Yellow
        Write-Host "    cd `"$ReactProject`"" -ForegroundColor White
        Write-Host "    npm run dev" -ForegroundColor White
    }
    else {
        Write-Host "  REACT CONFIG (.env.local):" -ForegroundColor Cyan
        Write-Host "    VITE_API_URL=$tunnelUrl" -ForegroundColor Yellow
        Write-Host "    REACT_APP_API_URL=$tunnelUrl" -ForegroundColor Yellow
    }
    
    # Update appsettings.json for CORS
    $settingsPath = "$ApiProject\appsettings.json"
    if (Test-Path $settingsPath) {
        try {
            $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
            if ($settings.Cloudflare) {
                $settings.Cloudflare.Enabled = $true
                $settings.Cloudflare.TunnelUrl = $tunnelUrl
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8
                Write-Host ""
                Write-Host "  [UPDATED] appsettings.json with tunnel URL" -ForegroundColor Green
                Write-Host "  [NOTE] Restart API to apply CORS changes" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  [WARNING] Could not update appsettings.json" -ForegroundColor Yellow
        }
    }
}
else {
    Write-Host "  PUBLIC ACCESS:" -ForegroundColor Cyan
    Write-Host "    URL NOT DETECTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Troubleshooting:" -ForegroundColor Yellow
    Write-Host "    1. Check logs: $logFile" -ForegroundColor White
    Write-Host "    2. Run: Get-Content '$logFile'" -ForegroundColor White
    Write-Host "    3. Verify cloudflared is running: Get-Process cloudflared" -ForegroundColor White
    Write-Host "    4. Check internet connectivity" -ForegroundColor White
}

Write-Host ""
Write-Host "  Process IDs:" -ForegroundColor Gray
Write-Host "    API:    $($apiProcess.Id)" -ForegroundColor Gray
Write-Host "    Tunnel: $($tunnelProcess.Id)" -ForegroundColor Gray
Write-Host ""
Write-Host "  Logs:" -ForegroundColor Gray
Write-Host "    Tunnel: $logFile" -ForegroundColor Gray
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Yellow
Write-Host "    Press Ctrl+C to stop all services" -ForegroundColor Yellow
Write-Host "  ==========================================" -ForegroundColor Yellow
Write-Host ""

# Monitor and keep alive
try {
    while ($true) {
        Start-Sleep -Seconds 5
        
        if ($apiProcess.HasExited) {
            Write-Host "[WARNING] API stopped! Exit code: $($apiProcess.ExitCode)" -ForegroundColor Red
        }
        
        if ($tunnelProcess.HasExited) {
            Write-Host "[WARNING] Tunnel stopped! Exit code: $($tunnelProcess.ExitCode)" -ForegroundColor Red
        }
        
        if ($apiProcess.HasExited -and $tunnelProcess.HasExited) {
            Write-Host "[ERROR] Both services stopped. Exiting..." -ForegroundColor Red
            break
        }
    }
}
finally {
    Write-Host ""
    Write-Host "[CLEANUP] Stopping services..." -ForegroundColor Yellow
    
    if (-not $tunnelProcess.HasExited) {
        Stop-Process -Id $tunnelProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Stopped tunnel (PID: $($tunnelProcess.Id))" -ForegroundColor Gray
    }
    
    if (-not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Stopped API (PID: $($apiProcess.Id))" -ForegroundColor Gray
    }
    
    Remove-Item "$ConfigPath\active-tunnel.json" -Force -ErrorAction SilentlyContinue
    Write-Host "[DONE] All services stopped." -ForegroundColor Green
    Write-Host ""
}