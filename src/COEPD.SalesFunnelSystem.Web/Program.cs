using System.Text;
using COEPD.SalesFunnelSystem.Application.Validators;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using COEPD.SalesFunnelSystem.Infrastructure.Options;
using COEPD.SalesFunnelSystem.Infrastructure.Services;
using COEPD.SalesFunnelSystem.Web.Middleware;
using COEPD.SalesFunnelSystem.Web.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.WithOrigins(
                "http://localhost:5099",
                "https://localhost:7099",
                "http://127.0.0.1:5099",
                "https://127.0.0.1:7099",
                "http://localhost:5500",
                "http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid request." : error.ErrorMessage)
            .Distinct()
            .ToList();

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Validation failed.",
            errors,
            traceId = context.HttpContext.TraceIdentifier
        });
    };
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateLeadRequestValidator>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "__Host.COEPD.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = !isDevelopment;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = signingKey
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff"));
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase, out var remainingPath))
    {
        var destination = $"/admin{remainingPath}{context.Request.QueryString}";
        context.Response.Redirect(destination, permanent: true);
        return;
    }

    await next();
});

app.UseRouting();
app.UseCors("FrontendApp");
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapControllerRoute(
    name: "admin-root",
    pattern: "admin",
    defaults: new { controller = "Admin", action = "Index" });
app.MapControllerRoute(
    name: "admin-dashboard",
    pattern: "admin/dashboard",
    defaults: new { controller = "Admin", action = "Index" });
app.MapControllerRoute(
    name: "admin-leads",
    pattern: "admin/leads",
    defaults: new { controller = "Admin", action = "Leads" });
app.MapControllerRoute(
    name: "admin-area",
    pattern: "admin/{action}/{id?}",
    defaults: new { controller = "Admin" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsureSchemaCompatibilityAsync(db);
    await ApplicationDbSeeder.SeedAsync(db);
}

app.Run();

static async Task EnsureSchemaCompatibilityAsync(ApplicationDbContext db)
{
    const string sql = """
        IF COL_LENGTH('Leads', 'Status') IS NULL
        BEGIN
            ALTER TABLE Leads
            ADD Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Leads_Status DEFAULT 'New';
        END

        IF COL_LENGTH('Leads', 'Score') IS NULL
        BEGIN
            ALTER TABLE Leads
            ADD Score NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Leads_Score DEFAULT 'Warm';
        END

        IF COL_LENGTH('Leads', 'Notes') IS NULL
        BEGIN
            ALTER TABLE Leads
            ADD Notes NVARCHAR(2000) NULL;
        END

        IF COL_LENGTH('Leads', 'FunnelStage') IS NULL
        BEGIN
            ALTER TABLE Leads
            ADD FunnelStage NVARCHAR(30) NOT NULL
            CONSTRAINT DF_Leads_FunnelStage DEFAULT 'New';
        END

        IF COL_LENGTH('AppUsers', 'FailedLoginAttempts') IS NULL
        BEGIN
            ALTER TABLE AppUsers
            ADD FailedLoginAttempts INT NOT NULL
            CONSTRAINT DF_AppUsers_FailedLoginAttempts DEFAULT 0;
        END

        IF COL_LENGTH('AppUsers', 'LockoutEndUtc') IS NULL
        BEGIN
            ALTER TABLE AppUsers
            ADD LockoutEndUtc DATETIME2 NULL;
        END

        IF COL_LENGTH('AppUsers', 'LastLoginAtUtc') IS NULL
        BEGIN
            ALTER TABLE AppUsers
            ADD LastLoginAtUtc DATETIME2 NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Email' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE UNIQUE INDEX IX_Leads_Email ON Leads(Email);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Phone' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE UNIQUE INDEX IX_Leads_Phone ON Leads(Phone);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_CreatedAt' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE INDEX IX_Leads_CreatedAt ON Leads(CreatedAt);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Status' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE INDEX IX_Leads_Status ON Leads(Status);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Source' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE INDEX IX_Leads_Source ON Leads(Source);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_InterestedDomain' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE INDEX IX_Leads_InterestedDomain ON Leads(InterestedDomain);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Status_CreatedAt' AND object_id = OBJECT_ID('Leads'))
        BEGIN
            CREATE INDEX IX_Leads_Status_CreatedAt ON Leads(Status, CreatedAt);
        END

        IF OBJECT_ID('LeadActivityLogs', 'U') IS NULL
        BEGIN
            CREATE TABLE LeadActivityLogs (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                LeadId INT NOT NULL,
                ActivityType NVARCHAR(60) NOT NULL,
                Message NVARCHAR(500) NOT NULL,
                Status NVARCHAR(30) NOT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );

            CREATE INDEX IX_LeadActivityLogs_LeadId ON LeadActivityLogs (LeadId);
        END

        IF OBJECT_ID('FunnelEvents', 'U') IS NULL
        BEGIN
            CREATE TABLE FunnelEvents (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                LeadId INT NOT NULL,
                Stage NVARCHAR(20) NOT NULL,
                Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );

            CREATE INDEX IX_FunnelEvents_LeadId ON FunnelEvents (LeadId);
            CREATE INDEX IX_FunnelEvents_Stage ON FunnelEvents (Stage);
        END

        IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NULL
        BEGIN
            CREATE TABLE LeadFollowUpJobs (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                LeadId INT NOT NULL,
                FollowUpType NVARCHAR(30) NOT NULL,
                DueAt DATETIME2 NOT NULL,
                Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                AttemptCount INT NOT NULL DEFAULT 0,
                ProcessedAt DATETIME2 NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );

            CREATE INDEX IX_LeadFollowUpJobs_DueAt ON LeadFollowUpJobs (DueAt);
            CREATE INDEX IX_LeadFollowUpJobs_Status_DueAt ON LeadFollowUpJobs (Status, DueAt);
        END

        IF EXISTS (SELECT 1 FROM DemoBookings WHERE Status = 'Booked')
        BEGIN
            UPDATE DemoBookings
            SET Status = 'Confirmed'
            WHERE Status = 'Booked';
        END

        UPDATE Leads
        SET Status = CASE FunnelStage
            WHEN 'Contacted' THEN 'Contacted'
            WHEN 'DemoBooked' THEN 'DemoBooked'
            WHEN 'Enrolled' THEN 'Converted'
            ELSE Status
        END
        WHERE (FunnelStage = 'Contacted' AND Status <> 'Contacted')
           OR (FunnelStage = 'DemoBooked' AND Status <> 'DemoBooked')
           OR (FunnelStage = 'Enrolled' AND Status <> 'Converted');

        UPDATE Leads
        SET Status = 'DemoBooked',
            FunnelStage = 'DemoBooked'
        WHERE EXISTS (
            SELECT 1
            FROM DemoBookings db
            WHERE db.LeadId = Leads.Id
              AND db.Status IN ('Pending', 'Confirmed')
        )
          AND (Status <> 'DemoBooked' OR FunnelStage <> 'DemoBooked');
        """;

    try
    {
        await db.Database.ExecuteSqlRawAsync(sql);
    }
    catch (SqlException)
    {
        // Let the app continue using the existing schema when the column already exists under another state.
    }
}
