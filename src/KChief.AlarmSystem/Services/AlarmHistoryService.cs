using System.Collections.Concurrent;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Serilog;
using Serilog.Context;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Service for tracking alarm history and analyzing trends.
/// </summary>
public class AlarmHistoryService
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmHistoryService> _logger;
    private readonly ConcurrentDictionary<string, List<AlarmHistory>> _alarmHistories = new();
    private readonly ConcurrentDictionary<string, Alarm> _alarmSnapshots = new();

    public AlarmHistoryService(
        IAlarmService alarmService,
        ILogger<AlarmHistoryService> logger)
    {
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to alarm events
        _alarmService.AlarmCreated += OnAlarmCreated;
        _alarmService.AlarmAcknowledged += OnAlarmAcknowledged;
        _alarmService.AlarmCleared += OnAlarmCleared;
    }

    /// <summary>
    /// Records an alarm history event.
    /// </summary>
    public void RecordHistoryEvent(
        string alarmId,
        AlarmHistoryEventType eventType,
        string? userId = null,
        string? details = null,
        AlarmSeverity? previousSeverity = null,
        AlarmSeverity? newSeverity = null,
        double? sourceValue = null,
        double? thresholdValue = null)
    {
        var history = new AlarmHistory
        {
            Id = Guid.NewGuid().ToString(),
            AlarmId = alarmId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Details = details,
            PreviousSeverity = previousSeverity,
            NewSeverity = newSeverity,
            SourceValue = sourceValue,
            ThresholdValue = thresholdValue
        };

        var histories = _alarmHistories.GetOrAdd(alarmId, _ => new List<AlarmHistory>());
        lock (histories)
        {
            histories.Add(history);
        }

        using (LogContext.PushProperty("AlarmId", alarmId))
        using (LogContext.PushProperty("EventType", eventType))
        {
            Log.Debug("Alarm history event recorded: {AlarmId} - {EventType}", alarmId, eventType);
        }
    }

    /// <summary>
    /// Gets history for an alarm.
    /// </summary>
    public IEnumerable<AlarmHistory> GetAlarmHistory(string alarmId)
    {
        if (_alarmHistories.TryGetValue(alarmId, out var histories))
        {
            return histories.OrderBy(h => h.Timestamp);
        }
        return Enumerable.Empty<AlarmHistory>();
    }

    /// <summary>
    /// Gets alarm trends for a time period.
    /// </summary>
    public AlarmTrend GetTrends(DateTime startDate, DateTime endDate)
    {
        var allHistories = _alarmHistories.Values
            .SelectMany(h => h)
            .Where(h => h.Timestamp >= startDate && h.Timestamp <= endDate)
            .ToList();

        var createdEvents = allHistories
            .Where(h => h.EventType == AlarmHistoryEventType.Created)
            .ToList();

        var trend = new AlarmTrend
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalAlarms = createdEvents.Count
        };

        // Calculate by severity
        var alarms = _alarmService.GetAllAlarmsAsync().Result
            .Where(a => a.TriggeredAt >= startDate && a.TriggeredAt <= endDate);

        foreach (var alarm in alarms)
        {
            if (!trend.AlarmsBySeverity.ContainsKey(alarm.Severity))
            {
                trend.AlarmsBySeverity[alarm.Severity] = 0;
            }
            trend.AlarmsBySeverity[alarm.Severity]++;

            var sourceType = GetSourceType(alarm);
            if (!trend.AlarmsBySourceType.ContainsKey(sourceType))
            {
                trend.AlarmsBySourceType[sourceType] = 0;
            }
            trend.AlarmsBySourceType[sourceType]++;
        }

        // Calculate average acknowledge time
        var acknowledgedAlarms = alarms
            .Where(a => a.AcknowledgedAt.HasValue)
            .ToList();

        if (acknowledgedAlarms.Any())
        {
            var totalAcknowledgeTime = acknowledgedAlarms
                .Sum(a => (a.AcknowledgedAt!.Value - a.TriggeredAt).TotalSeconds);
            trend.AverageAcknowledgeTimeSeconds = totalAcknowledgeTime / acknowledgedAlarms.Count;
        }

        // Calculate average clear time
        var clearedAlarms = alarms
            .Where(a => a.ClearedAt.HasValue)
            .ToList();

        if (clearedAlarms.Any())
        {
            var totalClearTime = clearedAlarms
                .Sum(a => (a.ClearedAt!.Value - a.TriggeredAt).TotalSeconds);
            trend.AverageClearTimeSeconds = totalClearTime / clearedAlarms.Count;
        }

        // Calculate most frequent alarms
        var alarmTitles = alarms
            .GroupBy(a => a.Title)
            .Select(g => new AlarmFrequency
            {
                AlarmTitle = g.Key,
                Count = g.Count(),
                Percentage = (double)g.Count() / trend.TotalAlarms * 100
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        trend.MostFrequentAlarms = alarmTitles;

        return trend;
    }

    /// <summary>
    /// Gets source type from alarm.
    /// </summary>
    private string GetSourceType(Alarm alarm)
    {
        if (!string.IsNullOrEmpty(alarm.SensorId))
        {
            return "Sensor";
        }
        if (!string.IsNullOrEmpty(alarm.EngineId))
        {
            return "Engine";
        }
        if (!string.IsNullOrEmpty(alarm.VesselId))
        {
            return "Vessel";
        }
        return "Unknown";
    }

    /// <summary>
    /// Handles alarm created event.
    /// </summary>
    private void OnAlarmCreated(object? sender, Alarm alarm)
    {
        RecordHistoryEvent(
            alarm.Id,
            AlarmHistoryEventType.Created,
            details: $"Alarm created: {alarm.Title}",
            sourceValue: null,
            thresholdValue: null);

        // Store snapshot
        _alarmSnapshots[alarm.Id] = new Alarm
        {
            Id = alarm.Id,
            Title = alarm.Title,
            Description = alarm.Description,
            Severity = alarm.Severity,
            Status = alarm.Status,
            VesselId = alarm.VesselId,
            EngineId = alarm.EngineId,
            SensorId = alarm.SensorId,
            TriggeredAt = alarm.TriggeredAt
        };
    }

    /// <summary>
    /// Handles alarm acknowledged event.
    /// </summary>
    private void OnAlarmAcknowledged(object? sender, Alarm alarm)
    {
        var previousSeverity = _alarmSnapshots.TryGetValue(alarm.Id, out var snapshot) 
            ? snapshot.Severity 
            : (AlarmSeverity?)null;

        RecordHistoryEvent(
            alarm.Id,
            AlarmHistoryEventType.Acknowledged,
            userId: alarm.AcknowledgedBy,
            details: $"Alarm acknowledged by {alarm.AcknowledgedBy}",
            previousSeverity: previousSeverity,
            newSeverity: alarm.Severity);
    }

    /// <summary>
    /// Handles alarm cleared event.
    /// </summary>
    private void OnAlarmCleared(object? sender, Alarm alarm)
    {
        RecordHistoryEvent(
            alarm.Id,
            AlarmHistoryEventType.Cleared,
            userId: alarm.ClearedBy,
            details: $"Alarm cleared by {alarm.ClearedBy}",
            previousSeverity: _alarmSnapshots.TryGetValue(alarm.Id, out var snapshot) 
                ? snapshot.Severity 
                : (AlarmSeverity?)null,
            newSeverity: alarm.Severity);
    }
}

