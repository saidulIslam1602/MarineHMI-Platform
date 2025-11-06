namespace KChief.Platform.Core.Models;

/// <summary>
/// Represents an alarm in the marine automation system.
/// </summary>
public class Alarm
{
    /// <summary>
    /// Unique identifier for the alarm.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alarm title or name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the alarm.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the alarm.
    /// </summary>
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Info;

    /// <summary>
    /// Current status of the alarm.
    /// </summary>
    public AlarmStatus Status { get; set; } = AlarmStatus.Active;

    /// <summary>
    /// Source vessel ID that triggered the alarm.
    /// </summary>
    public string? VesselId { get; set; }

    /// <summary>
    /// Source engine ID that triggered the alarm (if applicable).
    /// </summary>
    public string? EngineId { get; set; }

    /// <summary>
    /// Source sensor ID that triggered the alarm (if applicable).
    /// </summary>
    public string? SensorId { get; set; }

    /// <summary>
    /// Timestamp when the alarm was triggered.
    /// </summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the alarm was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// User who acknowledged the alarm.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Timestamp when the alarm was cleared.
    /// </summary>
    public DateTime? ClearedAt { get; set; }
}

/// <summary>
/// Severity levels for alarms.
/// </summary>
public enum AlarmSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Status of an alarm.
/// </summary>
public enum AlarmStatus
{
    Active,
    Acknowledged,
    Cleared
}

