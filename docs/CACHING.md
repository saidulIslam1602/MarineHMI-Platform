# Caching and Performance Optimization

## Overview

The K-Chief Marine Automation Platform implements comprehensive caching strategies to optimize performance, reduce database load, and improve response times. The caching system supports both in-memory and distributed caching (Redis) with intelligent cache invalidation strategies.

## Architecture

### Caching Layers

```
┌─────────────────────────────────────────┐
│         Application Layer                │
│  ┌──────────────┐  ┌──────────────┐     │
│  │ Controllers  │  │  Services    │     │
│  └──────┬───────┘  └──────┬───────┘     │
│         │                 │              │
│  ┌──────▼─────────────────▼──────┐     │
│  │   Cached Repositories          │     │
│  └──────┬─────────────────────────┘     │
│         │                                │
└─────────┼────────────────────────────────┘
          │
┌─────────▼────────────────────────────────┐
│         Caching Layer                      │
│  ┌──────────────┐  ┌──────────────┐     │
│  │ In-Memory    │  │    Redis     │     │
│  │   Cache      │  │  (Optional)  │     │
│  └──────────────┘  └──────────────┘     │
└───────────────────────────────────────────┘
          │
┌─────────▼────────────────────────────────┐
│      Data Access Layer                     │
│  ┌──────────────┐  ┌──────────────┐     │
│  │ Repositories │  │  Database   │     │
│  └──────────────┘  └──────────────┘     │
└───────────────────────────────────────────┘
```

## Caching Strategies

### 1. In-Memory Caching

Fast, local caching using `IMemoryCache` for frequently accessed data within a single application instance.

**Use Cases:**
- Frequently accessed read-only data
- User session data
- Configuration data
- Computed results

**Configuration:**
```json
{
  "Caching": {
    "InMemoryCacheSizeLimit": 1000,
    "DefaultExpiration": "00:05:00"
  }
}
```

### 2. Distributed Caching (Redis)

Shared cache across multiple application instances for scalability and consistency.

**Use Cases:**
- Multi-instance deployments
- Shared session data
- Cross-service caching
- High-availability scenarios

**Configuration:**
```json
{
  "Caching": {
    "UseDistributedCache": true,
    "RedisConnectionString": "localhost:6379"
  }
}
```

### 3. Composite Caching

Combines in-memory and distributed caching for optimal performance:
- Primary: In-memory cache (fastest access)
- Secondary: Redis cache (shared across instances)
- Automatic promotion from secondary to primary on cache hits

### 4. Response Caching

HTTP response-level caching for entire API responses.

**Features:**
- Automatic caching of GET requests
- Configurable cache duration per endpoint
- Cache-Control header support
- Size-based filtering

## Cache Invalidation Strategies

### Automatic Invalidation

The system automatically invalidates cache entries when data changes:

1. **Entity-Level Invalidation**: When an entity is updated/deleted, its cache entry is removed
2. **Related Entity Invalidation**: Related entities are invalidated (e.g., engines when vessel changes)
3. **Pattern-Based Invalidation**: Cache entries matching patterns are invalidated (e.g., all vessel lists)

### Invalidation Strategies by Entity

#### Vessel
- Invalidates: `vessel:{id}`, `vessel:list:*`, related engines, sensors, alarms
- Strategy: Cascade to related entities

#### Engine
- Invalidates: `engine:{id}`, `engine:list:*`, related sensors, alarms
- Strategy: Cascade to related entities

#### Alarm
- Invalidates: `alarm:{id}`, `alarm:active`, `alarm:recent`
- Strategy: Minimal invalidation (alarms are frequently created)

### Manual Invalidation

```csharp
// Invalidate specific entity
await _cacheInvalidationService.InvalidateEntityAsync("Vessel", "vessel-001");

// Invalidate all entities of a type
await _cacheInvalidationService.InvalidateEntityTypeAsync("Vessel");

// Invalidate by pattern
await _cacheInvalidationService.InvalidatePatternAsync("vessel:list:*");
```

## Implementation Details

### Cached Repository Pattern

Repositories are wrapped with caching decorators that:
1. Check cache before database queries
2. Store query results in cache
3. Invalidate cache on data modifications

**Example:**
```csharp
public class CachedVesselRepository : IVesselRepository
{
    public async Task<Vessel?> GetByIdAsync(object id)
    {
        var cacheKey = $"vessel:{id}";
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(id),
            TimeSpan.FromMinutes(5));
    }
}
```

### Cache Key Naming Convention

```
{prefix}:{entity}:{id}              // Single entity
{prefix}:{entity}:list:{filter}     // List queries
{prefix}:{entity}:{id}:{relation}   // Related entities
```

Examples:
- `kchief:vessel:vessel-001`
- `kchief:vessel:list:status:Online`
- `kchief:vessel:vessel-001:engines`

### Cache Expiration Times

| Entity Type | Expiration | Reason |
|------------|-----------|--------|
| Vessel | 5 minutes | Changes infrequently |
| Engine | 30 seconds | Real-time data |
| Sensor | 30 seconds | Real-time data |
| Alarm | 1 minute | Frequently created |
| Configuration | 1 hour | Rarely changes |

## Configuration

### appsettings.json

