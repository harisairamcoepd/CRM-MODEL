using COEPD.SalesFunnelSystem.Application.Common;
using System.Text.Json;

namespace COEPD.SalesFunnelSystem.Web.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                success = false,
                message,
                traceId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
