<#
.SYNOPSIS
    Stops all running Cloudflare tunnels.
#>

Write-Host ""
Write-Host "Stopping Cloudflare Tunnels..." -ForegroundColor Yellow
Write-Host ""

$processes = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue

if ($processes) {
    Write-Host "Found $($processes.Count) process(es)" -ForegroundColor Gray
    foreach ($proc in $processes) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Stopped PID: $($proc.Id)" -ForegroundColor Green
    }
}
else {
    Write-Host "No tunnels running." -ForegroundColor Gray
}

$trackingFile = "$env:USERPROFILE\.cloudflared\active-tunnel.json"
if (Test-Path $trackingFile) {
    Remove-Item $trackingFile -Force
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host ""
