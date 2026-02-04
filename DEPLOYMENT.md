# Plan-B: PostgreSQL + Railway + Vercel Deployment

## ?? Overview

This branch contains the **FREE deployment** solution using:
- **Backend**: Railway (PostgreSQL + .NET 9 API)
- **Frontend**: Vercel (React + Vite)
- **Cost**: $0/month for 2+ years

---

## ?? Cost Analysis

| Service | Monthly Cost | Notes |
|---------|-------------|-------|
| Railway API | $0 | $5 credit = 25 months |
| Railway PostgreSQL | $0 | Free tier (1GB) |
| Vercel Frontend | $0 | Free forever |
| **Total** | **$0** | Free for 2+ years |

### Data Volume Fits Free Tier:
- 5,000 students = ~5 MB
- 150,000 meals/month = ~150 MB
- 1 year total = ~2 GB (fits Railway free tier)

---

## ?? Quick Setup

### Prerequisites
1. [PostgreSQL](https://www.postgresql.org/download/) installed locally
2. [Node.js 18+](https://nodejs.org/)
3. [.NET 9 SDK](https://dotnet.microsoft.com/download)
4. [Railway account](https://railway.app/) (free)
5. [Vercel account](https://vercel.com/) (free)
6. [GitHub account](https://github.com/)

### Step 1: Local PostgreSQL Setup

```powershell
# Run the setup script
.\Scripts\Setup-PostgreSQL.ps1 -PostgresPassword "your_postgres_password"
```

Or manually:

```sql
-- In pgAdmin or psql:
CREATE DATABASE "ASUDormsDB";
```

Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ASUDormsDB;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### Step 2: Create Migrations

```powershell
# Remove old SQL Server migrations
Remove-Item "ASUDorms.Infrastructure\Migrations\*" -Force

# Create fresh PostgreSQL migration
dotnet ef migrations add InitialPostgreSQL --project ASUDorms.Infrastructure --startup-project "ASU Dorms Management System"

# Apply migrations
dotnet ef database update --project ASUDorms.Infrastructure --startup-project "ASU Dorms Management System"
```

### Step 3: Test Locally

```powershell
# Backend
cd "ASU Dorms Management System"
dotnet run

# Frontend (separate terminal)
cd "C:\Users\Poula Saber\Downloads\dorms1\dorms"
npm run dev
```

---

## ?? Deploy to Railway (Backend)

### Step 1: Push to GitHub
```powershell
git add .
git commit -m "Plan-B: PostgreSQL + Railway ready"
git push origin plan-B
```

### Step 2: Create Railway Project
1. Go to [railway.app](https://railway.app/)
2. Click **"New Project"** ? **"Deploy from GitHub repo"**
3. Select your repository and `plan-B` branch

### Step 3: Add PostgreSQL
1. In Railway dashboard, click **"+ New"** ? **"Database"** ? **"PostgreSQL"**
2. Railway automatically sets `DATABASE_URL` environment variable

### Step 4: Configure Environment Variables
In Railway ? Settings ? Variables, add:

| Variable | Value |
|----------|-------|
| `JWT_SECRET` | `YourSuperSecretKeyThatIsAtLeast32CharactersLong!ChangeThis` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Step 5: Get Your Railway URL
After deployment, your API will be at:
```
https://your-project-name.railway.app
```

---

## ?? Deploy to Vercel (Frontend)

### Step 1: Push Frontend to GitHub
```powershell
cd "C:\Users\Poula Saber\Downloads\dorms1\dorms"
git checkout plan-B  # or create it
git add .
git commit -m "Plan-B: Vercel deployment"
git push origin plan-B
```

### Step 2: Create Vercel Project
1. Go to [vercel.com](https://vercel.com/)
2. Click **"Add New"** ? **"Project"**
3. Import your frontend repository
4. Select `plan-B` branch

### Step 3: Configure Environment Variables
In Vercel ? Settings ? Environment Variables:

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | `https://your-project-name.railway.app` |

### Step 4: Deploy
Click **"Deploy"** - Vercel handles the rest!

---

## ?? Troubleshooting

### "Database connection failed"
```powershell
# Check if PostgreSQL is running
Get-Service postgresql*

# Test connection
psql -U postgres -h localhost -c "SELECT 1"
```

### "Migration failed"
```powershell
# Check for EF Core tools
dotnet tool install --global dotnet-ef

# Verify package is installed
dotnet restore
```

### "CORS error"
The API is configured to allow:
- `localhost` (development)
- `*.vercel.app` (Vercel deployments)
- `*.railway.app` (Railway deployments)

If you need custom domain, add it to `ServiceExtensions.cs`.

---

## ?? Project Structure

```
ASU Dorms Management System/
??? ASU Dorms Management System/    # API project
?   ??? appsettings.json            # Local config
?   ??? appsettings.Production.json # Railway config
?   ??? Program.cs                  # Entry point
??? ASUDorms.Infrastructure/        # Data layer
?   ??? Migrations/                 # PostgreSQL migrations
?   ??? Data/
??? Dockerfile                      # Railway build
??? railway.json                    # Railway config
??? Scripts/
    ??? Setup-PostgreSQL.ps1        # Local setup
```

---

## ?? Security Checklist

- [ ] Change `JWT_SECRET` to a strong random string
- [ ] Use Railway's auto-generated `DATABASE_URL`
- [ ] Enable HTTPS (Railway provides this automatically)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

---

## ?? Scaling Notes

### When to Upgrade

| Trigger | Solution | Cost |
|---------|----------|------|
| Database > 1GB | Railway Starter ($7/mo) | $7/mo |
| >500 hours/month | Railway Hobby ($5/mo credit) | $5/mo |
| High traffic | Vercel Pro ($20/mo) | $20/mo |

### Data Archiving Strategy
To stay on free tier forever:
```csharp
// Archive meals older than 1 year
var oldMeals = await context.MealTransactions
    .Where(m => m.Date < DateTime.Now.AddYears(-1))
    .ToListAsync();
// Export to CSV, then delete
```

---

## ?? Success!

Once deployed, you'll have:
- ? **API**: `https://your-app.railway.app/swagger`
- ? **Frontend**: `https://your-app.vercel.app`
- ? **Cost**: $0/month
- ? **SSL**: Automatic HTTPS
- ? **CI/CD**: Auto-deploy on git push

---

## ?? Support

If you encounter issues:
1. Check Railway logs: Dashboard ? Deployments ? View Logs
2. Check Vercel logs: Dashboard ? Deployments ? View Build Logs
3. Test API locally first before deploying
