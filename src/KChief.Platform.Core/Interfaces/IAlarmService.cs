using KChief.Platform.Core.Models;

namespace KChief.Platform.Core.Interfaces;

/// <summary>
/// Service interface for alarm management operations.
/// </summary>
public interface IAlarmService
{
    /// <summary>
    /// Gets all active alarms.
    /// </summary>
    Task<IEnumerable<Alarm>> GetActiveAlarmsAsync();

    /// <summary>
    /// Gets all alarms (active and cleared).
    /// </summary>
    Task<IEnumerable<Alarm>> GetAllAlarmsAsync();

    /// <summary>
    /// Gets an alarm by its ID.
    /// </summary>
    Task<Alarm?> GetAlarmByIdAsync(string alarmId);

    /// <summary>
    /// Creates a new alarm.
    /// </summary>
    Task<Alarm> CreateAlarmAsync(string title, string description, AlarmSeverity severity, string? vesselId = null, string? engineId = null, string? sensorId = null);

    /// <summary>
    /// Acknowledges an alarm.
    /// </summary>
    Task<bool> AcknowledgeAlarmAsync(string alarmId, string acknowledgedBy);

    /// <summary>
    /// Clears an alarm.
    /// </summary>
    Task<bool> ClearAlarmAsync(string alarmId);

    /// <summary>
    /// Event raised when a new alarm is created.
    /// </summary>
    event EventHandler<Alarm>? AlarmCreated;

    /// <summary>
    /// Event raised when an alarm is acknowledged.
    /// </summary>
    event EventHandler<Alarm>? AlarmAcknowledged;

    /// <summary>
    /// Event raised when an alarm is cleared.
    /// </summary>
    event EventHandler<Alarm>? AlarmCleared;
}

