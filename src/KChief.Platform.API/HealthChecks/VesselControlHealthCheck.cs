using Microsoft.Extensions.Diagnostics.HealthChecks;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.HealthChecks;

/// <summary>
/// Health check for vessel control service functionality.
/// </summary>
public class VesselControlHealthCheck : IHealthCheck
{
    private readonly IVesselControlService _vesselControlService;
    private readonly ILogger<VesselControlHealthCheck> _logger;

    public VesselControlHealthCheck(IVesselControlService vesselControlService, ILogger<VesselControlHealthCheck> logger)
    {
        _vesselControlService = vesselControlService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Vessel Control Service health");

            // Test basic functionality by getting vessels
            var vessels = await _vesselControlService.GetAllVesselsAsync();
            var vesselCount = vessels?.Count() ?? 0;

            if (vesselCount > 0)
            {
                _logger.LogDebug("Vessel Control Service is operational with {VesselCount} vessels", vesselCount);
                return HealthCheckResult.Healthy($"Vessel Control Service is operational with {vesselCount} vessels");
            }
            else
            {
                _logger.LogWarning("Vessel Control Service is operational but no vessels found");
                return HealthCheckResult.Degraded("Vessel Control Service is operational but no vessels are available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vessel Control Service health check failed");
            return HealthCheckResult.Unhealthy("Vessel Control Service is not operational", ex);
        }
    }
}
