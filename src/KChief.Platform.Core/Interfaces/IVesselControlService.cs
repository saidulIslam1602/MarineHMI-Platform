using KChief.Platform.Core.Models;

namespace KChief.Platform.Core.Interfaces;

/// <summary>
/// Service interface for vessel control operations.
/// </summary>
public interface IVesselControlService
{
    /// <summary>
    /// Gets all vessels in the system.
    /// </summary>
    Task<IEnumerable<Vessel>> GetAllVesselsAsync();

    /// <summary>
    /// Gets a vessel by its unique identifier.
    /// </summary>
    Task<Vessel?> GetVesselByIdAsync(string vesselId);

    /// <summary>
    /// Gets all engines for a specific vessel.
    /// </summary>
    Task<IEnumerable<Engine>> GetVesselEnginesAsync(string vesselId);

    /// <summary>
    /// Gets a specific engine by vessel and engine ID.
    /// </summary>
    Task<Engine?> GetEngineByIdAsync(string vesselId, string engineId);

    /// <summary>
    /// Starts an engine.
    /// </summary>
    Task<bool> StartEngineAsync(string vesselId, string engineId);

    /// <summary>
    /// Stops an engine.
    /// </summary>
    Task<bool> StopEngineAsync(string vesselId, string engineId);

    /// <summary>
    /// Sets the RPM for an engine.
    /// </summary>
    Task<bool> SetEngineRpmAsync(string vesselId, string engineId, int rpm);

    /// <summary>
    /// Gets all sensors for a specific vessel.
    /// </summary>
    Task<IEnumerable<Sensor>> GetVesselSensorsAsync(string vesselId);
}

