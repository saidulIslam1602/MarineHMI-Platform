using System.Diagnostics;
using KChief.Platform.API.Services;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for monitoring HTTP request performance and metrics.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, PerformanceMonitoringService performanceService)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        
        try
        {
            // Add correlation ID for request tracking
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            context.Items["CorrelationId"] = correlationId;
            
            // Add correlation ID to response headers
            context.Response.Headers.Append("X-Correlation-ID", correlationId);
            
            _logger.LogDebug("Request started: {Method} {Path} [CorrelationId: {CorrelationId}]", 
                method, requestPath, correlationId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: {Method} {Path}", method, requestPath);
            context.Response.StatusCode = 500;
            
            if (!context.Response.HasStarted)
            {
                await context.Response.WriteAsync("Internal Server Error");
            }
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

            // Record metrics
            performanceService.RecordHttpRequest(method, requestPath, statusCode, duration);

            // Log request completion
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning :
                          duration > 5.0 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel,
                "Request completed: {Method} {Path} {StatusCode} in {Duration:F3}s [CorrelationId: {CorrelationId}]",
                method, requestPath, statusCode, duration, correlationId);

            // Log slow requests
            if (duration > 1.0)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration:F3}s [CorrelationId: {CorrelationId}]",
                    method, requestPath, duration, correlationId);
            }
        }
    }
}
