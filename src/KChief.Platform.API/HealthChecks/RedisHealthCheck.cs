using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using KChief.Platform.Core.Models;

namespace KChief.Platform.API.HealthChecks;

/// <summary>
/// Health check for Redis connection.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly CacheOptions _options;
    private readonly ILogger<RedisHealthCheck> _logger;
    private IConnectionMultiplexer? _connection;

    public RedisHealthCheck(
        IOptions<CacheOptions> options,
        ILogger<RedisHealthCheck> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Skip check if Redis is not configured
            if (!_options.UseDistributedCache || string.IsNullOrWhiteSpace(_options.RedisConnectionString))
            {
                return HealthCheckResult.Healthy("Redis caching is not configured");
            }

            // Try to get or create connection
            if (_connection == null || !_connection.IsConnected)
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(_options.RedisConnectionString!);
            }

            // Test connection with a ping
            var database = _connection.GetDatabase();
            await database.PingAsync();

            return HealthCheckResult.Healthy("Redis connection is active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}

