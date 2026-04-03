# COEPD AI Sales Funnel System

## Project Folder Structure

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

## Deployment Steps

1. Install .NET 9 SDK and SQL Server.
2. Update `src/COEPD.SalesFunnelSystem.Web/appsettings.json`.
3. Run `dotnet restore`.
4. Apply `database/schema.sql` or add EF migrations.
5. Run `dotnet run --project src/COEPD.SalesFunnelSystem.Web`.
6. Open `https://localhost:7065`.

## Default Credentials

- Admin: `admin@coepd.local` / `Admin@123`
- Staff: `staff@coepd.local` / `Staff@123`
