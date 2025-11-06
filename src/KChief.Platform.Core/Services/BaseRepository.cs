using Microsoft.Extensions.Logging;

namespace KChief.Platform.Core.Services;

/// <summary>
/// Base class for repositories with common functionality.
/// </summary>
public abstract class BaseRepository
{
    protected readonly ILogger Logger;

    protected BaseRepository(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that an entity ID is not null or empty.
    /// </summary>
    protected void ValidateId(string id, string entityName = "Entity")
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{entityName} ID cannot be null or empty", nameof(id));
        }
    }

    /// <summary>
    /// Validates that an entity is not null.
    /// </summary>
    protected void ValidateEntity<T>(T? entity, string entityName = "Entity")
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity), $"{entityName} cannot be null");
        }
    }

    /// <summary>
    /// Logs a repository operation.
    /// </summary>
    protected void LogOperation(string operation, string entityId, LogLevel level = LogLevel.Debug)
    {
        Logger.Log(level, "Repository operation: {Operation} for entity {EntityId}", operation, entityId);
    }
}

