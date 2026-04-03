# CRM Web API Backend Structure

## Architecture (Clean Architecture)

```text
COEPD.SalesFunnelSystem.sln
src/
  COEPD.SalesFunnelSystem.Domain/
    Common/
      BaseEntity.cs
    Entities/
      Lead.cs
      DemoBooking.cs
      AppUser.cs
    Enums/
      UserRole.cs

  COEPD.SalesFunnelSystem.Application/
    DTOs/
      LeadDtos.cs
      DemoBookingDtos.cs
      StatsDtos.cs
      UserDtos.cs
      AuthDtos.cs
    Interfaces/
      Contracts.cs
    Services/
      LeadService.cs
      DemoBookingService.cs
      AnalyticsService.cs
      UserService.cs
      AuthService.cs
    Validators/
      Validators.cs
    Common/
      AppExceptions.cs

  COEPD.SalesFunnelSystem.Infrastructure/
    Data/
      ApplicationDbContext.cs
    Repositories/
      Repositories.cs
    Services/
      InfrastructureServices.cs
    Options/
      Options.cs

  COEPD.SalesFunnelSystem.Web/
    Controllers/Api/
      ApiControllers.cs
    Middleware/
      ApiExceptionMiddleware.cs
    Seed/
      ApplicationDbSeeder.cs
    Program.cs
    appsettings.json
database/
  schema.sql
```

## Modules Delivered

### 1. Leads
- `POST /api/leads` -> create lead
- `GET /api/leads` -> get all leads
- `PUT /api/leads/{id}/status` -> update lead status (`New`, `Contacted`, `DemoBooked`, `Converted`)

Lead fields: `Id`, `Name`, `Phone`, `Email`, `Location`, `Domain`, `Source`, `Status`, `CreatedAt`

### 2. Demo Booking
- `POST /api/demo` -> book demo
- `GET /api/demo/lead/{leadId}` -> get lead bookings
- `LeadId` is validated in service and validator
- Stores `Day` and `Slot`

### 3. Dashboard
- `GET /api/dashboard/stats` -> includes:
  - total leads
  - today leads
  - conversion count

### 4. Users (Admin/Staff roles)
- `GET /api/users` (Admin only)
- `POST /api/users` (Admin only)
- `PUT /api/users/{id}/role` (Admin only)
- `PUT /api/users/{id}/status` (Admin only)

Role values: `Admin`, `Staff`

## Technical Requirements Coverage

- Entity Framework Core with SQL Server (`ApplicationDbContext` + `UseSqlServer`)
- Clean architecture layering (Domain, Application, Infrastructure, Web)
- Centralized API error handling middleware (`ApiExceptionMiddleware`)
- FluentValidation for request validation
- JWT + role-based authorization policies

## Run

1. Install .NET SDK (8+ or 9) and SQL Server.
2. Update `src/COEPD.SalesFunnelSystem.Web/appsettings.json`.
3. Run:
   - `dotnet restore`
   - `dotnet run --project src/COEPD.SalesFunnelSystem.Web`
