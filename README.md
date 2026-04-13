# COEPD CRM Model

COEPD CRM Model is a .NET 8 admissions CRM for lead capture, chatbot-assisted qualification, demo scheduling, funnel tracking, staff notifications, and admin/staff pipeline management.

## Production Readiness

The web application now includes:

- JWT authentication and role-based authorization for `Admin` and `Staff`
- Global rate limiting and response compression
- Forwarded-header support for reverse proxies and load balancers
- Security headers and request trace IDs
- Health checks at `/health`
- Production config templates and environment variable examples
- Deployment assets for IIS, Render, and Azure

See [docs/DEPLOYMENT.md](C:\Users\PC\Desktop\CRM MODEL\docs\DEPLOYMENT.md) for the full deployment guide.

## Solution Structure

```text
COEPD.SalesFunnelSystem.sln
database/
  schema.sql
src/
  COEPD.SalesFunnelSystem.Domain/
  COEPD.SalesFunnelSystem.Application/
  COEPD.SalesFunnelSystem.Infrastructure/
  COEPD.SalesFunnelSystem.Web/
```

## Local Run

1. Install the .NET 8 SDK or newer.
2. Restore and build the solution:

```powershell
dotnet restore
dotnet build COEPD.SalesFunnelSystem.sln
```

3. Start the web app:

```powershell
dotnet run --project src/COEPD.SalesFunnelSystem.Web
```

4. Open the CRM in the browser:

- `https://localhost:7099`
- `http://localhost:5099`

## Database Modes

The project now supports two database modes:

- `Sqlite`:
  Default for local development. The app auto-creates `src/COEPD.SalesFunnelSystem.Web/App_Data/coepd-crm.db`.
- `SqlServer`:
  For organizational deployment. Set `Database:Provider` to `SqlServer` and provide a valid SQL Server connection string in `ConnectionStrings:DefaultConnection` or `ConnectionStrings:SqlServerConnection`.

## Authentication Setup

- Configure secure credentials for seeded users with environment variables:
  `COEPD_ADMIN_EMAIL`, `COEPD_ADMIN_PASSWORD`, `COEPD_STAFF_EMAIL`, and `COEPD_STAFF_PASSWORD`.
- In local non-production mode, the app can still fall back to development-only seeded users for easier testing.
- In production, provide secure environment variables instead of relying on any defaults.

## Environment Variables

Use [.env.example](C:\Users\PC\Desktop\CRM MODEL\.env.example) as the source of truth for required production variables.

## Organization Setup Notes

- Update `Jwt`, `Email`, `WhatsApp`, and `Cors` values in `src/COEPD.SalesFunnelSystem.Web/appsettings.json` for your organization.
- SMTP and WhatsApp integrations safely simulate delivery when real provider settings are not configured, so demos and local testing still work.
- `database/schema.sql` can still be used for legacy SQL Server environments, but the app can bootstrap itself in local SQLite mode without manual database setup.
