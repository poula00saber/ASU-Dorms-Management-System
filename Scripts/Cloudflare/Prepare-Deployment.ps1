<#
.SYNOPSIS
    Prepares ASU Dorms system for deployment to multiple devices.
.DESCRIPTION
    Creates a deployment package that can be copied to 40+ devices across 7 locations.
    Includes all necessary files and setup instructions.
.PARAMETER OutputPath
    Where to create the deployment package (default: Desktop)
.EXAMPLE
    .\Prepare-Deployment.ps1
    .\Prepare-Deployment.ps1 -OutputPath "D:\Deployments"
#>

param(
    [string]$OutputPath = "$env:USERPROFILE\Desktop\ASU-Dorms-Deployment"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ASU Dorms Deployment Packager" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Create deployment folder
if (Test-Path $OutputPath) {
    Write-Host "[WARNING] Deployment folder already exists" -ForegroundColor Yellow
    $response = Read-Host "Delete and recreate? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Remove-Item $OutputPath -Recurse -Force
    }
    else {
        Write-Host "Cancelled." -ForegroundColor Gray
        exit 0
    }
}

Write-Host "[1/6] Creating deployment structure..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Backend" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Frontend" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Scripts" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Tools" -Force | Out-Null
Write-Host "  Created folders" -ForegroundColor Green

# Get source paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path "$scriptDir\..\..").Path