```json
{
  "Caching": {
    "DefaultExpiration": "00:05:00",
    "FrequentDataExpiration": "00:15:00",
    "RareDataExpiration": "01:00:00",
    "RealTimeDataExpiration": "00:00:30",
    "InMemoryCacheSizeLimit": 1000,
    "UseDistributedCache": false,
    "RedisConnectionString": "",
    "KeyPrefix": "kchief:",
    "EntityExpirations": {
      "Vessel": "00:05:00",
      "Engine": "00:00:30",
      "Sensor": "00:00:30",
      "Alarm": "00:01:00"
    }
  }
}
```

### Environment-Specific Configuration

**Development:**
- In-memory caching only
- Shorter expiration times for testing

**Production:**
- Redis distributed caching
- Optimized expiration times
- Cache size limits

## Response Caching

### Middleware Configuration

The `ResponseCachingMiddleware` automatically caches HTTP responses:

```csharp
var responseCacheOptions = new ResponseCachingOptions
{
    DefaultCacheDuration = TimeSpan.FromMinutes(1),
    MaxResponseSize = 1024 * 1024, // 1MB
    AllowCachingForAuthenticatedUsers = false,
    CachePerUser = false
};
```

### Cache-Control Headers

Clients can control caching using standard HTTP headers:

```http
Cache-Control: max-age=300        # Cache for 5 minutes
Cache-Control: no-cache            # Don't cache
Cache-Control: no-store            # Don't store
```

### Endpoint-Specific Caching

Different endpoints have different cache durations:
- `/api/vessels`: 5 minutes (changes infrequently)
- `/api/engines`: 30 seconds (real-time data)
- `/api/alarms`: 1 minute (frequently updated)

## Health Checks

### Redis Health Check

The system includes a health check for Redis connectivity:

```csharp
.AddCheck<RedisHealthCheck>("redis")
```

**Health Check Endpoint:**
```http
GET /health
```

Returns:
- `Healthy`: Redis is connected and responding
- `Unhealthy`: Redis connection failed
- `Healthy` (with message): Redis not configured (fallback to in-memory)

## Performance Considerations

### Cache Hit Rates

Monitor cache hit rates to optimize:
- High hit rate (>80%): Good caching strategy
- Low hit rate (<50%): Review cache keys and expiration times

### Memory Management

- In-memory cache has size limits to prevent memory issues
- LRU (Least Recently Used) eviction when limit reached
- Monitor memory usage in production

### Cache Warming

Pre-populate cache with frequently accessed data:

```csharp
// On application startup
await _cacheService.SetAsync("vessel:list:all", vessels, TimeSpan.FromMinutes(15));
```

## Best Practices

### 1. Cache Key Design
- Use consistent naming conventions
- Include all relevant parameters in keys
- Keep keys concise but descriptive

### 2. Expiration Times
- Balance freshness vs. performance
- Shorter for frequently changing data
- Longer for stable reference data

### 3. Invalidation
- Invalidate on all write operations
- Use cascade invalidation for related entities
- Consider eventual consistency for non-critical data

### 4. Monitoring
- Track cache hit/miss rates
- Monitor cache size and memory usage
- Alert on cache failures

### 5. Error Handling
- Cache failures should not break application
- Fallback to database on cache errors
- Log cache operations for debugging

## Usage Examples

### Using Cache Service Directly

```csharp
public class VesselService
{
    private readonly ICacheService _cacheService;
    
    public async Task<Vessel?> GetVesselAsync(string id)
    {
        var cacheKey = $"vessel:{id}";
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(id),
            TimeSpan.FromMinutes(5));
    }
}
```

### Manual Cache Invalidation

```csharp
public async Task UpdateVesselAsync(Vessel vessel)
{
    await _repository.UpdateAsync(vessel);
    
    // Invalidate cache
    await _cacheInvalidationService.InvalidateEntityAsync("Vessel", vessel.Id);
}
```

### Response Caching in Controllers

```csharp
[HttpGet]
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public async Task<ActionResult<IEnumerable<Vessel>>> GetVessels()
{
    // Response will be cached automatically
    return Ok(await _vesselService.GetAllVesselsAsync());
}
```

## Troubleshooting

### Cache Not Working

1. **Check Configuration**: Verify caching is enabled in `appsettings.json`
2. **Check Redis**: If using Redis, verify connection string
3. **Check Logs**: Look for cache-related errors in logs
4. **Verify Keys**: Ensure cache keys are consistent

### High Memory Usage

1. **Reduce Cache Size**: Lower `InMemoryCacheSizeLimit`
2. **Shorter Expiration**: Reduce expiration times
3. **Review Keys**: Ensure no memory leaks from key generation

### Stale Data

1. **Check Invalidation**: Verify invalidation is called on updates
2. **Review Expiration**: Ensure expiration times are appropriate
3. **Clear Cache**: Manually clear cache if needed

## Future Enhancements

- [ ] Cache compression for large objects
- [ ] Cache analytics and metrics dashboard
- [ ] Automatic cache warming strategies
- [ ] Cache partitioning for better memory management
- [ ] Support for cache clusters
- [ ] Cache versioning for schema changes
- [ ] Advanced cache eviction policies (LFU, FIFO)

## Related Documentation

- [Architecture Documentation](ARCHITECTURE.md)
- [Performance Monitoring](MONITORING.md)
- [Error Handling](ERROR_HANDLING.md)
- [Logging](LOGGING.md)

