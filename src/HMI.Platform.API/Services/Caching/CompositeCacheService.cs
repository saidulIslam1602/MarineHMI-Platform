using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using HMI.Platform.Core.Interfaces;
using HMI.Platform.Core.Models;

namespace HMI.Platform.API.Services.Caching;

/// <summary>
/// Composite cache service that uses in-memory cache as primary and Redis as secondary (if available).
/// Implements cache-aside pattern with fallback.
/// </summary>
public class CompositeCacheService : ICacheService
{
    private readonly ICacheService _primaryCache;
    private readonly ICacheService? _secondaryCache;
    private readonly CacheOptions _options;
    private readonly ILogger<CompositeCacheService> _logger;

    public CompositeCacheService(
        ICacheService primaryCache,
        ICacheService? secondaryCache,
        IOptions<CacheOptions> options,
        ILogger<CompositeCacheService> logger)
    {
        _primaryCache = primaryCache ?? throw new ArgumentNullException(nameof(primaryCache));
        _secondaryCache = secondaryCache;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        using (LogContext.PushProperty("CacheKey", key))
        using (LogContext.PushProperty("Operation", "Get"))
        {
            // Try primary cache first (fastest)
            var value = await _primaryCache.GetAsync<T>(key, cancellationToken);
            if (value != null)
            {
                Log.Debug("Cache hit in primary cache for key {CacheKey}", key);
                return value;
            }

            // Try secondary cache (distributed)
            if (_secondaryCache != null)
            {
                value = await _secondaryCache.GetAsync<T>(key, cancellationToken);
                if (value != null)
                {
                    Log.Debug("Cache hit in secondary cache for key {CacheKey}, promoting to primary", key);
                    // Promote to primary cache for faster access next time
                    await _primaryCache.SetAsync(key, value, TimeSpan.FromMinutes(1), cancellationToken);
                    return value;
                }
            }

            Log.Debug("Cache miss for key {CacheKey}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        using (LogContext.PushProperty("CacheKey", key))
        using (LogContext.PushProperty("Operation", "Set"))
        {
            // Set in both caches
            await _primaryCache.SetAsync(key, value, expiration, cancellationToken);
            
            if (_secondaryCache != null)
            {
                await _secondaryCache.SetAsync(key, value, expiration, cancellationToken);
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("CacheKey", key))
        using (LogContext.PushProperty("Operation", "Remove"))
        {
            await _primaryCache.RemoveAsync(key, cancellationToken);
            
            if (_secondaryCache != null)
            {
                await _secondaryCache.RemoveAsync(key, cancellationToken);
            }
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Pattern", pattern))
        using (LogContext.PushProperty("Operation", "RemoveByPattern"))
        {
            await _primaryCache.RemoveByPatternAsync(pattern, cancellationToken);
            
            if (_secondaryCache != null)
            {
                await _secondaryCache.RemoveByPatternAsync(pattern, cancellationToken);
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = await _primaryCache.ExistsAsync(key, cancellationToken);
        if (exists)
        {
            return true;
        }

        if (_secondaryCache != null)
        {
            return await _secondaryCache.ExistsAsync(key, cancellationToken);
        }

        return false;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Cache miss - execute factory
        var value = await factory();
        
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value!;
    }

    public async Task RefreshAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        await _primaryCache.RefreshAsync(key, expiration, cancellationToken);
        
        if (_secondaryCache != null)
        {
            await _secondaryCache.RefreshAsync(key, expiration, cancellationToken);
        }
    }
}

