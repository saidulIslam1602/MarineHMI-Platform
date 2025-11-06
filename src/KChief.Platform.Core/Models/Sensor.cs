namespace KChief.Platform.Core.Models;

/// <summary>
/// Represents a sensor in the vessel monitoring system.
/// </summary>
public class Sensor
{
    /// <summary>
    /// Unique identifier for the sensor.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the sensor.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of sensor (e.g., "Temperature", "Pressure", "Flow", "Level").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement (e.g., "Celsius", "Bar", "L/min").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Current sensor value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Minimum acceptable value for this sensor.
    /// </summary>
    public double MinValue { get; set; }

    /// <summary>
    /// Maximum acceptable value for this sensor.
    /// </summary>
    public double MaxValue { get; set; }

    /// <summary>
    /// Indicates if the sensor is currently active and providing data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the sensor value was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

