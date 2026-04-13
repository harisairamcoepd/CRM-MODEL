# COEPD CRM Production Deployment Guide

## Production Hardening Included

The application now includes these production-oriented behaviors:

- JWT validation with production startup checks for strong secrets
- Role-based authorization policies for `AdminOnly` and `StaffOrAdmin`
- Response compression for better payload performance
- Global rate limiting for API abuse protection
- Forwarded header support for IIS, Azure App Service, reverse proxies, and Render
- Security headers and per-request trace IDs
- Structured request logging and API problem-details error responses
- Health check endpoint at `/health`
- Docker support for container deployments

## Required Environment Variables

Use environment variables instead of storing secrets in source control.

Core settings:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=SqlServer
ConnectionStrings__DefaultConnection=Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=coepd-crm;Persist Security Info=False;User ID=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
Jwt__Key=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS
Jwt__Issuer=COEPD.SalesFunnel
Jwt__Audience=COEPD.SalesFunnel.Client
Jwt__ExpiryHours=8
Cors__AllowedOrigins__0=https://your-frontend.example.com
```

Automation settings:

```text
Email__Provider=SendGrid
Email__FromName=COEPD CRM
Email__FromEmail=no-reply@yourdomain.com
Email__SendGridApiKey=CHANGE_ME
WhatsApp__ApiUrl=https://graph.facebook.com/v20.0/YOUR_PHONE_NUMBER_ID/messages
WhatsApp__AccessToken=CHANGE_ME
WhatsApp__SenderId=YOUR_SENDER_ID
```

Seeded users:

```text
COEPD_ADMIN_EMAIL=admin@yourdomain.com
COEPD_ADMIN_PASSWORD=CHANGE_ME
COEPD_STAFF_EMAIL=staff@yourdomain.com
COEPD_STAFF_PASSWORD=CHANGE_ME
```

Reference file: [.env.example](C:\Users\PC\Desktop\CRM MODEL\.env.example)

## IIS Deployment

1. Install:
   - .NET 8 Hosting Bundle
   - IIS with `ASP.NET Core Module`
2. Publish:

```powershell
dotnet publish src/COEPD.SalesFunnelSystem.Web -c Release -o .\publish
```

3. Create an IIS site pointing to the `publish` folder.
4. Configure the App Pool:
   - `No Managed Code`
   - 64-bit enabled
5. Add production environment variables at the server or App Pool level.
6. Ensure SQL Server is reachable from the IIS host.
7. Verify:
   - `https://your-domain/health`
   - login flow
   - `/api/auth/login` and protected admin/staff APIs

## Render Deployment

The repository now includes:

- [Dockerfile](C:\Users\PC\Desktop\CRM MODEL\Dockerfile)
- [render.yaml](C:\Users\PC\Desktop\CRM MODEL\render.yaml)

Steps:

1. Push the repo to GitHub.
2. Create a new Render Web Service from the repo.
3. Let Render use the included `render.yaml`, or point it to the included `Dockerfile`.
4. Add all required environment variables from `.env.example`.
5. Use a managed SQL Server instance reachable from Render.
6. Confirm the service passes `/health`.

## Azure App Service Deployment

Recommended setup:

- Azure App Service for the web app
- Azure SQL Database for persistence
- Azure Key Vault for secrets

Steps:

1. Create an Azure SQL Database and firewall rule for the app.
2. Create an App Service running `.NET 8` or deploy the included container image.
3. Configure App Settings with the same keys shown in `.env.example`.
4. Set:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=SqlServer
```

5. Store secrets in Key Vault and reference them from App Service when possible.
6. Verify `/health` and protected APIs after deployment.

## Security Checklist

- Replace the demo JWT key before production startup
- Use HTTPS only in public environments
- Restrict `Cors__AllowedOrigins` to known frontend domains
- Use strong seeded passwords or replace seeded accounts after first login
- Store email and WhatsApp tokens in secret storage, not source control
- Monitor rate-limit responses and request logs

## Operational Notes

- Production startup will fail fast if:
  - `Database__Provider` is not `SqlServer`
  - SQL Server connection string is missing
  - JWT key is weak or still a demo key
  - CORS origins are not configured
- API errors return `application/problem+json` with a trace ID
- Reverse-proxy headers are honored for IIS, Azure, and Render
