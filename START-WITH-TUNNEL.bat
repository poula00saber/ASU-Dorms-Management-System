@echo off
REM ==================================================
REM ASU Dorms Management System - Auto Launcher
REM ==================================================
REM This script automatically starts:
REM   1. .NET API (Backend)
REM   2. Cloudflare Tunnel (Public Access)
REM   3. Configures React Frontend
REM ==================================================

title ASU Dorms - Starting...

echo.
echo ==================================================
echo   ASU Dorms Management System
echo   Auto Launcher with Cloudflare Tunnel
echo ==================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ADMIN] Requesting administrator privileges...
    echo.
    PowerShell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo [OK] Running with administrator privileges
echo.

REM Change to script directory
cd /d "%~dp0"

REM Run the PowerShell script
echo [START] Launching development environment...
echo.
PowerShell -ExecutionPolicy Bypass -NoProfile -File "%~dp0Scripts\Cloudflare\Start-DevEnvironment.ps1"

REM If PowerShell script fails, show error
if %errorLevel% neq 0 (
    echo.
    echo ==================================================
    echo   ERROR: Failed to start environment
    echo ==================================================
    echo.
    echo Troubleshooting:
    echo   1. Make sure .NET SDK is installed
    echo   2. Verify cloudflared.exe exists at: C:\cloudflared\cloudflared.exe
    echo   3. Check your internet connection
    echo   4. Run: .\Scripts\Cloudflare\Troubleshoot-Tunnel.ps1
    echo.
    pause
    exit /b 1
)

pause