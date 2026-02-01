<#
.SYNOPSIS
    Shows current Cloudflare tunnel status.
#>

$ConfigPath = "$env:USERPROFILE\.cloudflared"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Cloudflare Tunnel Status" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Installation
Write-Host "INSTALLATION:" -ForegroundColor Yellow
try {
    $cloudflaredPath = (Get-Command cloudflared -ErrorAction Stop).Source
    $version = cloudflared --version 2>&1
    Write-Host "  Status: INSTALLED" -ForegroundColor Green
    Write-Host "  Version: $version" -ForegroundColor Gray
}
catch {
    $directPath = "$env:ProgramFiles\cloudflared\cloudflared.exe"
    if (Test-Path $directPath) {
        $version = & $directPath --version 2>&1
        Write-Host "  Status: INSTALLED (not in PATH)" -ForegroundColor Yellow
        Write-Host "  Version: $version" -ForegroundColor Gray
    }
    else {
        Write-Host "  Status: NOT INSTALLED" -ForegroundColor Red
        Write-Host "  Run: .\Install-CloudflareTunnel.ps1" -ForegroundColor Yellow
    }
}

# Processes
Write-Host ""
Write-Host "PROCESSES:" -ForegroundColor Yellow
$processes = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host "  Running: $($processes.Count)" -ForegroundColor Green
    foreach ($proc in $processes) {
        $mem = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        Write-Host "    PID $($proc.Id): $mem MB" -ForegroundColor Gray
    }
}
else {
    Write-Host "  Running: None" -ForegroundColor Gray
}

# Active tunnel
Write-Host ""
Write-Host "ACTIVE TUNNEL:" -ForegroundColor Yellow
$tunnelFile = "$ConfigPath\active-tunnel.json"
if (Test-Path $tunnelFile) {
    $info = Get-Content $tunnelFile | ConvertFrom-Json
    Write-Host "  URL:     $($info.Url)" -ForegroundColor Green
    Write-Host "  Port:    $($info.Port)" -ForegroundColor Gray
    Write-Host "  Started: $($info.StartTime)" -ForegroundColor Gray
}
else {
    Write-Host "  No active tunnel" -ForegroundColor Gray
}

# Local services
Write-Host ""
Write-Host "LOCAL SERVICES:" -ForegroundColor Yellow
@(5065, 5000, 3000, 5173) | ForEach-Object {
    $port = $_
    $test = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
    $status = if ($test) { "LISTENING" } else { "Not listening" }
    $color = if ($test) { "Green" } else { "Gray" }
    $desc = switch ($port) {
        5065 { "API (default)" }
        5000 { "API (alt)" }
        3000 { "React (CRA)" }
        5173 { "React (Vite)" }
    }
    Write-Host "  Port $port ($desc): $status" -ForegroundColor $color
}

Write-Host ""
