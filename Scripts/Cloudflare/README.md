# Cloudflare Tunnel Setup - ASU Dorms Management System

## ?? Quick Start (2 Minutes)

Get your API accessible from anywhere on the internet - **FREE** and **without port forwarding**!

### Option 1: One-Click (Easiest)

Just **double-click** `START-WITH-TUNNEL.bat` in the project root!

### Option 2: PowerShell

```powershell
# Run as Administrator
cd "C:\Users\Poula Saber\source\repos\ASU Dorms Management System"

# Install cloudflared (one-time setup)
.\Scripts\Cloudflare\Install-CloudflareTunnel.ps1

# Start everything
.\Scripts\Cloudflare\Start-DevEnvironment.ps1
```

---

## What You Get

```
LOCAL ACCESS:
  API:     http://localhost:5065
  Swagger: http://localhost:5065/swagger

PUBLIC ACCESS (via Cloudflare):
  API:     https://random-words.trycloudflare.com
  Swagger: https://random-words.trycloudflare.com/swagger
```

Your API is now accessible from **anywhere in the world** with automatic HTTPS! ??

---

## React Frontend Configuration

After the tunnel starts, add to your React app's `.env.local`:

```env
VITE_API_URL=https://your-tunnel-url.trycloudflare.com
REACT_APP_API_URL=https://your-tunnel-url.trycloudflare.com
```

The script will show you the exact URL to use.

---

## Available Scripts

| Script | Purpose |
|--------|---------|
| `Install-CloudflareTunnel.ps1` | Install cloudflared (one-time) |
| `Start-DevEnvironment.ps1` | **Start API + tunnel together** ? |
| `Start-QuickTunnel.ps1` | Start tunnel only (API must be running) |
| `Stop-AllTunnels.ps1` | Stop all tunnels |
| `Get-TunnelStatus.ps1` | Check current status |
| `Monitor-Tunnel.ps1` | Health monitoring with auto-restart |
| `Troubleshoot-Tunnel.ps1` | Diagnose issues |
| `Setup-WindowsService.ps1` | Install as Windows service (auto-start on boot) |

---

## Key Features

? **100% FREE** - No paid plans required  
? **No Port Forwarding** - Works behind CGNAT/firewalls  
? **Automatic HTTPS** - SSL certificates included  
? **IP Hidden** - Your server IP never exposed  
? **DDoS Protection** - Cloudflare security included  
? **No Configuration** - Works out of the box  

---

## Permanent Setup (Optional)

Quick tunnels give you a **new URL each time** you restart. For a **permanent URL**:

```powershell
.\Scripts\Cloudflare\Setup-WindowsService.ps1
```

Requirements:
- FREE Cloudflare account (sign up at https://dash.cloudflare.com/sign-up)
- Gives you a persistent URL
- Auto-starts tunnel on Windows boot
- Supports custom domains

---

## Troubleshooting

### "cloudflared not found"
```powershell
.\Scripts\Cloudflare\Install-CloudflareTunnel.ps1
```

### "API not reachable"
Start your API first:
```powershell
cd "ASU Dorms Management System"
dotnet run
```

### "CORS error in browser"
The tunnel URL is automatically added to CORS. Restart your API after the tunnel starts.

### Run diagnostics
```powershell
.\Scripts\Cloudflare\Troubleshoot-Tunnel.ps1
```

### Check status
```powershell
.\Scripts\Cloudflare\Get-TunnelStatus.ps1
```

### View logs
```powershell
Get-Content "$env:USERPROFILE\.cloudflared\logs\*.log" -Tail 50
```

---

## For Non-Technical Users

1. **Right-click** `START-WITH-TUNNEL.bat` ? **Run as administrator**
2. Wait for the script to finish (about 1-2 minutes)
3. Copy the public URL that appears
4. Share the URL with your team!

That's it! No complex configuration needed.
