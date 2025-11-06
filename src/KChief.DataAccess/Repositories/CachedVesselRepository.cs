using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace KChief.DataAccess.Repositories;

/// <summary>
/// Cached wrapper for VesselRepository that implements caching for frequently accessed data.
/// </summary>
public class CachedVesselRepository : IVesselRepository
{
    private readonly IVesselRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly ILogger<CachedVesselRepository> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

    public CachedVesselRepository(
        IVesselRepository repository,
        ICacheService cacheService,
        ICacheInvalidationService cacheInvalidationService,
        ILogger<CachedVesselRepository> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _cacheInvalidationService = cacheInvalidationService ?? throw new ArgumentNullException(nameof(cacheInvalidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Vessel>> GetAllAsync()
    {
        const string cacheKey = "vessel:list:all";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetAllAsync(),
            _defaultExpiration);
    }

    public async Task<IEnumerable<Vessel>> GetAsync(
        System.Linq.Expressions.Expression<Func<Vessel, bool>>? filter = null,
        Func<IQueryable<Vessel>, IOrderedQueryable<Vessel>>? orderBy = null,
        string includeProperties = "")
    {
        // For complex queries, don't cache (or use a more sophisticated key generation)
        return await _repository.GetAsync(filter, orderBy, includeProperties);
    }

    public async Task<Vessel?> GetByIdAsync(object id)
    {
        var vesselId = id.ToString() ?? throw new ArgumentException("Vessel ID cannot be null", nameof(id));
        var cacheKey = $"vessel:{vesselId}";

        using (LogContext.PushProperty("VesselId", vesselId))
        {
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByIdAsync(id),
                _defaultExpiration);
        }
    }

    public async Task<Vessel?> GetFirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<Vessel, bool>>? filter = null,
        string includeProperties = "")
    {
        return await _repository.GetFirstOrDefaultAsync(filter, includeProperties);
    }

    public async Task<Vessel> AddAsync(Vessel entity)
    {
        var result = await _repository.AddAsync(entity);
        
        // Invalidate cache
        await _cacheInvalidationService.InvalidateEntityAsync("Vessel", entity.Id);
        await _cacheInvalidationService.InvalidatePatternAsync("vessel:list:*");
        
        _logger.LogInformation("Vessel {VesselId} added, cache invalidated", entity.Id);
        
        return result;
    }

    public async Task AddRangeAsync(IEnumerable<Vessel> entities)
    {
        await _repository.AddRangeAsync(entities);
        
        // Invalidate all vessel caches
        await _cacheInvalidationService.InvalidateEntityTypeAsync("Vessel");
        
        _logger.LogInformation("Multiple vessels added, cache invalidated");
    }

    public void Update(Vessel entity)
    {
        _repository.Update(entity);
        
        // Invalidate cache asynchronously (fire and forget for non-async method)
        _ = Task.Run(async () =>
        {
            await _cacheInvalidationService.InvalidateEntityAsync("Vessel", entity.Id);
            await _cacheInvalidationService.InvalidatePatternAsync("vessel:list:*");
            _logger.LogInformation("Vessel {VesselId} updated, cache invalidated", entity.Id);
        });
    }

    public void UpdateRange(IEnumerable<Vessel> entities)
    {
        _repository.UpdateRange(entities);
        
        // Invalidate all vessel caches
        _ = Task.Run(async () =>
        {
            await _cacheInvalidationService.InvalidateEntityTypeAsync("Vessel");
            _logger.LogInformation("Multiple vessels updated, cache invalidated");
        });
    }

    public void Delete(Vessel entity)
    {
        var vesselId = entity.Id;
        _repository.Delete(entity);
        
        // Invalidate cache
        _ = Task.Run(async () =>
        {
            await _cacheInvalidationService.InvalidateEntityAsync("Vessel", vesselId);
            await _cacheInvalidationService.InvalidatePatternAsync("vessel:list:*");
            _logger.LogInformation("Vessel {VesselId} deleted, cache invalidated", vesselId);
        });
    }

    public async Task DeleteAsync(object id)
    {
        var vesselId = id.ToString() ?? throw new ArgumentException("Vessel ID cannot be null", nameof(id));
        await _repository.DeleteAsync(id);
        
        // Invalidate cache
        await _cacheInvalidationService.InvalidateEntityAsync("Vessel", vesselId);
        await _cacheInvalidationService.InvalidatePatternAsync("vessel:list:*");
        
        _logger.LogInformation("Vessel {VesselId} deleted, cache invalidated", vesselId);
    }

    public void DeleteRange(IEnumerable<Vessel> entities)
    {
        _repository.DeleteRange(entities);
        
        // Invalidate all vessel caches
        _ = Task.Run(async () =>
        {
            await _cacheInvalidationService.InvalidateEntityTypeAsync("Vessel");
            _logger.LogInformation("Multiple vessels deleted, cache invalidated");
        });
    }

    public Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<Vessel, bool>> filter)
    {
        return _repository.ExistsAsync(filter);
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Vessel, bool>>? filter = null)
    {
        return _repository.CountAsync(filter);
    }

    public async Task<IEnumerable<Vessel>> GetByStatusAsync(string status)
    {
        var cacheKey = $"vessel:list:status:{status}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetByStatusAsync(status),
            _defaultExpiration);
    }

    public async Task<IEnumerable<Vessel>> GetByTypeAsync(string type)
    {
        var cacheKey = $"vessel:list:type:{type}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetByTypeAsync(type),
            _defaultExpiration);
    }

    public async Task<IEnumerable<Vessel>> GetVesselsWithEnginesAsync()
    {
        const string cacheKey = "vessel:list:withengines";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetVesselsWithEnginesAsync(),
            _defaultExpiration);
    }

    public async Task<Vessel?> GetVesselWithEnginesAsync(string vesselId)
    {
        var cacheKey = $"vessel:{vesselId}:withengines";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetVesselWithEnginesAsync(vesselId),
            _defaultExpiration);
    }
}

