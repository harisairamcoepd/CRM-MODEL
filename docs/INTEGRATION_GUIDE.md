# COEPD AI Funnel OS Integration Guide

## Local Run

1. Build the solution:

```powershell
dotnet build COEPD.SalesFunnelSystem.sln
```

2. Start the web app:

```powershell
dotnet run --project src/COEPD.SalesFunnelSystem.Web --urls http://localhost:5099
```

3. Open these routes:

- Public funnel: `http://localhost:5099/`
- Login: `http://localhost:5099/Auth/Login`
- Admin dashboard: `http://localhost:5099/admin/dashboard`
- Staff workspace: `http://localhost:5099/Staff`
- Swagger in development: `http://localhost:5099/swagger`
- Health check: `http://localhost:5099/health`

## Default Development Credentials

These are seeded automatically in non-production environments:

- Admin: `admin@coepd.local` / `Admin@123`
- Staff: `staff@coepd.local` / `Staff@123`

## Canonical Frontend Files

The public funnel is now served from the ASP.NET app rather than a disconnected static prototype:

- Landing view: `src/COEPD.SalesFunnelSystem.Web/Views/Home/Index.cshtml`
- Landing styles: `src/COEPD.SalesFunnelSystem.Web/wwwroot/css/landing.css`
- Landing behavior and chatbot: `src/COEPD.SalesFunnelSystem.Web/wwwroot/js/landing.js`
- Admin live dashboard script: `src/COEPD.SalesFunnelSystem.Web/wwwroot/js/admin-dashboard.js`

## Core APIs

- `POST /api/leads`
- `GET /api/leads`
- `GET /api/stats`
- `POST /api/demo`
- `POST /api/demo-booking`
- `GET /api/demo/availability`
- `GET /api/admin/lead-growth`
- `GET /api/admin/lead-stats`
- `GET /api/staff/leads/assigned`

## Database

- Development default: SQLite at `src/COEPD.SalesFunnelSystem.Web/App_Data/coepd-crm.db`
- Production target: SQL Server using the schema in `database/schema.sql`

The app now handles legacy local databases created before EF migrations by:

- baselining the initial migration history for SQLite
- backfilling missing audit/status columns
- preserving existing data instead of forcing a reset

## EF Core Migration Commands

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/COEPD.SalesFunnelSystem.Infrastructure `
  --startup-project src/COEPD.SalesFunnelSystem.Web `
  --context ApplicationDbContext

dotnet ef database update `
  --project src/COEPD.SalesFunnelSystem.Infrastructure `
  --startup-project src/COEPD.SalesFunnelSystem.Web `
  --context ApplicationDbContext
```

## Production Environment Variables

Minimum production configuration:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Database__Provider=SqlServer`
- `ConnectionStrings__DefaultConnection=<sql-server-connection-string>`
- `Jwt__Key=<32+ char secret>`
- `Jwt__Issuer=COEPD.SalesFunnel`
- `Jwt__Audience=COEPD.SalesFunnel.Client`
- `Cors__AllowedOrigins__0=https://your-domain.com`

Optional automation settings:

- `Email__Provider`
- `Email__FromEmail`
- `Email__SmtpHost`
- `Email__SmtpUsername`
- `Email__SmtpPassword`
- `Email__SendGridApiKey`
- `WhatsApp__ApiUrl`
- `WhatsApp__AccessToken`
- `WhatsApp__SenderId`

## Smoke Test Checklist

1. Create a lead from the public form.
2. Create a lead and demo booking from the chatbot.
3. Confirm `/api/stats` reflects the new totals.
4. Sign in as Admin and verify `/admin/dashboard` charts refresh.
5. Sign in as Staff and verify only assigned leads are visible.
