using Microsoft.Extensions.Diagnostics.HealthChecks;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.HealthChecks;

/// <summary>
/// Health check for RabbitMQ message bus connectivity.
/// </summary>
public class MessageBusHealthCheck : IHealthCheck
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageBusHealthCheck> _logger;

    public MessageBusHealthCheck(IMessageBus messageBus, ILogger<MessageBusHealthCheck> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Message Bus health");

            // Check if message bus is connected
            if (_messageBus.IsConnected)
            {
                _logger.LogDebug("Message Bus is connected");
                return HealthCheckResult.Healthy("Message Bus (RabbitMQ) is connected and operational");
            }

            // Try to connect for health check
            _logger.LogDebug("Message Bus not connected, attempting connection test");
            
            try
            {
                await _messageBus.ConnectAsync();
                
                if (_messageBus.IsConnected)
                {
                    _logger.LogDebug("Message Bus connection test successful");
                    return HealthCheckResult.Healthy("Message Bus is available and connected");
                }
                else
                {
                    _logger.LogWarning("Message Bus connection test failed - running in simulation mode");
                    return HealthCheckResult.Degraded("Message Bus is running in simulation mode (RabbitMQ not available)");
                }
            }
            catch (Exception connectEx)
            {
                _logger.LogWarning(connectEx, "Message Bus connection failed - running in simulation mode");
                return HealthCheckResult.Degraded("Message Bus is running in simulation mode", connectEx);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message Bus health check failed");
            return HealthCheckResult.Unhealthy("Message Bus is not available", ex);
        }
    }
}
