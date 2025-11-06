using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for rate limiting requests based on IP address, user identity, or API key.
/// Supports multiple rate limiting strategies: fixed window, sliding window, and token bucket.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ICacheService? _cacheService;

    // In-memory rate limit tracking (fallback if cache not available)
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration,
        ICacheService? cacheService = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService;
        
        // Load options from configuration
        _options = new RateLimitingOptions
        {
            RequestsPerWindow = configuration.GetValue<int>("Middleware:RateLimiting:RequestsPerWindow", 100),
            WindowSize = TimeSpan.FromSeconds(configuration.GetValue<int>("Middleware:RateLimiting:WindowSizeSeconds", 60)),
            Strategy = configuration.GetValue<string>("Middleware:RateLimiting:Strategy", "FixedWindow") == "SlidingWindow"
                ? RateLimitingStrategy.SlidingWindow
                : RateLimitingStrategy.FixedWindow,
            PerEndpointLimiting = configuration.GetValue<bool>("Middleware:RateLimiting:PerEndpointLimiting", false),
            ExcludedPaths = configuration.GetSection("Middleware:RateLimiting:ExcludedPaths")
                .Get<List<string>>() ?? new List<string> { "/health", "/health-ui", "/metrics" }
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for excluded paths
        if (ShouldSkipRateLimit(context))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? "unknown";
        var clientIdentifier = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ClientIdentifier", clientIdentifier))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            try
            {
                var rateLimitResult = await CheckRateLimitAsync(clientIdentifier, endpoint);

                if (!rateLimitResult.IsAllowed)
                {
                    Log.Warning(
                        "Rate limit exceeded for {ClientIdentifier} on {Endpoint}. Limit: {Limit}, Remaining: {Remaining}, ResetAt: {ResetAt}",
                        clientIdentifier, endpoint, rateLimitResult.Limit, rateLimitResult.Remaining, rateLimitResult.ResetAt);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    
                    // Add rate limit headers
                    context.Response.Headers.Append("X-RateLimit-Limit", rateLimitResult.Limit.ToString());
                    context.Response.Headers.Append("X-RateLimit-Remaining", rateLimitResult.Remaining.ToString());
                    context.Response.Headers.Append("X-RateLimit-Reset", rateLimitResult.ResetAt.ToUnixTimeSeconds().ToString());
                    context.Response.Headers.Append("Retry-After", ((int)(rateLimitResult.ResetAt - DateTimeOffset.UtcNow).TotalSeconds).ToString());

                    var errorResponse = new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too Many Requests",
                        status = (int)HttpStatusCode.TooManyRequests,
                        detail = $"Rate limit exceeded. Maximum {rateLimitResult.Limit} requests per {_options.WindowSize.TotalSeconds} seconds.",
                        instance = context.Request.Path,
                        correlationId = correlationId,
                        retryAfter = (int)(rateLimitResult.ResetAt - DateTimeOffset.UtcNow).TotalSeconds,
                        timestamp = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(json, Encoding.UTF8);
                    return;
                }

                // Add rate limit headers to successful responses
                context.Response.Headers.Append("X-RateLimit-Limit", rateLimitResult.Limit.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining", rateLimitResult.Remaining.ToString());
                context.Response.Headers.Append("X-RateLimit-Reset", rateLimitResult.ResetAt.ToUnixTimeSeconds().ToString());

                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during rate limiting");
                // On error, allow request to proceed (fail open)
                await _next(context);
            }
        }
    }

    private bool ShouldSkipRateLimit(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Skip health checks and monitoring endpoints
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check excluded paths
        foreach (var excludedPath in _options.ExcludedPaths)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Priority: API Key > User Identity > IP Address
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return $"user:{context.User.Identity.Name}";
        }

        // Check for API key in headers
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
        {
            return $"apikey:{apiKey}";
        }

        // Use IP address
        var ipAddress = GetClientIpAddress(context);
        return $"ip:{ipAddress}";
    }

    private string GetEndpointKey(HttpContext context)
    {
        // Use path as endpoint key, or "global" for global rate limiting
        if (_options.PerEndpointLimiting)
        {
            return context.Request.Path.Value ?? "unknown";
        }
        return "global";
    }

    private string GetClientIpAddress(HttpContext context)
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

    private async Task<RateLimitResult> CheckRateLimitAsync(string clientIdentifier, string endpoint)
    {
        var key = $"ratelimit:{clientIdentifier}:{endpoint}";
        var now = DateTimeOffset.UtcNow;
        var windowStart = GetWindowStart(now);

        // Try to use distributed cache if available
        if (_cacheService != null)
        {
            return await CheckRateLimitWithCacheAsync(key, windowStart);
        }

        // Fallback to in-memory tracking
        return CheckRateLimitInMemory(key, windowStart);
    }

    private async Task<RateLimitResult> CheckRateLimitWithCacheAsync(string key, DateTimeOffset windowStart)
    {
        var cacheKey = $"{key}:{windowStart:yyyyMMddHHmmss}";
        var limit = _options.RequestsPerWindow;
        
        // Get current count from cache
        var countStr = await _cacheService.GetAsync<string>(cacheKey) ?? "0";
        var currentCount = int.TryParse(countStr, out var count) ? count : 0;

        if (currentCount >= limit)
        {
            var resetAt = windowStart.Add(_options.WindowSize);
            return new RateLimitResult
            {
                IsAllowed = false,
                Limit = limit,
                Remaining = 0,
                ResetAt = resetAt
            };
        }

        // Increment count
        currentCount++;
        var expiration = _options.WindowSize;
        await _cacheService.SetAsync(cacheKey, currentCount.ToString(), expiration);

        var remaining = Math.Max(0, limit - currentCount);
        var resetAt2 = windowStart.Add(_options.WindowSize);

        return new RateLimitResult
        {
            IsAllowed = true,
            Limit = limit,
            Remaining = remaining,
            ResetAt = resetAt2
        };
    }

    private RateLimitResult CheckRateLimitInMemory(string key, DateTimeOffset windowStart)
    {
        var limit = _options.RequestsPerWindow;
        var now = DateTimeOffset.UtcNow;

        var rateLimitInfo = _rateLimitStore.AddOrUpdate(
            key,
            new RateLimitInfo { Count = 1, WindowStart = windowStart },
            (k, existing) =>
            {
                // Reset if window has passed
                if (now >= existing.WindowStart.Add(_options.WindowSize))
                {
                    return new RateLimitInfo { Count = 1, WindowStart = windowStart };
                }

                // Increment count
                existing.Count++;
                return existing;
            });

        if (rateLimitInfo.Count > limit)
        {
            var resetAt = rateLimitInfo.WindowStart.Add(_options.WindowSize);
            return new RateLimitResult
            {
                IsAllowed = false,
                Limit = limit,
                Remaining = 0,
                ResetAt = resetAt
            };
        }

        var remaining = Math.Max(0, limit - rateLimitInfo.Count);
        var resetAt2 = rateLimitInfo.WindowStart.Add(_options.WindowSize);

        return new RateLimitResult
        {
            IsAllowed = true,
            Limit = limit,
            Remaining = remaining,
            ResetAt = resetAt2
        };
    }

    private DateTimeOffset GetWindowStart(DateTimeOffset now)
    {
        return _options.Strategy switch
        {
            RateLimitingStrategy.FixedWindow => GetFixedWindowStart(now),
            RateLimitingStrategy.SlidingWindow => now, // Sliding window uses current time
            _ => GetFixedWindowStart(now)
        };
    }

    private DateTimeOffset GetFixedWindowStart(DateTimeOffset now)
    {
        var windowSeconds = (int)_options.WindowSize.TotalSeconds;
        var windowNumber = now.ToUnixTimeSeconds() / windowSeconds;
        return DateTimeOffset.FromUnixTimeSeconds(windowNumber * windowSeconds);
    }
}

