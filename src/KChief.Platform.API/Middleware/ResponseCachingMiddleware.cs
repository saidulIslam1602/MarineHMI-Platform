using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Serilog.Context;
using System.Text;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for HTTP response caching with configurable policies.
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly ResponseCachingOptions _options;
    private readonly IResponseCache _cache;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        ILogger<ResponseCachingMiddleware> logger,
        IResponseCache cache,
        ResponseCachingOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? new ResponseCachingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip caching for non-GET requests
        if (context.Request.Method != "GET" && context.Request.Method != "HEAD")
        {
            await _next(context);
            return;
        }

        // Skip caching for authenticated requests (unless explicitly allowed)
        if (context.User.Identity?.IsAuthenticated == true && !_options.AllowCachingForAuthenticatedUsers)
        {
            await _next(context);
            return;
        }

        // Generate cache key
        var cacheKey = GenerateCacheKey(context);

        using (LogContext.PushProperty("CacheKey", cacheKey))
        using (LogContext.PushProperty("Path", context.Request.Path))
        {
            // Try to get from cache
            var cachedResponse = await _cache.GetAsync(cacheKey);
            if (cachedResponse != null)
            {
                Log.Debug("Response cache hit for {Path}", context.Request.Path);
                
                // Set response headers from cache
                context.Response.StatusCode = cachedResponse.StatusCode;
                foreach (var header in cachedResponse.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value;
                }

                // Write cached body
                await context.Response.Body.WriteAsync(cachedResponse.Body, 0, cachedResponse.Body.Length);
                return;
            }

            Log.Debug("Response cache miss for {Path}", context.Request.Path);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Cache successful GET responses
            if (context.Response.StatusCode == 200 && 
                context.Request.Method == "GET" &&
                ShouldCacheResponse(context))
            {
                var responseBodyBytes = responseBody.ToArray();
                var cacheEntry = new CachedResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    Body = responseBodyBytes,
                    CachedAt = DateTime.UtcNow
                };

                var cacheDuration = GetCacheDuration(context);
                await _cache.SetAsync(cacheKey, cacheEntry, cacheDuration);
                
                Log.Debug("Response cached for {Path} with duration {Duration}s", 
                    context.Request.Path, cacheDuration.TotalSeconds);
            }

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(context.Request.Path);
        
        // Include query string in cache key
        if (context.Request.QueryString.HasValue)
        {
            keyBuilder.Append(context.Request.QueryString.Value);
        }

        // Include user identity if authenticated (for user-specific caching)
        if (context.User.Identity?.IsAuthenticated == true && _options.CachePerUser)
        {
            keyBuilder.Append($":user:{context.User.Identity.Name}");
        }

        return $"response:{keyBuilder}";
    }

    private bool ShouldCacheResponse(HttpContext context)
    {
        // Don't cache if response has no-cache headers
        if (context.Response.Headers.ContainsKey("Cache-Control") &&
            context.Response.Headers["Cache-Control"].ToString().Contains("no-cache"))
        {
            return false;
        }

        // Don't cache large responses
        if (context.Response.ContentLength > _options.MaxResponseSize)
        {
            return false;
        }

        return true;
    }

    private TimeSpan GetCacheDuration(HttpContext context)
    {
        // Check for Cache-Control header
        if (context.Response.Headers.TryGetValue("Cache-Control", out var cacheControl))
        {
            var maxAgeMatch = System.Text.RegularExpressions.Regex.Match(
                cacheControl.ToString(), 
                @"max-age=(\d+)");
            
            if (maxAgeMatch.Success && int.TryParse(maxAgeMatch.Groups[1].Value, out var maxAge))
            {
                return TimeSpan.FromSeconds(maxAge);
            }
        }

        // Default cache duration based on path
        if (context.Request.Path.StartsWithSegments("/api/vessels"))
        {
            return TimeSpan.FromMinutes(5); // Vessel data changes less frequently
        }

        if (context.Request.Path.StartsWithSegments("/api/engines"))
        {
            return TimeSpan.FromSeconds(30); // Engine data changes more frequently
        }

        return _options.DefaultCacheDuration;
    }
}

/// <summary>
/// Options for response caching middleware.
/// </summary>
public class ResponseCachingOptions
{
    /// <summary>
    /// Default cache duration for responses.
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum response size to cache (in bytes).
    /// </summary>
    public long MaxResponseSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Whether to cache responses for authenticated users.
    /// </summary>
    public bool AllowCachingForAuthenticatedUsers { get; set; } = false;

    /// <summary>
    /// Whether to create separate cache entries per user.
    /// </summary>
    public bool CachePerUser { get; set; } = false;
}

/// <summary>
/// Cached response data.
/// </summary>
public class CachedResponse
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public byte[] Body { get; set; } = Array.Empty<byte>();
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// Interface for response cache storage.
/// </summary>
public interface IResponseCache
{
    Task<CachedResponse?> GetAsync(string key);
    Task SetAsync(string key, CachedResponse response, TimeSpan expiration);
    Task RemoveAsync(string key);
}

/// <summary>
/// In-memory implementation of response cache.
/// </summary>
public class InMemoryResponseCache : IResponseCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryResponseCache> _logger;

    public InMemoryResponseCache(
        IMemoryCache memoryCache,
        ILogger<InMemoryResponseCache> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<CachedResponse?> GetAsync(string key)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is CachedResponse response)
        {
            return Task.FromResult<CachedResponse?>(response);
        }
        return Task.FromResult<CachedResponse?>(null);
    }

    public Task SetAsync(string key, CachedResponse response, TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        _memoryCache.Set(key, response, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}

