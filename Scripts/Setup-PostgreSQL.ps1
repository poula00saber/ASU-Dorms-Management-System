<#
.SYNOPSIS
    Sets up PostgreSQL database and creates fresh migrations for Plan-B deployment.
.DESCRIPTION
    1. Removes old SQL Server migrations
    2. Creates new PostgreSQL migrations
    3. Creates the database
    4. Applies migrations
.PARAMETER PostgresPassword
    Your PostgreSQL password (default: postgres)
.EXAMPLE
    .\Setup-PostgreSQL.ps1
    .\Setup-PostgreSQL.ps1 -PostgresPassword "mypassword"
#>

param(
    [string]$PostgresPassword = "your_password_here",
    [string]$DatabaseName = "ASUDormsDB"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  PostgreSQL Setup for Plan-B" -ForegroundColor Cyan
Write-Host "  ASU Dorms Management System" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Update appsettings.json with PostgreSQL connection
Write-Host "[1/5] Updating connection string..." -ForegroundColor Yellow
$settingsPath = Join-Path $ProjectRoot "ASU Dorms Management System\appsettings.json"
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.ConnectionStrings.DefaultConnection = "Host=localhost;Port=5432;Database=$DatabaseName;Username=postgres;Password=$PostgresPassword"
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8
Write-Host "  Connection string updated" -ForegroundColor Green

# Step 2: Remove old migrations
Write-Host ""
Write-Host "[2/5] Removing old SQL Server migrations..." -ForegroundColor Yellow
$migrationsPath = Join-Path $ProjectRoot "ASUDorms.Infrastructure\Migrations"
if (Test-Path $migrationsPath) {
    Remove-Item "$migrationsPath\*" -Force -Recurse
    Write-Host "  Old migrations removed" -ForegroundColor Green
}

# Step 3: Create PostgreSQL database
Write-Host ""
Write-Host "[3/5] Creating PostgreSQL database..." -ForegroundColor Yellow
$env:PGPASSWORD = $PostgresPassword
try {
    # Check if database exists
    $dbExists = psql -U postgres -h localhost -tc "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName'" 2>$null
    if ($dbExists -notmatch "1") {
        psql -U postgres -h localhost -c "CREATE DATABASE `"$DatabaseName`";" 2>$null
        Write-Host "  Database '$DatabaseName' created" -ForegroundColor Green
    } else {
        Write-Host "  Database '$DatabaseName' already exists" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  [WARNING] Could not create database automatically" -ForegroundColor Yellow
    Write-Host "  Please create it manually: CREATE DATABASE `"$DatabaseName`";" -ForegroundColor Gray
}

# Step 4: Restore packages
Write-Host ""
Write-Host "[4/5] Restoring NuGet packages..." -ForegroundColor Yellow
Push-Location $ProjectRoot
dotnet restore
Pop-Location
Write-Host "  Packages restored" -ForegroundColor Green

# Step 5: Create fresh migrations
Write-Host ""
Write-Host "[5/5] Creating fresh PostgreSQL migrations..." -ForegroundColor Yellow
Push-Location $ProjectRoot
dotnet ef migrations add InitialPostgreSQL --project ASUDorms.Infrastructure --startup-project "ASU Dorms Management System" --output-dir Migrations
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Migration created successfully" -ForegroundColor Green
    
    # Apply migrations
    Write-Host ""
    Write-Host "[BONUS] Applying migrations..." -ForegroundColor Yellow
    dotnet ef database update --project ASUDorms.Infrastructure --startup-project "ASU Dorms Management System"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Database updated successfully" -ForegroundColor Green
    }
} else {
    Write-Host "  [ERROR] Migration creation failed" -ForegroundColor Red
}
Pop-Location

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  PostgreSQL Setup Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run the API: dotnet run --project 'ASU Dorms Management System'" -ForegroundColor White
Write-Host "  2. Test at: http://localhost:5065/swagger" -ForegroundColor White
Write-Host ""
Write-Host "To deploy to Railway:" -ForegroundColor Cyan
Write-Host "  1. Push to GitHub" -ForegroundColor White
Write-Host "  2. Connect Railway to your repo" -ForegroundColor White
Write-Host "  3. Add PostgreSQL plugin in Railway" -ForegroundColor White
Write-Host "  4. Set JWT_SECRET environment variable" -ForegroundColor White
Write-Host ""
