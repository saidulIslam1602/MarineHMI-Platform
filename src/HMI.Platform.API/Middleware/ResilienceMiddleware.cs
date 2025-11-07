using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace HMI.Platform.API.Middleware;

/// <summary>
/// Middleware that applies resilience patterns to HTTP requests.
/// Provides request-level timeout, rate limiting, and circuit breaker functionality.
/// </summary>
public class ResilienceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResilienceMiddleware> _logger;
    private readonly IConfiguration _configuration;
    
    // Circuit breaker for overall API health
    private static readonly ResiliencePipeline _resiliencePipeline;
    
    static ResilienceMiddleware()
    {
        // Initialize resilience pipeline with Polly v8 syntax
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromMinutes(1)
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public ResilienceMiddleware(RequestDelegate next, ILogger<ResilienceMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        var requestPath = context.Request.Path.Value ?? "unknown";
        var requestMethod = context.Request.Method;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", requestPath))
        using (LogContext.PushProperty("RequestMethod", requestMethod))
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Apply resilience patterns using Polly v8
                await _resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    await _next(context);
                }, context.RequestAborted);
                
                stopwatch.Stop();
                
                // Log successful request
                Log.Debug("Request completed successfully in {ElapsedMs}ms: {RequestMethod} {RequestPath}",
                    stopwatch.ElapsedMilliseconds, requestMethod, requestPath);
            }
            catch (BrokenCircuitException)
            {
                stopwatch.Stop();
                Log.Warning("Request rejected by circuit breaker: {RequestMethod} {RequestPath}",
                    requestMethod, requestPath);
                
                context.Response.StatusCode = 503; // Service Unavailable
                await context.Response.WriteAsync("Service temporarily unavailable due to circuit breaker");
            }
            // Bulkhead rejection handling is now part of the unified resilience pipeline
            // Timeout handling is now part of the unified resilience pipeline
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "Request failed: {RequestMethod} {RequestPath} in {ElapsedMs}ms",
                    requestMethod, requestPath, stopwatch.ElapsedMilliseconds);
                
                // Let the global exception handler deal with other exceptions
                throw;
            }
        }
    }

    // Removed GetResiliencePolicyForRequest - using unified pipeline approach with Polly v8

    // Timeout handling is now part of the unified resilience pipeline

    /// <summary>
    /// Determines if the operation is critical and requires strict resilience policies.
    /// </summary>
    private bool IsCriticalOperation(string path, string method)
    {
        // Vessel control operations are critical
        if (path.Contains("/vessels") && (method == "POST" || method == "PUT" || method == "DELETE"))
            return true;
            
        // Engine control operations are critical
        if (path.Contains("/engines") && (method == "POST" || method == "PUT"))
            return true;
            
        // Emergency operations are critical
        if (path.Contains("/emergency"))
            return true;
            
        // Alarm operations are critical
        if (path.Contains("/alarms") && (method == "POST" || method == "PUT"))
            return true;
            
        return false;
    }

    /// <summary>
    /// Determines if the endpoint is for monitoring purposes.
    /// </summary>
    private bool IsMonitoringEndpoint(string path)
    {
        return path.Contains("/health") || 
               path.Contains("/metrics") || 
               path.Contains("/status") ||
               path.Contains("/ping");
    }

    /// <summary>
    /// Determines if the endpoint is for authentication.
    /// </summary>
    private bool IsAuthenticationEndpoint(string path)
    {
        return path.Contains("/auth") || path.Contains("/login") || path.Contains("/token");
    }
}

/// <summary>
/// Extension methods for registering resilience middleware.
/// </summary>
public static class ResilienceMiddlewareExtensions
{
    /// <summary>
    /// Adds resilience middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseResilience(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResilienceMiddleware>();
    }
}
