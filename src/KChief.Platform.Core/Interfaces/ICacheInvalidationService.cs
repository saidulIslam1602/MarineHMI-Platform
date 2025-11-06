namespace KChief.Platform.Core.Interfaces;

/// <summary>
/// Service for managing cache invalidation strategies.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates cache entries for a specific entity.
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "Vessel", "Engine")</param>
    /// <param name="entityId">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries for all entities of a specific type.
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateEntityTypeAsync(string entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries matching a pattern.
    /// </summary>
    /// <param name="pattern">Cache key pattern</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries for related entities when a parent entity changes.
    /// </summary>
    /// <param name="parentEntityType">Parent entity type</param>
    /// <param name="parentEntityId">Parent entity identifier</param>
    /// <param name="relatedEntityTypes">Related entity types to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateRelatedEntitiesAsync(
        string parentEntityType,
        string parentEntityId,
        IEnumerable<string> relatedEntityTypes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a cache invalidation strategy for an entity type.
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="strategy">Invalidation strategy</param>
    void RegisterStrategy(string entityType, CacheInvalidationStrategy strategy);
}

/// <summary>
/// Cache invalidation strategy configuration.
/// </summary>
public class CacheInvalidationStrategy
{
    /// <summary>
    /// Entity type this strategy applies to.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Related entity types to invalidate when this entity changes.
    /// </summary>
    public List<string> RelatedEntityTypes { get; set; } = new();

    /// <summary>
    /// Cache key patterns to invalidate.
    /// </summary>
    public List<string> KeyPatterns { get; set; } = new();

    /// <summary>
    /// Whether to invalidate all entities of this type when any entity changes.
    /// </summary>
    public bool InvalidateAllOnChange { get; set; } = false;
}