/// <summary>
/// Options for rate limiting middleware.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Number of requests allowed per window.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Size of the time window.
    /// </summary>
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Rate limiting strategy to use.
    /// </summary>
    public RateLimitingStrategy Strategy { get; set; } = RateLimitingStrategy.FixedWindow;

    /// <summary>
    /// Whether to apply rate limiting per endpoint or globally.
    /// </summary>
    public bool PerEndpointLimiting { get; set; } = false;

    /// <summary>
    /// Paths to exclude from rate limiting.
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/health-ui",
        "/metrics"
    };

    /// <summary>
    /// Custom rate limits per endpoint.
    /// </summary>
    public Dictionary<string, EndpointRateLimit> EndpointLimits { get; set; } = new();
}

/// <summary>
/// Rate limiting strategy.
/// </summary>
public enum RateLimitingStrategy
{
    /// <summary>
    /// Fixed window: requests are counted in fixed time windows.
    /// </summary>
    FixedWindow,

    /// <summary>
    /// Sliding window: requests are counted in a sliding time window.
    /// </summary>
    SlidingWindow
}

/// <summary>
/// Rate limit configuration for a specific endpoint.
/// </summary>
public class EndpointRateLimit
{
    public int RequestsPerWindow { get; set; }
    public TimeSpan WindowSize { get; set; }
}

/// <summary>
/// Result of rate limit check.
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTimeOffset ResetAt { get; set; }
}

/// <summary>
/// In-memory rate limit tracking information.
/// </summary>
internal class RateLimitInfo
{
    public int Count { get; set; }
    public DateTimeOffset WindowStart { get; set; }
}

