using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.Services.Caching;

/// <summary>
/// Service for managing cache invalidation strategies.
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly Dictionary<string, CacheInvalidationStrategy> _strategies;

    public CacheInvalidationService(
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategies = new Dictionary<string, CacheInvalidationStrategy>();
        
        InitializeDefaultStrategies();
    }

    public async Task InvalidateEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("EntityType", entityType))
        using (LogContext.PushProperty("EntityId", entityId))
        using (LogContext.PushProperty("Operation", "InvalidateEntity"))
        {
            try
            {
                // Invalidate specific entity
                var entityKey = $"{entityType}:{entityId}";
                await _cacheService.RemoveAsync(entityKey, cancellationToken);
                Log.Debug("Invalidated cache for entity {EntityType}:{EntityId}", entityType, entityId);

                // Invalidate related patterns
                var patterns = new List<string>
                {
                    $"{entityType}:{entityId}:*",  // All keys for this entity
                    $"{entityType}:list:*",         // List queries
                    $"{entityType}:query:*"         // Query results
                };

                foreach (var pattern in patterns)
                {
                    await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
                }

                // Apply registered strategy
                if (_strategies.TryGetValue(entityType, out var strategy))
                {
                    // Invalidate related entity types
                    foreach (var relatedType in strategy.RelatedEntityTypes)
                    {
                        await InvalidateEntityTypeAsync(relatedType, cancellationToken);
                    }

                    // Invalidate custom patterns
                    foreach (var keyPattern in strategy.KeyPatterns)
                    {
                        var expandedPattern = keyPattern.Replace("{EntityId}", entityId);
                        await _cacheService.RemoveByPatternAsync(expandedPattern, cancellationToken);
                    }

                    // If strategy requires invalidating all entities of this type
                    if (strategy.InvalidateAllOnChange)
                    {
                        await InvalidateEntityTypeAsync(entityType, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error invalidating cache for entity {EntityType}:{EntityId}", entityType, entityId);
            }
        }
    }

    public async Task InvalidateEntityTypeAsync(string entityType, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("EntityType", entityType))
        using (LogContext.PushProperty("Operation", "InvalidateEntityType"))
        {
            try
            {
                var pattern = $"{entityType}:*";
                await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
                Log.Debug("Invalidated all cache entries for entity type {EntityType}", entityType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error invalidating cache for entity type {EntityType}", entityType);
            }
        }
    }

    public async Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Pattern", pattern))
        using (LogContext.PushProperty("Operation", "InvalidatePattern"))
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
                Log.Debug("Invalidated cache entries matching pattern {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error invalidating cache pattern {Pattern}", pattern);
            }
        }
    }

    public async Task InvalidateRelatedEntitiesAsync(
        string parentEntityType,
        string parentEntityId,
        IEnumerable<string> relatedEntityTypes,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("ParentEntityType", parentEntityType))
        using (LogContext.PushProperty("ParentEntityId", parentEntityId))
        using (LogContext.PushProperty("Operation", "InvalidateRelatedEntities"))
        {
            try
            {
                // Invalidate parent entity
                await InvalidateEntityAsync(parentEntityType, parentEntityId, cancellationToken);

                // Invalidate each related entity type
                foreach (var relatedType in relatedEntityTypes)
                {
                    // Invalidate all entities of related type that reference the parent
                    var pattern = $"{relatedType}:*:{parentEntityType}:{parentEntityId}";
                    await InvalidatePatternAsync(pattern, cancellationToken);
                    
                    // Also invalidate all entities of related type (if they're tightly coupled)
                    await InvalidateEntityTypeAsync(relatedType, cancellationToken);
                }

                Log.Debug("Invalidated related entities for {ParentEntityType}:{ParentEntityId}", parentEntityType, parentEntityId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error invalidating related entities for {ParentEntityType}:{ParentEntityId}", parentEntityType, parentEntityId);
            }
        }
    }

    public void RegisterStrategy(string entityType, CacheInvalidationStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
        }

        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        _strategies[entityType] = strategy;
        Log.Information("Registered cache invalidation strategy for entity type {EntityType}", entityType);
    }

    private void InitializeDefaultStrategies()
    {
        // Vessel strategy - invalidate engines and sensors when vessel changes
        RegisterStrategy("Vessel", new CacheInvalidationStrategy
        {
            EntityType = "Vessel",
            RelatedEntityTypes = new List<string> { "Engine", "Sensor", "Alarm" },
            KeyPatterns = new List<string> { "vessel:{EntityId}:engines", "vessel:{EntityId}:sensors" },
            InvalidateAllOnChange = false
        });

        // Engine strategy - invalidate sensors and alarms when engine changes
        RegisterStrategy("Engine", new CacheInvalidationStrategy
        {
            EntityType = "Engine",
            RelatedEntityTypes = new List<string> { "Sensor", "Alarm" },
            KeyPatterns = new List<string> { "engine:{EntityId}:sensors", "engine:{EntityId}:status" },
            InvalidateAllOnChange = false
        });

        // Alarm strategy - minimal invalidation
        RegisterStrategy("Alarm", new CacheInvalidationStrategy
        {
            EntityType = "Alarm",
            RelatedEntityTypes = new List<string>(),
            KeyPatterns = new List<string> { "alarm:active", "alarm:recent" },
            InvalidateAllOnChange = false
        });
    }
}

