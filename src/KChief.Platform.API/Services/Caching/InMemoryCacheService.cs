using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;

namespace KChief.Platform.API.Services.Caching;

/// <summary>
/// In-memory cache service implementation using IMemoryCache.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(
        IMemoryCache memoryCache,
        IOptions<CacheOptions> options,
        ILogger<InMemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Get"))
        {
            try
            {
                if (_memoryCache.TryGetValue(fullKey, out var cachedValue) && cachedValue is T typedValue)
                {
                    Log.Debug("Cache hit for key {CacheKey}", fullKey);
                    return Task.FromResult<T?>(typedValue);
                }

                Log.Debug("Cache miss for key {CacheKey}", fullKey);
                return Task.FromResult<T?>(null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving value from cache for key {CacheKey}", fullKey);
                return Task.FromResult<T?>(null);
            }
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        var expirationTime = expiration ?? _options.DefaultExpiration;
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expirationTime,
            SlidingExpiration = TimeSpan.FromMinutes(1), // Reset expiration if accessed
            Priority = CacheItemPriority.Normal
        };

        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Set"))
        using (LogContext.PushProperty("ExpirationSeconds", expirationTime.TotalSeconds))
        {
            try
            {
                _memoryCache.Set(fullKey, value, cacheEntryOptions);
                Log.Debug("Value cached for key {CacheKey} with expiration {ExpirationSeconds}s", fullKey, expirationTime.TotalSeconds);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting value in cache for key {CacheKey}", fullKey);
                return Task.CompletedTask;
            }
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Remove"))
        {
            try
            {
                _memoryCache.Remove(fullKey);
                Log.Debug("Cache entry removed for key {CacheKey}", fullKey);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing value from cache for key {CacheKey}", fullKey);
                return Task.CompletedTask;
            }
        }
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: IMemoryCache doesn't support pattern-based removal natively
        // This is a limitation of in-memory cache - would need to track keys
        // For production, use distributed cache (Redis) for pattern-based operations
        Log.Warning("Pattern-based removal not fully supported in in-memory cache. Pattern: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var exists = _memoryCache.TryGetValue(fullKey, out _);
        return Task.FromResult(exists);
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

            return value;
        }
    }

    public Task RefreshAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        
        using (LogContext.PushProperty("CacheKey", fullKey))
        using (LogContext.PushProperty("Operation", "Refresh"))
        {
            // For in-memory cache, refresh means re-setting with new expiration
            if (_memoryCache.TryGetValue(fullKey, out var value))
            {
                var expirationTime = expiration ?? _options.DefaultExpiration;
                return SetAsync(fullKey, value, expirationTime, cancellationToken);
            }

            Log.Debug("Cannot refresh non-existent cache key {CacheKey}", fullKey);
            return Task.CompletedTask;
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

