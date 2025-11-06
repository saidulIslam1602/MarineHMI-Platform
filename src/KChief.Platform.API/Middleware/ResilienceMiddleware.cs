using Polly;
using Polly.CircuitBreaker;
using Polly.Bulkhead;
using Polly.Timeout;
using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace KChief.Platform.API.Middleware;

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
    private static readonly IAsyncPolicy _apiCircuitBreaker;
    
    // Bulkhead for request isolation
    private static readonly IAsyncPolicy _requestBulkhead;
    
    static ResilienceMiddleware()
    {
        // Initialize static policies
        _apiCircuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 10,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    Log.Error("API Circuit breaker opened for {DurationMinutes} minutes due to: {ExceptionMessage}",
                        duration.TotalMinutes, exception.Message);
                },
                onReset: () =>
                {
                    Log.Information("API Circuit breaker reset - service is healthy again");
                });

        _requestBulkhead = Policy.BulkheadAsync(
            maxParallelization: 100,
            maxQueuingActions: 200,
            onBulkheadRejected: () =>
            {
                Log.Warning("Request rejected by bulkhead - system is at capacity");
            });
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
                // Apply resilience patterns based on request type
                var resiliencePolicy = GetResiliencePolicyForRequest(context);
                
                await resiliencePolicy.ExecuteAsync(async () =>
                {
                    await _next(context);
                });
                
                stopwatch.Stop();
                
                // Log successful request
                Log.Debug("Request completed successfully in {ElapsedMs}ms: {RequestMethod} {RequestPath}",
                    stopwatch.ElapsedMilliseconds, requestMethod, requestPath);
            }
            catch (CircuitBreakerOpenException)
            {
                stopwatch.Stop();
                Log.Warning("Request rejected by circuit breaker: {RequestMethod} {RequestPath}",
                    requestMethod, requestPath);
                
                context.Response.StatusCode = 503; // Service Unavailable
                await context.Response.WriteAsync("Service temporarily unavailable due to circuit breaker");
            }
            catch (BulkheadRejectedException)
            {
                stopwatch.Stop();
                Log.Warning("Request rejected by bulkhead: {RequestMethod} {RequestPath}",
                    requestMethod, requestPath);
                
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Service at capacity, please try again later");
            }
            catch (TimeoutRejectedException)
            {
                stopwatch.Stop();
                Log.Warning("Request timed out: {RequestMethod} {RequestPath}",
                    requestMethod, requestPath);
                
                context.Response.StatusCode = 408; // Request Timeout
                await context.Response.WriteAsync("Request timed out");
            }
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

    /// <summary>
    /// Gets the appropriate resilience policy based on the request characteristics.
    /// </summary>
    private IAsyncPolicy GetResiliencePolicyForRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();
        
        // Different timeout policies based on request type
        var timeoutPolicy = GetTimeoutPolicyForRequest(path, method);
        
        // Critical operations get stricter policies
        if (IsCriticalOperation(path, method))
        {
            return Policy.WrapAsync(
                timeoutPolicy,
                _apiCircuitBreaker,
                _requestBulkhead
            );
        }
        
        // Health checks and monitoring endpoints get minimal resilience
        if (IsMonitoringEndpoint(path))
        {
            return Policy.TimeoutAsync(TimeSpan.FromSeconds(5));
        }
        
        // Authentication endpoints get moderate resilience
        if (IsAuthenticationEndpoint(path))
        {
            var authTimeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(10));
            var authBulkheadPolicy = Policy.BulkheadAsync(50, 100);
            
            return Policy.WrapAsync(authTimeoutPolicy, authBulkheadPolicy);
        }
        
        // Default policy for regular operations
        return Policy.WrapAsync(timeoutPolicy, _requestBulkhead);
    }

    /// <summary>
    /// Gets timeout policy based on request characteristics.
    /// </summary>
    private IAsyncPolicy GetTimeoutPolicyForRequest(string path, string method)
    {
        var timeout = method switch
        {
            "GET" => TimeSpan.FromSeconds(30),
            "POST" => TimeSpan.FromSeconds(60),
            "PUT" => TimeSpan.FromSeconds(60),
            "DELETE" => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(30)
        };

        // Adjust timeout based on path
        if (path.Contains("/vessels") && method == "POST")
        {
            timeout = TimeSpan.FromSeconds(90); // Vessel creation might take longer
        }
        else if (path.Contains("/engines") && (method == "POST" || method == "PUT"))
        {
            timeout = TimeSpan.FromSeconds(45); // Engine operations
        }
        else if (path.Contains("/alarms"))
        {
            timeout = TimeSpan.FromSeconds(15); // Alarms should be fast
        }

        return Policy.TimeoutAsync(
            timeout,
            TimeoutStrategy.Optimistic,
            onTimeout: (context, timespan, task) =>
            {
                Log.Warning("Request timeout after {TimeoutSeconds}s for {RequestPath}",
                    timespan.TotalSeconds, path);
                return Task.CompletedTask;
            });
    }

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
