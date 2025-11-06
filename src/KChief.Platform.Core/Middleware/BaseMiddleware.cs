using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace KChief.Platform.Core.Middleware;

/// <summary>
/// Base class for middleware with common functionality.
/// </summary>
public abstract class BaseMiddleware
{
    protected readonly RequestDelegate Next;
    protected readonly ILogger Logger;

    protected BaseMiddleware(RequestDelegate next, ILogger logger)
    {
        Next = next ?? throw new ArgumentNullException(nameof(next));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the correlation ID from the HTTP context.
    /// </summary>
    protected string GetCorrelationId(HttpContext context)
    {
        return context.Items["CorrelationId"]?.ToString() 
            ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>
    /// Gets the client IP address from the HTTP context.
    /// </summary>
    protected string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Creates a log context with common properties.
    /// </summary>
    protected IDisposable CreateLogContext(HttpContext context, string operation)
    {
        var correlationId = GetCorrelationId(context);
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        return LogContext.PushProperty("CorrelationId", correlationId)
            .PushProperty("Operation", operation)
            .PushProperty("Path", path)
            .PushProperty("Method", method);
    }

    /// <summary>
    /// Checks if the request should be processed by this middleware.
    /// </summary>
    protected virtual bool ShouldProcess(HttpContext context)
    {
        // Skip processing for health checks and static files
        var path = context.Request.Path.Value ?? string.Empty;
        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Handles exceptions in middleware.
    /// </summary>
    protected async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        using (CreateLogContext(context, "ErrorHandling"))
        {
            Logger.LogError(exception, "Error in middleware: {Path}", context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title = "Internal Server Error",
                    status = StatusCodes.Status500InternalServerError,
                    detail = "An error occurred while processing your request",
                    instance = context.Request.Path,
                    correlationId = GetCorrelationId(context),
                    timestamp = DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        }
    }
}