Write-Host ""
Write-Host "[2/6] Copying backend (.NET API)..." -ForegroundColor Yellow
$backendSource = "$projectRoot\ASU Dorms Management System"
if (Test-Path $backendSource) {
    # Copy only necessary files (exclude bin, obj, logs)
    $excludePatterns = @('bin', 'obj', '.vs', 'logs', '*.user', '*.suo')
    
    Get-ChildItem $backendSource -Recurse | Where-Object {
        $item = $_
        $excluded = $false
        foreach ($pattern in $excludePatterns) {
            if ($item.FullName -like "*\$pattern\*" -or $item.Name -like $pattern) {
                $excluded = $true
                break
            }
        }
        -not $excluded
    } | ForEach-Object {
        $dest = $_.FullName.Replace($backendSource, "$OutputPath\Backend")
        if ($_.PSIsContainer) {
            New-Item -ItemType Directory -Path $dest -Force | Out-Null
        }
        else {
            Copy-Item $_.FullName -Destination $dest -Force
        }
    }
    Write-Host "  Copied backend files" -ForegroundColor Green
}
else {
    Write-Host "  [WARNING] Backend not found at: $backendSource" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[3/6] Copying frontend (React)..." -ForegroundColor Yellow
$frontendSource = "$projectRoot\asu-dorms-frontend"  # Adjust to your React folder
if (Test-Path $frontendSource) {
    $excludePatterns = @('node_modules', 'dist', 'build', '.next', '.env.local')
    
    Get-ChildItem $frontendSource -Recurse | Where-Object {
        $item = $_
        $excluded = $false
        foreach ($pattern in $excludePatterns) {
            if ($item.FullName -like "*\$pattern\*" -or $item.Name -like $pattern) {
                $excluded = $true
                break
            }
        }
        -not $excluded
    } | ForEach-Object {
        $dest = $_.FullName.Replace($frontendSource, "$OutputPath\Frontend")
        if ($_.PSIsContainer) {
            New-Item -ItemType Directory -Path $dest -Force | Out-Null
        }
        else {
            Copy-Item $_.FullName -Destination $dest -Force
        }
    }
    Write-Host "  Copied frontend files" -ForegroundColor Green
}
else {
    Write-Host "  [WARNING] Frontend not found at: $frontendSource" -ForegroundColor Yellow
    Write-Host "  Update the `$frontendSource variable in this script" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[4/6] Copying deployment scripts..." -ForegroundColor Yellow
$scriptsSource = "$projectRoot\Scripts\Cloudflare"
if (Test-Path $scriptsSource) {
    Copy-Item "$scriptsSource\*" -Destination "$OutputPath\Scripts\" -Recurse -Force
    Write-Host "  Copied PowerShell scripts" -ForegroundColor Green
}

# Copy batch files
Copy-Item "$projectRoot\START-WITH-TUNNEL.bat" -Destination "$OutputPath\" -Force -ErrorAction SilentlyContinue
Write-Host "  Copied batch launcher" -ForegroundColor Green

Write-Host ""
Write-Host "[5/6] Downloading required tools..." -ForegroundColor Yellow

# Download cloudflared
Write-Host "  Downloading cloudflared.exe..." -ForegroundColor Gray
try {
    $cloudflaredUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
    Invoke-WebRequest -Uri $cloudflaredUrl -OutFile "$OutputPath\Tools\cloudflared.exe" -UseBasicParsing
    Write-Host "  Downloaded cloudflared.exe" -ForegroundColor Green
}
catch {
    Write-Host "  [WARNING] Failed to download cloudflared: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[6/6] Creating setup instructions..." -ForegroundColor Yellow

$instructions = @"
# ASU Dorms Management System - Deployment Package
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## What's Included

- **Backend**: .NET API (ASP.NET Core)
- **Frontend**: React Application
- **Scripts**: Automated deployment scripts
- **Tools**: Cloudflared.exe for public access

## System Requirements (Each Device)

1. **Windows 10/11** (64-bit)
2. **.NET 8 SDK** or higher
   - Download: https://dotnet.microsoft.com/download
3. **Node.js 18+** (for React frontend)
   - Download: https://nodejs.org/
4. **SQL Server** (LocalDB, Express, or full version)
   - Download: https://www.microsoft.com/sql-server/sql-server-downloads
5. **Internet Connection** (for Cloudflare tunnel)

## Quick Setup (3 Steps)

### Step 1: Install Prerequisites

Run PowerShell as Administrator and execute:

``````powershell
# Check if .NET is installed
dotnet --version

# Check if Node.js is installed
node --version

# Check if SQL Server is accessible
# (This will vary based on your SQL Server installation)
``````

If any command fails, install the missing component from the links above.

### Step 2: Setup Cloudflared

Copy `Tools\cloudflared.exe` to `C:\cloudflared\cloudflared.exe`

``````powershell
# Create directory
New-Item -ItemType Directory -Path "C:\cloudflared" -Force

# Copy file
Copy-Item "Tools\cloudflared.exe" -Destination "C:\cloudflared\cloudflared.exe"
``````

### Step 3: Configure Database

1. Open `Backend\appsettings.json`
2. Update the connection string to point to your SQL Server:

``````json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ASUDorms;Trusted_Connection=True;"
  }
}
``````

For SQL Server Express, use:
``````
Server=.\\SQLEXPRESS;Database=ASUDorms;Trusted_Connection=True;
``````

### Step 4: Run Database Migrations

``````powershell
cd Backend
dotnet ef database update
``````

## Running the System

### Option 1: One-Click Launch (Easiest)

1. **Double-click** `START-WITH-TUNNEL.bat`
2. Wait 1-2 minutes for everything to start
3. Copy the public URL that appears
4. Share with your team!

### Option 2: Manual Launch

``````powershell
# Terminal 1: Start Backend + Tunnel
.\Scripts\Start-DevEnvironment.ps1

# Terminal 2: Start Frontend (after getting tunnel URL)
cd Frontend
npm install  # First time only
npm run dev
``````

## What You'll See

After running START-WITH-TUNNEL.bat:

``````
==========================================
  ENVIRONMENT READY!
==========================================

LOCAL ACCESS:
  API:     http://localhost:5065
  Swagger: http://localhost:5065/swagger

PUBLIC ACCESS (Cloudflare):
  API:     https://random-words.trycloudflare.com
  Swagger: https://random-words.trycloudflare.com/swagger
``````

The **public URL** is automatically accessible from anywhere on the internet!

## Deploying to 40 Devices

For each device:

1. Copy this entire folder to the device
2. Follow Quick Setup steps above
3. Run `START-WITH-TUNNEL.bat`
4. Each device will get its own public URL

## Network Configuration

- **No port forwarding required**
- **Works behind CGNAT/firewalls**
- **Automatic HTTPS**
- **Free unlimited usage**

Each location's 5-6 devices can:
- Access the same local database (if on same network)
- OR each run their own independent instance
- All accessible via unique Cloudflare tunnel URLs

## Troubleshooting

### "cloudflared not found"
Make sure you copied `cloudflared.exe` to `C:\cloudflared\cloudflared.exe`

### "API failed to start"
Check that .NET SDK is installed: `dotnet --version`

### "Database connection failed"
1. Verify SQL Server is running
2. Update connection string in `Backend\appsettings.json`
3. Run migrations: `dotnet ef database update`

### "No tunnel URL generated"
1. Check internet connectivity
2. Run: `.\Scripts\Troubleshoot-Tunnel.ps1`
3. View logs: `Get-Content "$env:USERPROFILE\.cloudflared\logs\*.log"`

### Get Help
Run diagnostics: `.\Scripts\Troubleshoot-Tunnel.ps1`

## Advanced: Permanent Tunnel URLs

For production deployment with persistent URLs:

``````powershell
.\Scripts\Setup-WindowsService.ps1
``````

This requires a FREE Cloudflare account but gives you:
- Fixed URL (doesn't change on restart)
- Auto-start on Windows boot
- Better reliability

## Security Notes

1. **Change default passwords** in appsettings.json
2. **Configure CORS** properly for production
3. **Enable authentication** for public access
4. **Use HTTPS** (automatically provided by Cloudflare)

## Support

- Check logs: `%USERPROFILE%\.cloudflared\logs`
- View active config: `.\Scripts\Get-TunnelStatus.ps1`
- Get API config: `.\Scripts\Get-ApiConfig.ps1`

## File Structure

``````
ASU-Dorms-Deployment/
├── Backend/                 # .NET API
├── Frontend/                # React App  
├── Scripts/                 # PowerShell scripts
├── Tools/                   # cloudflared.exe
├── START-WITH-TUNNEL.bat   # Main launcher
└── README.md               # This file
``````

---

**Ready to deploy?** Just copy this folder to each device and run START-WITH-TUNNEL.bat!
"@

Set-Content -Path "$OutputPath\README.md" -Value $instructions -Encoding UTF8
Write-Host "  Created README.md" -ForegroundColor Green

# Create a quick reference card
$quickRef = @"
QUICK REFERENCE CARD
====================

SETUP (One time per device):
1. Install .NET SDK: https://dotnet.microsoft.com/download
2. Install Node.js: https://nodejs.org/
3. Copy cloudflared.exe to C:\cloudflared\
4. Update database connection in Backend\appsettings.json
5. Run: cd Backend && dotnet ef database update

RUN:
Double-click: START-WITH-TUNNEL.bat

WHAT YOU GET:
✓ Local API at http://localhost:5065
✓ Public API at https://xxx.trycloudflare.com (changes each restart)
✓ Auto-configured React frontend
✓ Automatic HTTPS
✓ Works behind any firewall

TROUBLESHOOTING:
Scripts\Troubleshoot-Tunnel.ps1
"@

Set-Content -Path "$OutputPath\QUICK-START.txt" -Value $quickRef -Encoding UTF8
Write-Host "  Created QUICK-START.txt" -ForegroundColor Green

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Deployment Package Ready!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Location: $OutputPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Package Contents:" -ForegroundColor Yellow
Write-Host "    ✓ Backend (.NET API)" -ForegroundColor White
Write-Host "    ✓ Frontend (React)" -ForegroundColor White
Write-Host "    ✓ Automation scripts" -ForegroundColor White
Write-Host "    ✓ Cloudflared.exe" -ForegroundColor White
Write-Host "    ✓ Setup instructions" -ForegroundColor White
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Yellow
Write-Host "    1. Test the package on your dev machine" -ForegroundColor White
Write-Host "    2. Copy to a USB drive or network share" -ForegroundColor White
Write-Host "    3. Deploy to your 40 devices across 7 locations" -ForegroundColor White
Write-Host "    4. Each device: Run START-WITH-TUNNEL.bat" -ForegroundColor White
Write-Host ""
Write-Host "  Testing:" -ForegroundColor Yellow
Write-Host "    cd '$OutputPath'" -ForegroundColor White
Write-Host "    .\START-WITH-TUNNEL.bat" -ForegroundColor White
Write-Host ""`

# Open the folder
Start-Process explorer.exe -ArgumentList $OutputPath