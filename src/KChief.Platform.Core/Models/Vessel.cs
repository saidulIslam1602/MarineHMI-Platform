namespace KChief.Platform.Core.Models;

/// <summary>
/// Represents a marine vessel in the automation system.
/// </summary>
public class Vessel
{
    /// <summary>
    /// Unique identifier for the vessel.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the vessel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vessel type (e.g., "Container Ship", "Tanker", "Cruise Ship").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Current operational status of the vessel.
    /// </summary>
    public VesselStatus Status { get; set; } = VesselStatus.Offline;

    /// <summary>
    /// List of engines on the vessel.
    /// </summary>
    public List<Engine> Engines { get; set; } = new();

    /// <summary>
    /// Timestamp when the vessel was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Operational status of a vessel.
/// </summary>
public enum VesselStatus
{
    Offline,
    Online,
    Maintenance,
    Emergency
}

