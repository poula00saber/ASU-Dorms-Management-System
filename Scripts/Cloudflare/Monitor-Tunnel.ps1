<#
.SYNOPSIS
    Monitors tunnel health and auto-restarts if needed.
.PARAMETER AutoRestart
    Automatically restart tunnel on failure
.PARAMETER CheckInterval
    Seconds between checks (default: 30)
#>

param(
    [switch]$AutoRestart,
    [int]$CheckInterval = 30,
    [int]$Port = 5065
)

$ConfigPath = "$env:USERPROFILE\.cloudflared"
$cloudflaredPath = $null

try { $cloudflaredPath = (Get-Command cloudflared -ErrorAction Stop).Source }
catch { $cloudflaredPath = "$env:ProgramFiles\cloudflared\cloudflared.exe" }

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Tunnel Health Monitor" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Check Interval: $CheckInterval sec" -ForegroundColor Gray
Write-Host "  Auto-Restart:   $AutoRestart" -ForegroundColor Gray
Write-Host "  Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

$failCount = 0

try {
    while ($true) {
        $timestamp = Get-Date -Format "HH:mm:ss"
        
        # Check cloudflared process
        $tunnelRunning = (Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue) -ne $null
        
        # Check API
        $apiRunning = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue -InformationLevel Quiet
        
        $tunnelStatus = if ($tunnelRunning) { "OK" } else { "DOWN" }
        $tunnelColor = if ($tunnelRunning) { "Green" } else { "Red" }
        
        $apiStatus = if ($apiRunning) { "OK" } else { "DOWN" }
        $apiColor = if ($apiRunning) { "Green" } else { "Red" }
        
        Write-Host "[$timestamp] Tunnel: " -NoNewline -ForegroundColor Gray
        Write-Host $tunnelStatus -NoNewline -ForegroundColor $tunnelColor
        Write-Host " | API: " -NoNewline -ForegroundColor Gray
        Write-Host $apiStatus -ForegroundColor $apiColor
        
        if (-not $tunnelRunning -and $AutoRestart) {
            $failCount++
            if ($failCount -ge 3) {
                Write-Host "[RESTART] Restarting tunnel..." -ForegroundColor Yellow
                $logFile = "$ConfigPath\logs\tunnel-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
                Start-Process -FilePath $cloudflaredPath `
                    -ArgumentList "tunnel", "--url", "http://localhost:$Port", "--logfile", $logFile `
                    -WindowStyle Hidden
                $failCount = 0
                Start-Sleep -Seconds 10
            }
        }
        elseif ($tunnelRunning) {
            $failCount = 0
        }
        
        Start-Sleep -Seconds $CheckInterval
    }
}
finally {
    Write-Host ""
    Write-Host "Monitor stopped." -ForegroundColor Yellow
}
