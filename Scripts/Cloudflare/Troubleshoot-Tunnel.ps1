<#
.SYNOPSIS
    Enhanced troubleshooter for ASU Dorms Cloudflare tunnel.
.DESCRIPTION
    Diagnoses common issues and provides fixes for tunnel URL detection problems.
#>

param(
    [switch]$Verbose
)

$ConfigPath = "$env:USERPROFILE\.cloudflared"
$issues = @()
$fixes = @()

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ASU Dorms Tunnel Troubleshooter" -ForegroundColor Cyan
Write-Host "  Enhanced Diagnostics" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check cloudflared in all possible locations
Write-Host "[1/10] Checking cloudflared installation..." -ForegroundColor Yellow
$cloudflaredLocations = @(
    "C:\cloudflared\cloudflared.exe",
    "$env:ProgramFiles\cloudflared\cloudflared.exe",
    "$env:LOCALAPPDATA\cloudflared\cloudflared.exe"
)

$foundAt = $null
foreach ($location in $cloudflaredLocations) {
    if (Test-Path $location) {
        $foundAt = $location
        try {
            $version = & $location --version 2>&1
            Write-Host "  PASS: Found at $location" -ForegroundColor Green
            Write-Host "        Version: $version" -ForegroundColor Gray
            break
        }
        catch {
            Write-Host "  WARN: Found but cannot execute: $location" -ForegroundColor Yellow
        }
    }
}

if (-not $foundAt) {
    try {
        $pathVersion = (Get-Command cloudflared -ErrorAction Stop).Source
        $foundAt = $pathVersion
        Write-Host "  PASS: Found in PATH: $pathVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "  FAIL: cloudflared not found" -ForegroundColor Red
        $issues += "cloudflared not installed"
        $fixes += "Download from: https://github.com/cloudflare/cloudflared/releases/latest"
        $fixes += "Save to: C:\cloudflared\cloudflared.exe"
    }
}

# Test 2: Config directory structure
Write-Host ""
Write-Host "[2/10] Checking configuration directories..." -ForegroundColor Yellow
$requiredDirs = @(
    $ConfigPath,
    "$ConfigPath\logs"
)

foreach ($dir in $requiredDirs) {
    if (Test-Path $dir) {
        Write-Host "  PASS: $dir exists" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL: $dir missing" -ForegroundColor Red
        $issues += "Config directory missing: $dir"
        $fixes += "Run: New-Item -ItemType Directory -Path '$dir' -Force"
    }
}

# Test 3: Internet connectivity
Write-Host ""
Write-Host "[3/10] Testing internet connectivity..." -ForegroundColor Yellow
$testHosts = @(
    @{Host="cloudflare.com"; Port=443},
    @{Host="api.cloudflare.com"; Port=443},
    @{Host="1.1.1.1"; Port=443}
)

$internetOk = $false
foreach ($test in $testHosts) {
    try {
        $result = Test-NetConnection -ComputerName $test.Host -Port $test.Port -WarningAction SilentlyContinue -InformationLevel Quiet -ErrorAction SilentlyContinue
        if ($result) {
            Write-Host "  PASS: Can reach $($test.Host)" -ForegroundColor Green
            $internetOk = $true
            break
        }
    }
    catch {
        Write-Host "  WARN: Cannot reach $($test.Host)" -ForegroundColor Yellow
    }
}

if (-not $internetOk) {
    Write-Host "  FAIL: No internet connectivity" -ForegroundColor Red
    $issues += "Cannot reach Cloudflare servers"
    $fixes += "Check your internet connection"
    $fixes += "Check firewall settings"
}

# Test 4: .NET SDK
Write-Host ""
Write-Host "[4/10] Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "  PASS: .NET SDK installed ($dotnetVersion)" -ForegroundColor Green
}
catch {
    Write-Host "  FAIL: .NET SDK not found" -ForegroundColor Red
    $issues += ".NET SDK not installed"
    $fixes += "Download from: https://dotnet.microsoft.com/download"
}

# Test 5: API port availability
Write-Host ""
Write-Host "[5/10] Checking API port (5065)..." -ForegroundColor Yellow
$portTest = Test-NetConnection -ComputerName localhost -Port 5065 -WarningAction SilentlyContinue -InformationLevel Quiet -ErrorAction SilentlyContinue
if ($portTest) {
    Write-Host "  PASS: Service running on port 5065" -ForegroundColor Green
}
else {
    Write-Host "  INFO: No service on port 5065 (expected if not running)" -ForegroundColor Gray
    Write-Host "        Start with: dotnet run --urls http://localhost:5065" -ForegroundColor Gray
}

# Test 6: Running processes
Write-Host ""
Write-Host "[6/10] Checking running processes..." -ForegroundColor Yellow
$cloudflaredProcs = Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue
if ($cloudflaredProcs) {
    Write-Host "  INFO: $($cloudflaredProcs.Count) cloudflared process(es) running" -ForegroundColor Green
    foreach ($proc in $cloudflaredProcs) {
        $mem = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        Write-Host "        PID $($proc.Id): $mem MB" -ForegroundColor Gray
    }
}
else {
    Write-Host "  INFO: No tunnel running (expected if not started)" -ForegroundColor Gray
}

