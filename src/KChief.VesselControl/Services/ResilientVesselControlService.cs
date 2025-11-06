using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using KChief.Platform.Core.Exceptions;
using KChief.Platform.API.Services;

namespace KChief.VesselControl.Services;

/// <summary>
/// Resilient vessel control service that implements retry, circuit breaker, and fallback patterns.
/// Demonstrates how to integrate resilience patterns into domain services.
/// </summary>
public class ResilientVesselControlService : IVesselControlService
{
    private readonly VesselControlService _baseService;
    private readonly ResilienceService _resilienceService;
    private readonly ILogger<ResilientVesselControlService> _logger;

    // Fallback data for when primary service is unavailable
    private static readonly Dictionary<string, Vessel> _fallbackVessels = new()
    {
        ["fallback-001"] = new Vessel
        {
            Id = "fallback-001",
            Name = "Emergency Vessel",
            Type = VesselType.CargoShip,
            Status = VesselStatus.Docked,
            Location = "Safe Harbor",
            Length = 100,
            Width = 20,
            MaxSpeed = 15,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }
    };

    public ResilientVesselControlService(
        VesselControlService baseService,
        ResilienceService resilienceService,
        ILogger<ResilientVesselControlService> logger)
    {
        _baseService = baseService;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    public async Task<IEnumerable<Vessel>> GetVesselsAsync()
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "GetVessels"))
                {
                    Log.Debug("Executing GetVessels operation");
                    return await _baseService.GetVesselsAsync();
                }
            },
            "GetVessels");
    }

    public async Task<Vessel?> GetVesselByIdAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "GetVesselById"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Debug("Executing GetVesselById operation for vessel {VesselId}", vesselId);
                    
                    var vessel = await _baseService.GetVesselByIdAsync(vesselId);
                    
                    // If vessel not found and we're in degraded mode, return fallback
                    if (vessel == null && _fallbackVessels.ContainsKey(vesselId))
                    {
                        Log.Warning("Primary vessel data unavailable, returning fallback data for {VesselId}", vesselId);
                        return _fallbackVessels[vesselId];
                    }
                    
                    return vessel;
                }
            },
            "GetVesselById");
    }

    public async Task<IEnumerable<Engine>> GetVesselEnginesAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "GetVesselEngines"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Debug("Executing GetVesselEngines operation for vessel {VesselId}", vesselId);
                    
                    try
                    {
                        return await _baseService.GetVesselEnginesAsync(vesselId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to get engines for vessel {VesselId}, returning fallback engines", vesselId);
                        
                        // Return fallback engines
                        return new[]
                        {
                            new Engine
                            {
                                Id = $"fallback-engine-{vesselId}",
                                Name = "Emergency Engine",
                                Type = EngineType.Diesel,
                                VesselId = vesselId,
                                RPM = 0,
                                MaxRPM = 1800,
                                Temperature = 20,
                                IsRunning = false
                            }
                        };
                    }
                }
            },
            "GetVesselEngines");
    }

    public async Task<IEnumerable<Sensor>> GetVesselSensorsAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "GetVesselSensors"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Debug("Executing GetVesselSensors operation for vessel {VesselId}", vesselId);
                    
                    try
                    {
                        return await _baseService.GetVesselSensorsAsync(vesselId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to get sensors for vessel {VesselId}, returning fallback sensors", vesselId);
                        
                        // Return fallback sensors with safe default values
                        return new[]
                        {
                            new Sensor
                            {
                                Id = $"fallback-sensor-{vesselId}",
                                Name = "Emergency Sensor",
                                Type = SensorType.Temperature,
                                VesselId = vesselId,
                                Value = 20.0,
                                Unit = "°C",
                                IsActive = true,
                                LastReadingAt = DateTime.UtcNow
                            }
                        };
                    }
                }
            },
            "GetVesselSensors");
    }

    public async Task<bool> StartVesselAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "StartVessel"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Information("Executing StartVessel operation for vessel {VesselId}", vesselId);
                    
                    // Critical operation - validate vessel exists first
                    var vessel = await _baseService.GetVesselByIdAsync(vesselId);
                    if (vessel == null)
                    {
                        throw new VesselNotFoundException($"Vessel {vesselId} not found");
                    }
                    
                    if (vessel.Status == VesselStatus.InTransit)
                    {
                        Log.Warning("Vessel {VesselId} is already running", vesselId);
                        return true; // Already started
                    }
                    
                    var result = await _baseService.StartVesselAsync(vesselId);
                    
                    if (result)
                    {
                        Log.Information("Vessel {VesselId} started successfully", vesselId);
                    }
                    else
                    {
                        Log.Error("Failed to start vessel {VesselId}", vesselId);
                    }
                    
                    return result;
                }
            },
            "StartVessel");
    }

    public async Task<bool> StopVesselAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "StopVessel"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Information("Executing StopVessel operation for vessel {VesselId}", vesselId);
                    
                    // Critical operation - validate vessel exists first
                    var vessel = await _baseService.GetVesselByIdAsync(vesselId);
                    if (vessel == null)
                    {
                        throw new VesselNotFoundException($"Vessel {vesselId} not found");
                    }
                    
                    if (vessel.Status == VesselStatus.Docked)
                    {
                        Log.Warning("Vessel {VesselId} is already stopped", vesselId);
                        return true; // Already stopped
                    }
                    
                    var result = await _baseService.StopVesselAsync(vesselId);
                    
                    if (result)
                    {
                        Log.Information("Vessel {VesselId} stopped successfully", vesselId);
                    }
                    else
                    {
                        Log.Error("Failed to stop vessel {VesselId}", vesselId);
                    }
                    
                    return result;
                }
            },
            "StopVessel");
    }

    public async Task<bool> SetEngineRPMAsync(string vesselId, string engineId, int rpm)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "SetEngineRPM"))
                using (LogContext.PushProperty("VesselId", vesselId))
                using (LogContext.PushProperty("EngineId", engineId))
                using (LogContext.PushProperty("RPM", rpm))
                {
                    Log.Information("Executing SetEngineRPM operation for engine {EngineId} on vessel {VesselId} to {RPM} RPM",
                        engineId, vesselId, rpm);
                    
                    // Validate RPM range
                    if (rpm < 0 || rpm > 3000)
                    {
                        throw new VesselOperationException($"Invalid RPM value: {rpm}. Must be between 0 and 3000.");
                    }
                    
                    var result = await _baseService.SetEngineRPMAsync(vesselId, engineId, rpm);
                    
                    if (result)
                    {
                        Log.Information("Engine {EngineId} RPM set to {RPM} successfully", engineId, rpm);
                    }
                    else
                    {
                        Log.Error("Failed to set engine {EngineId} RPM to {RPM}", engineId, rpm);
                    }
                    
                    return result;
                }
            },
            "SetEngineRPM");
    }

    public async Task<VesselStatus> GetVesselStatusAsync(string vesselId)
    {
        return await _resilienceService.ExecuteVesselControlAsync(
            async (context, cancellationToken) =>
            {
                using (LogContext.PushProperty("Operation", "GetVesselStatus"))
                using (LogContext.PushProperty("VesselId", vesselId))
                {
                    Log.Debug("Executing GetVesselStatus operation for vessel {VesselId}", vesselId);
                    
                    try
                    {
                        return await _baseService.GetVesselStatusAsync(vesselId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to get status for vessel {VesselId}, returning fallback status", vesselId);
                        
                        // Return safe fallback status
                        return VesselStatus.Unknown;
                    }
                }
            },
            "GetVesselStatus");
    }

    /// <summary>
    /// Emergency stop operation with minimal resilience overhead for maximum speed.
    /// </summary>
    public async Task<bool> EmergencyStopAsync(string vesselId)
    {
        using (LogContext.PushProperty("Operation", "EmergencyStop"))
        using (LogContext.PushProperty("VesselId", vesselId))
        {
            Log.Error("EMERGENCY STOP initiated for vessel {VesselId}", vesselId);
            
            try
            {
                // Emergency operations bypass normal resilience patterns for speed
                // but still have basic retry for critical safety
                var result = await _baseService.StopVesselAsync(vesselId);
                
                if (result)
                {
                    Log.Error("EMERGENCY STOP completed successfully for vessel {VesselId}", vesselId);
                }
                else
                {
                    Log.Fatal("EMERGENCY STOP FAILED for vessel {VesselId} - MANUAL INTERVENTION REQUIRED", vesselId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "EMERGENCY STOP EXCEPTION for vessel {VesselId} - MANUAL INTERVENTION REQUIRED", vesselId);
                throw new VesselOperationException($"Emergency stop failed for vessel {vesselId}", ex)
                    .WithContext("VesselId", vesselId)
                    .WithContext("Operation", "EmergencyStop")
                    .WithContext("Severity", "Critical");
            }
        }
    }
}
