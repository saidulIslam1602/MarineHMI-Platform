namespace KChief.Platform.Core.Models;

/// <summary>
/// Configuration options for caching.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Expiration time for frequently accessed data.
    /// </summary>
    public TimeSpan FrequentDataExpiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Expiration time for rarely changing data.
    /// </summary>
    public TimeSpan RareDataExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Expiration time for real-time data (shorter TTL).
    /// </summary>
    public TimeSpan RealTimeDataExpiration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of entries in in-memory cache.
    /// </summary>
    public int InMemoryCacheSizeLimit { get; set; } = 1000;

    /// <summary>
    /// Redis connection string (if using distributed caching).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Whether to use distributed caching (Redis).
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;

    /// <summary>
    /// Cache key prefix for namespacing.
    /// </summary>
    public string KeyPrefix { get; set; } = "kchief:";

    /// <summary>
    /// Entity-specific cache expiration times.
    /// </summary>
    public Dictionary<string, TimeSpan> EntityExpirations { get; set; } = new();
}

