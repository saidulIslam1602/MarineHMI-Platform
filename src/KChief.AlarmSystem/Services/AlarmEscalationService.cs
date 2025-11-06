using System.Collections.Concurrent;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Serilog;
using Serilog.Context;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Service for managing alarm escalation.
/// </summary>
public class AlarmEscalationService
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmEscalationService> _logger;
    private readonly ConcurrentDictionary<string, AlarmEscalationState> _escalationStates = new();
    private readonly Timer _escalationTimer;

    public AlarmEscalationService(
        IAlarmService alarmService,
        ILogger<AlarmEscalationService> logger)
    {
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Check for escalations every 30 seconds
        _escalationTimer = new Timer(CheckEscalations, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        // Subscribe to alarm events
        _alarmService.AlarmCreated += OnAlarmCreated;
        _alarmService.AlarmAcknowledged += OnAlarmAcknowledged;
        _alarmService.AlarmCleared += OnAlarmCleared;
    }

    /// <summary>
    /// Registers an alarm for escalation monitoring.
    /// </summary>
    public void RegisterAlarmForEscalation(Alarm alarm, AlarmEscalationConfig? escalationConfig)
    {
        if (escalationConfig == null || !escalationConfig.Enabled)
        {
            return;
        }

        var state = new AlarmEscalationState
        {
            AlarmId = alarm.Id,
            CurrentSeverity = alarm.Severity,
            EscalationConfig = escalationConfig,
            EscalationStartTime = DateTime.UtcNow,
            CurrentLevel = 0
        };

        _escalationStates[alarm.Id] = state;

        using (LogContext.PushProperty("AlarmId", alarm.Id))
        {
            Log.Debug("Alarm registered for escalation: {AlarmId}", alarm.Id);
        }
    }

    /// <summary>
    /// Checks all alarms for escalation.
    /// </summary>
    private void CheckEscalations(object? state)
    {
        var now = DateTime.UtcNow;
        var alarmsToEscalate = new List<AlarmEscalationState>();

        foreach (var escalationState in _escalationStates.Values)
        {
            var alarm = _alarmService.GetAlarmByIdAsync(escalationState.AlarmId).Result;
            if (alarm == null || alarm.Status == AlarmStatus.Cleared || alarm.Status == AlarmStatus.Acknowledged)
            {
                continue; // Skip cleared or acknowledged alarms
            }

            var timeSinceStart = now - escalationState.EscalationStartTime;
            if (timeSinceStart.TotalSeconds >= escalationState.EscalationConfig.EscalationTimeSeconds &&
                escalationState.CurrentLevel < escalationState.EscalationConfig.MaxEscalationLevel)
            {
                alarmsToEscalate.Add(escalationState);
            }
        }

        foreach (var escalationState in alarmsToEscalate)
        {
            EscalateAlarm(escalationState);
        }
    }

    /// <summary>
    /// Escalates an alarm.
    /// </summary>
    private void EscalateAlarm(AlarmEscalationState escalationState)
    {
        var alarm = _alarmService.GetAlarmByIdAsync(escalationState.AlarmId).Result;
        if (alarm == null)
        {
            return;
        }

        var newSeverity = escalationState.EscalationConfig.EscalateToSeverity;
        if (newSeverity <= alarm.Severity)
        {
            // Already at or above target severity
            return;
        }

        using (LogContext.PushProperty("AlarmId", alarm.Id))
        using (LogContext.PushProperty("PreviousSeverity", alarm.Severity))
        using (LogContext.PushProperty("NewSeverity", newSeverity))
        {
            // Update alarm severity (this would require extending AlarmService)
            // For now, we'll log the escalation
            Log.Warning(
                "Alarm {AlarmId} escalated from {PreviousSeverity} to {NewSeverity} (Level {Level})",
                alarm.Id, alarm.Severity, newSeverity, escalationState.CurrentLevel + 1);

            escalationState.CurrentLevel++;
            escalationState.CurrentSeverity = newSeverity;
            escalationState.LastEscalationTime = DateTime.UtcNow;

            // Send notifications
            SendEscalationNotifications(alarm, escalationState);
        }
    }

    /// <summary>
    /// Sends escalation notifications.
    /// </summary>
    private void SendEscalationNotifications(Alarm alarm, AlarmEscalationState escalationState)
    {
        foreach (var channel in escalationState.EscalationConfig.NotificationChannels)
        {
            Log.Information(
                "Sending escalation notification via {Channel} for alarm {AlarmId}",
                channel, alarm.Id);
            // Notification implementation would go here
        }
    }

    /// <summary>
    /// Handles alarm created event.
    /// </summary>
    private void OnAlarmCreated(object? sender, Alarm alarm)
    {
        // Check if alarm has escalation config from rule
        // This would be set when alarm is created by rule engine
    }

    /// <summary>
    /// Handles alarm acknowledged event.
    /// </summary>
    private void OnAlarmAcknowledged(object? sender, Alarm alarm)
    {
        _escalationStates.TryRemove(alarm.Id, out _);
    }

    /// <summary>
    /// Handles alarm cleared event.
    /// </summary>
    private void OnAlarmCleared(object? sender, Alarm alarm)
    {
        _escalationStates.TryRemove(alarm.Id, out _);
    }
}

/// <summary>
/// Alarm escalation state.
/// </summary>
internal class AlarmEscalationState
{
    public string AlarmId { get; set; } = string.Empty;
    public AlarmSeverity CurrentSeverity { get; set; }
    public AlarmEscalationConfig EscalationConfig { get; set; } = null!;
    public DateTime EscalationStartTime { get; set; }
    public DateTime? LastEscalationTime { get; set; }
    public int CurrentLevel { get; set; }
}

