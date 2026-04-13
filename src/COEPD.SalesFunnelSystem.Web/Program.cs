using System.Text;
using COEPD.SalesFunnelSystem.Application.Validators;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using COEPD.SalesFunnelSystem.Infrastructure.Options;
using COEPD.SalesFunnelSystem.Infrastructure.Services;
using COEPD.SalesFunnelSystem.Web.Hubs;
using COEPD.SalesFunnelSystem.Web.Infrastructure;
using COEPD.SalesFunnelSystem.Web.Middleware;
using COEPD.SalesFunnelSystem.Web.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var isProduction = builder.Environment.IsProduction();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

ValidateCriticalConfiguration(builder.Configuration, isProduction);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
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
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.Identity?.Name}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
builder.Services.AddSignalR();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});
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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var requestPath = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) && requestPath.StartsWithSegments(LeadHub.HubPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
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

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (!app.Environment.IsDevelopment())
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=604800";
        }
    }
});

app.Use(async (context, next) =>
{
    var requestPath = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(requestPath) && requestPath.StartsWith("/Admin", StringComparison.Ordinal))
    {
        var remainingPath = requestPath["/Admin".Length..];
        var destination = $"/admin{remainingPath}{context.Request.QueryString}";
        context.Response.Redirect(destination, permanent: true);
        return;
    }

    await next();
});

app.UseRouting();
app.UseCors("FrontendApp");
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    var startedAt = DateTime.UtcNow;
    var sw = System.Diagnostics.Stopwatch.StartNew();

    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("X-XSS-Protection", "0");
    context.Response.Headers.TryAdd("X-Trace-Id", context.TraceIdentifier);

    if (!isDevelopment)
    {
        context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self' https: data: 'unsafe-inline' 'unsafe-eval'; img-src 'self' https: data:; font-src 'self' https: data:; connect-src 'self' https: wss:;");
    }

    await next();

    sw.Stop();
    app.Logger.LogInformation(
        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms at {StartedAtUtc}. TraceId={TraceId}",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        startedAt,
        context.TraceIdentifier);
});
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<LeadHub>(LeadHub.HubPath);
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
    await DatabaseCompatibility.AdoptLegacyDatabaseAsync(db);
    await db.Database.MigrateAsync();
    await DatabaseCompatibility.EnsureSchemaCompatibilityAsync(db);
    await ApplicationDbSeeder.SeedAsync(db);
}

app.Run();

static void ValidateCriticalConfiguration(IConfiguration configuration, bool isProduction)
{
    var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
    {
        throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
    }

    if (!isProduction)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32 || jwt.Key.Contains("DEMO_KEY", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Jwt:Key must be configured with a strong production secret that is at least 32 characters long.");
    }

    var databaseProvider = configuration[$"{DatabaseOptions.SectionName}:Provider"];
    if (!string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Production mode requires Database:Provider to be set to SqlServer.");
    }

    var defaultConnection = configuration.GetConnectionString("DefaultConnection")
        ?? configuration.GetConnectionString("SqlServerConnection");
    if (string.IsNullOrWhiteSpace(defaultConnection))
    {
        throw new InvalidOperationException("A SQL Server connection string is required in production.");
    }

    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("At least one Cors:AllowedOrigins entry is required in production.");
    }
}
