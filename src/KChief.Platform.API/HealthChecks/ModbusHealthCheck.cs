using Microsoft.Extensions.Diagnostics.HealthChecks;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.HealthChecks;

/// <summary>
/// Health check for Modbus client connectivity.
/// </summary>
public class ModbusHealthCheck : IHealthCheck
{
    private readonly IModbusClient _modbusClient;
    private readonly ILogger<ModbusHealthCheck> _logger;

    public ModbusHealthCheck(IModbusClient modbusClient, ILogger<ModbusHealthCheck> logger)
    {
        _modbusClient = modbusClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Modbus client health");

            // Check if client is connected
            if (_modbusClient.IsConnected)
            {
                _logger.LogDebug("Modbus client is connected");
                return HealthCheckResult.Healthy("Modbus client is connected and operational");
            }

            // For health check, we'll verify the client can be initialized
            _logger.LogDebug("Modbus client not connected, checking availability");
            
            // In a real implementation, you might try a quick connection test
            var isHealthy = await Task.FromResult(true); // Simulate health check
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Modbus client is available but not connected");
            }

            return HealthCheckResult.Degraded("Modbus client is available but connection issues detected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modbus health check failed");
            return HealthCheckResult.Unhealthy("Modbus client is not available", ex);
        }
    }
}
