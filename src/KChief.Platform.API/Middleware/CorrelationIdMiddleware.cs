using KChief.Platform.Core.Middleware;
using KChief.Platform.Core.Utilities;
using Serilog.Context;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for tracking across the application.
/// </summary>
public class CorrelationIdMiddleware : BaseMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        : base(next, logger)
    {
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldProcess(context))
        {
            await Next(context);
            return;
        }

        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);
        
        // Store in HttpContext for other middleware and services
        context.Items[CorrelationIdKey] = correlationId;
        
        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
        
        // Push to Serilog LogContext for structured logging
        using (CreateLogContext(context, "CorrelationId"))
        {
            Logger.LogDebug("Processing request {Method} {Path} with correlation ID {CorrelationId}", 
                context.Request.Method, context.Request.Path, correlationId);
            
            await Next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check if correlation ID is provided in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue.FirstOrDefault()))
        {
            return headerValue.First()!;
        }

        // Check for other common correlation ID headers
        var alternativeHeaders = new[] { "X-Request-ID", "X-Trace-ID", "Request-ID" };
        foreach (var header in alternativeHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var altHeaderValue) &&
                !string.IsNullOrWhiteSpace(altHeaderValue.FirstOrDefault()))
            {
                return altHeaderValue.First()!;
            }
        }

        // Generate new correlation ID using utility
        return IdGenerator.GenerateCorrelationId();
    }
}
