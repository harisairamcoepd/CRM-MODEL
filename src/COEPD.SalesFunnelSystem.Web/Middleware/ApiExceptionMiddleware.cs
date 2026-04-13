using COEPD.SalesFunnelSystem.Application.Common;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace COEPD.SalesFunnelSystem.Web.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

            var (statusCode, message) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, ex.Message),
                InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var payload = new ProblemDetails
            {
                Status = statusCode,
                Title = message,
                Detail = _environment.IsDevelopment() ? ex.Message : null,
                Instance = context.Request.Path.ToString()
            };
            payload.Extensions["success"] = false;
            payload.Extensions["traceId"] = traceId;

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
