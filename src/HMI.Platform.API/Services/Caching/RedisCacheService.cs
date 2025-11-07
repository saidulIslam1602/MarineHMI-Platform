using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Serilog;
using Serilog.Context;
using HMI.Platform.Core.Interfaces;
using HMI.Platform.Core.Models;

namespace HMI.Platform.API.Services.Caching;

/// <summary>
/// Redis distributed cache service implementation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Get"))
        {
            try
            {
                var cachedBytes = await _distributedCache.GetAsync(fullKey, cancellationToken);
                
                if (cachedBytes == null || cachedBytes.Length == 0)
                {
                    Log.Debug("Cache miss for key {CacheKey}", fullKey);
                    return null;
                }

                var json = System.Text.Encoding.UTF8.GetString(cachedBytes);
                var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                Log.Debug("Cache hit for key {CacheKey}", fullKey);
                return value;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving value from Redis cache for key {CacheKey}", fullKey);
                return null;
            }
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        var expirationTime = expiration ?? _options.DefaultExpiration;
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Set"))
        using (LogContext.PushProperty("ExpirationSeconds", expirationTime.TotalSeconds))
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime,
                    SlidingExpiration = TimeSpan.FromMinutes(1)
                };

                await _distributedCache.SetAsync(fullKey, bytes, options, cancellationToken);
                Log.Debug("Value cached in Redis for key {CacheKey} with expiration {ExpirationSeconds}s", fullKey, expirationTime.TotalSeconds);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting value in Redis cache for key {CacheKey}", fullKey);
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Remove"))
        {
            try
            {
                await _distributedCache.RemoveAsync(fullKey, cancellationToken);
                Log.Debug("Cache entry removed from Redis for key {CacheKey}", fullKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing value from Redis cache for key {CacheKey}", fullKey);
            }
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: IDistributedCache doesn't support pattern-based removal directly
        // This would require Redis-specific implementation using StackExchange.Redis
        // For now, log a warning - full implementation would need Redis connection
        Log.Warning("Pattern-based removal requires Redis-specific implementation. Pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var cachedBytes = await _distributedCache.GetAsync(fullKey, cancellationToken);
        return cachedBytes != null && cachedBytes.Length > 0;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "GetOrSet"))
        {
            // Try to get from cache first
            var cachedValue = await GetAsync<T>(fullKey, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Cache miss - execute factory and cache result
            Log.Debug("Cache miss for key {CacheKey}, executing factory", fullKey);
            var value = await factory();
            
            if (value != null)
            {
                await SetAsync(fullKey, value, expiration, cancellationToken);
            }

            return value!;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Refresh"))
        {
            try
            {
                var cachedBytes = await _distributedCache.GetAsync(fullKey, cancellationToken);
                
                if (cachedBytes != null && cachedBytes.Length > 0)
                {
                    var expirationTime = expiration ?? _options.DefaultExpiration;
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expirationTime,
                        SlidingExpiration = TimeSpan.FromMinutes(1)
                    };

                    await _distributedCache.SetAsync(fullKey, cachedBytes, options, cancellationToken);
                    Log.Debug("Cache entry refreshed for key {CacheKey}", fullKey);
                }
                else
                {
                    Log.Debug("Cannot refresh non-existent cache key {CacheKey}", fullKey);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error refreshing cache key {CacheKey}", fullKey);
            }
        }
    }

    private string GetFullKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }

        return $"{_options.KeyPrefix}{key}";
    }
}

