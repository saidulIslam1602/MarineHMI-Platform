using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Service implementation for alarm management.
/// </summary>
public class AlarmService : IAlarmService
{
    private readonly Dictionary<string, Alarm> _alarms = new();
    private readonly object _lockObject = new();

    public event EventHandler<Alarm>? AlarmCreated;
    public event EventHandler<Alarm>? AlarmAcknowledged;
    public event EventHandler<Alarm>? AlarmCleared;

    public Task<IEnumerable<Alarm>> GetActiveAlarmsAsync()
    {
        lock (_lockObject)
        {
            return Task.FromResult(_alarms.Values.Where(a => a.Status == AlarmStatus.Active).AsEnumerable());
        }
    }

    public Task<IEnumerable<Alarm>> GetAllAlarmsAsync()
    {
        lock (_lockObject)
        {
            return Task.FromResult(_alarms.Values.AsEnumerable());
        }
    }

    public Task<Alarm?> GetAlarmByIdAsync(string alarmId)
    {
        lock (_lockObject)
        {
            _alarms.TryGetValue(alarmId, out var alarm);
            return Task.FromResult(alarm);
        }
    }

    public Task<Alarm> CreateAlarmAsync(string title, string description, AlarmSeverity severity, string? vesselId = null, string? engineId = null, string? sensorId = null)
    {
        var alarm = new Alarm
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Description = description,
            Severity = severity,
            Status = AlarmStatus.Active,
            VesselId = vesselId,
            EngineId = engineId,
            SensorId = sensorId,
            TriggeredAt = DateTime.UtcNow
        };

        lock (_lockObject)
        {
            _alarms[alarm.Id] = alarm;
        }

        AlarmCreated?.Invoke(this, alarm);
        return Task.FromResult(alarm);
    }

    public Task<bool> AcknowledgeAlarmAsync(string alarmId, string acknowledgedBy)
    {
        lock (_lockObject)
        {
            if (_alarms.TryGetValue(alarmId, out var alarm) && alarm.Status == AlarmStatus.Active)
            {
                alarm.Status = AlarmStatus.Acknowledged;
                alarm.AcknowledgedAt = DateTime.UtcNow;
                alarm.AcknowledgedBy = acknowledgedBy;

                AlarmAcknowledged?.Invoke(this, alarm);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> ClearAlarmAsync(string alarmId)
    {
        lock (_lockObject)
        {
            if (_alarms.TryGetValue(alarmId, out var alarm) && alarm.Status != AlarmStatus.Cleared)
            {
                alarm.Status = AlarmStatus.Cleared;
                alarm.ClearedAt = DateTime.UtcNow;

                AlarmCleared?.Invoke(this, alarm);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}