# Test 7: Recent log files
Write-Host ""
Write-Host "[7/10] Analyzing recent logs..." -ForegroundColor Yellow
$logDir = "$ConfigPath\logs"
if (Test-Path $logDir) {
    $recentLogs = Get-ChildItem $logDir -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    
    if ($recentLogs) {
        Write-Host "  Found $($recentLogs.Count) recent log file(s)" -ForegroundColor Green
        
        foreach ($log in $recentLogs) {
            Write-Host "  Checking: $($log.Name)" -ForegroundColor Gray
            $content = Get-Content $log.FullName -Raw -ErrorAction SilentlyContinue
            
            # Check for URL
            if ($content -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
                Write-Host "    ✓ Contains tunnel URL: $($Matches[0])" -ForegroundColor Green
            }
            
            # Check for errors
            $errors = $content -split "`n" | Where-Object { $_ -match "error|failed|fatal" -and $_ -notmatch "ERR=0" }
            if ($errors) {
                Write-Host "    ⚠ Found errors:" -ForegroundColor Yellow
                $errors | Select-Object -First 3 | ForEach-Object {
                    Write-Host "      $_" -ForegroundColor Red
                }
            }
        }
    }
    else {
        Write-Host "  INFO: No log files found" -ForegroundColor Gray
    }
}

# Test 8: Active tunnel info
Write-Host ""
Write-Host "[8/10] Checking active tunnel info..." -ForegroundColor Yellow
$activeTunnelFile = "$ConfigPath\active-tunnel.json"
if (Test-Path $activeTunnelFile) {
    try {
        $tunnelInfo = Get-Content $activeTunnelFile | ConvertFrom-Json
        Write-Host "  PASS: Active tunnel info found" -ForegroundColor Green
        Write-Host "        URL: $($tunnelInfo.Url)" -ForegroundColor Cyan
        Write-Host "        Port: $($tunnelInfo.Port)" -ForegroundColor Gray
        Write-Host "        Started: $($tunnelInfo.StartTime)" -ForegroundColor Gray
        
        if ($tunnelInfo.Url -eq "URL_NOT_DETECTED") {
            Write-Host "  WARN: URL was not detected during last run" -ForegroundColor Yellow
            $issues += "Tunnel started but URL not extracted from logs"
        }
    }
    catch {
        Write-Host "  WARN: Could not parse tunnel info" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  INFO: No active tunnel" -ForegroundColor Gray
}

# Test 9: Windows Event Log
Write-Host ""
Write-Host "[9/10] Checking Windows Event Log..." -ForegroundColor Yellow
try {
    $recentErrors = Get-EventLog -LogName Application -Source "*cloudflared*" -EntryType Error -Newest 5 -ErrorAction SilentlyContinue
    if ($recentErrors) {
        Write-Host "  WARN: Found recent cloudflared errors in Event Log" -ForegroundColor Yellow
        $recentErrors | ForEach-Object {
            Write-Host "    $($_.TimeGenerated): $($_.Message.Substring(0, [Math]::Min(100, $_.Message.Length)))..." -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  PASS: No recent errors in Event Log" -ForegroundColor Green
    }
}
catch {
    Write-Host "  INFO: Could not check Event Log" -ForegroundColor Gray
}

# Test 10: Firewall rules
Write-Host ""
Write-Host "[10/10] Checking Windows Firewall..." -ForegroundColor Yellow
try {
    $firewallRules = Get-NetFirewallRule -DisplayName "*cloudflared*" -ErrorAction SilentlyContinue
    if ($firewallRules) {
        Write-Host "  INFO: Found firewall rules for cloudflared" -ForegroundColor Green
        foreach ($rule in $firewallRules) {
            Write-Host "        $($rule.DisplayName): $($rule.Enabled)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  INFO: No specific firewall rules (should work anyway)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "  INFO: Could not check firewall" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  DIAGNOSTIC SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

if ($issues.Count -eq 0) {
    Write-Host "✓ All checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "System appears healthy. If you're still having issues:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\Start-DevEnvironment.ps1 -Verbose" -ForegroundColor White
    Write-Host "  2. Check logs in real-time: Get-Content '$ConfigPath\logs\tunnel-*.log' -Wait" -ForegroundColor White
    Write-Host "  3. Test manually: cloudflared tunnel --url http://localhost:5065" -ForegroundColor White
}
else {
    Write-Host "Found $($issues.Count) issue(s):" -ForegroundColor Red
    Write-Host ""
    
    for ($i = 0; $i -lt $issues.Count; $i++) {
        Write-Host "  $($i+1). $($issues[$i])" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Suggested fixes:" -ForegroundColor Cyan
    Write-Host ""
    
    $fixes | ForEach-Object {
        Write-Host "  → $_" -ForegroundColor White
    }
}

# Verbose output
if ($Verbose -and (Test-Path $logDir)) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "  VERBOSE: Recent Log Content" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    
    $latestLog = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "Latest log: $($latestLog.FullName)" -ForegroundColor Gray
        Write-Host ""
        Get-Content $latestLog.FullName -Tail 50 | ForEach-Object {
            if ($_ -match "error|failed|fatal") {
                Write-Host $_ -ForegroundColor Red
            }
            elseif ($_ -match "https://") {
                Write-Host $_ -ForegroundColor Green
            }
            else {
                Write-Host $_ -ForegroundColor Gray
            }
        }
    }
}

Write-Host ""
Write-Host "For more help, visit: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps" -ForegroundColor Gray
Write-Host ""