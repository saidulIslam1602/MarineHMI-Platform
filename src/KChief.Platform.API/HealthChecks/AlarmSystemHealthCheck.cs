using Microsoft.Extensions.Diagnostics.HealthChecks;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.HealthChecks;

/// <summary>
/// Health check for alarm system functionality.
/// </summary>
public class AlarmSystemHealthCheck : IHealthCheck
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmSystemHealthCheck> _logger;

    public AlarmSystemHealthCheck(IAlarmService alarmService, ILogger<AlarmSystemHealthCheck> logger)
    {
        _alarmService = alarmService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Alarm System health");

            // Test basic functionality by getting alarms
            var alarms = await _alarmService.GetAllAlarmsAsync();
            var activeAlarms = await _alarmService.GetActiveAlarmsAsync();
            
            var totalAlarms = alarms?.Count() ?? 0;
            var activeAlarmCount = activeAlarms?.Count() ?? 0;

            _logger.LogDebug("Alarm System operational - Total: {TotalAlarms}, Active: {ActiveAlarms}", 
                totalAlarms, activeAlarmCount);

            var data = new Dictionary<string, object>
            {
                ["total_alarms"] = totalAlarms,
                ["active_alarms"] = activeAlarmCount
            };

            if (activeAlarmCount > 10)
            {
                return HealthCheckResult.Degraded(
                    $"Alarm System operational but high number of active alarms ({activeAlarmCount})", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Alarm System operational - {totalAlarms} total alarms, {activeAlarmCount} active", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alarm System health check failed");
            return HealthCheckResult.Unhealthy("Alarm System is not operational", ex);
        }
    }
}
