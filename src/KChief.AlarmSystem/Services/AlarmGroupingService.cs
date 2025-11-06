using System.Collections.Concurrent;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Serilog;
using Serilog.Context;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Service for grouping and correlating related alarms.
/// </summary>
public class AlarmGroupingService
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmGroupingService> _logger;
    private readonly ConcurrentDictionary<string, AlarmGroup> _groups = new();
    private readonly ConcurrentDictionary<string, string> _alarmToGroupMap = new();

    public AlarmGroupingService(
        IAlarmService alarmService,
        ILogger<AlarmGroupingService> logger)
    {
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to alarm events
        _alarmService.AlarmCreated += OnAlarmCreated;
    }

    /// <summary>
    /// Groups an alarm with existing alarms based on grouping strategy.
    /// </summary>
    public async Task<string?> GroupAlarmAsync(Alarm alarm, AlarmGroupingConfig? groupingConfig)
    {
        if (groupingConfig == null || !groupingConfig.Enabled)
        {
            return null;
        }

        using (LogContext.PushProperty("AlarmId", alarm.Id))
        using (LogContext.PushProperty("Strategy", groupingConfig.Strategy))
        {
            // Find existing group or create new one
            var group = FindOrCreateGroup(alarm, groupingConfig);

            if (group != null)
            {
                if (!group.AlarmIds.Contains(alarm.Id))
                {
                    group.AlarmIds.Add(alarm.Id);
                    _alarmToGroupMap[alarm.Id] = group.Id;

                    Log.Debug(
                        "Alarm {AlarmId} added to group {GroupId} ({GroupName})",
                        alarm.Id, group.Id, group.Name);

                    // Check if group is full
                    if (group.AlarmIds.Count >= groupingConfig.MaxAlarmsPerGroup)
                    {
                        Log.Warning(
                            "Alarm group {GroupId} has reached maximum capacity ({MaxAlarms})",
                            group.Id, groupingConfig.MaxAlarmsPerGroup);
                    }
                }

                return group.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an existing group or creates a new one for an alarm.
    /// </summary>
    private AlarmGroup? FindOrCreateGroup(Alarm alarm, AlarmGroupingConfig groupingConfig)
    {
        var now = DateTime.UtcNow;
        var timeWindow = TimeSpan.FromSeconds(groupingConfig.TimeWindowSeconds);

        // Find existing groups within time window
        var candidateGroups = _groups.Values
            .Where(g => g.Strategy == groupingConfig.Strategy &&
                       g.Status == AlarmGroupStatus.Active &&
                       now - g.CreatedAt <= timeWindow)
            .ToList();

        foreach (var group in candidateGroups)
        {
            if (ShouldGroupWith(alarm, group, groupingConfig))
            {
                return group;
            }
        }

        // Create new group
        var newGroup = new AlarmGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = GenerateGroupName(alarm, groupingConfig.Strategy),
            Strategy = groupingConfig.Strategy,
            AlarmIds = new List<string> { alarm.Id },
            CreatedAt = now,
            Status = AlarmGroupStatus.Active
        };

        _groups[newGroup.Id] = newGroup;
        _alarmToGroupMap[alarm.Id] = newGroup.Id;

        Log.Information(
            "Created new alarm group {GroupId} ({GroupName}) with strategy {Strategy}",
            newGroup.Id, newGroup.Name, groupingConfig.Strategy);

        return newGroup;
    }

    /// <summary>
    /// Determines if an alarm should be grouped with an existing group.
    /// </summary>
    private bool ShouldGroupWith(Alarm alarm, AlarmGroup group, AlarmGroupingConfig config)
    {
        // Get first alarm in group to compare
        var firstAlarmId = group.AlarmIds.FirstOrDefault();
        if (firstAlarmId == null)
        {
            return false;
        }

        var firstAlarm = _alarmService.GetAlarmByIdAsync(firstAlarmId).Result;
        if (firstAlarm == null)
        {
            return false;
        }

        return config.Strategy switch
        {
            AlarmGroupingStrategy.BySource => 
                alarm.VesselId == firstAlarm.VesselId ||
                alarm.EngineId == firstAlarm.EngineId ||
                alarm.SensorId == firstAlarm.SensorId,

            AlarmGroupingStrategy.BySeverity => 
                alarm.Severity == firstAlarm.Severity,

            AlarmGroupingStrategy.ByVessel => 
                alarm.VesselId == firstAlarm.VesselId && !string.IsNullOrEmpty(alarm.VesselId),

            AlarmGroupingStrategy.ByTimeWindow => 
                true, // Already filtered by time window

            _ => false
        };
    }

    /// <summary>
    /// Generates a group name based on strategy.
    /// </summary>
    private string GenerateGroupName(Alarm alarm, AlarmGroupingStrategy strategy)
    {
        return strategy switch
        {
            AlarmGroupingStrategy.BySource => $"Alarms from {alarm.VesselId ?? alarm.EngineId ?? alarm.SensorId ?? "Unknown"}",
            AlarmGroupingStrategy.BySeverity => $"{alarm.Severity} Alarms",
            AlarmGroupingStrategy.ByVessel => $"Vessel {alarm.VesselId} Alarms",
            AlarmGroupingStrategy.ByTimeWindow => "Time-based Alarm Group",
            AlarmGroupingStrategy.ByRule => "Rule-based Alarm Group",
            _ => "Alarm Group"
        };
    }

    /// <summary>
    /// Gets all alarm groups.
    /// </summary>
    public IEnumerable<AlarmGroup> GetGroups()
    {
        return _groups.Values;
    }

    /// <summary>
    /// Gets a group by ID.
    /// </summary>
    public AlarmGroup? GetGroup(string groupId)
    {
        _groups.TryGetValue(groupId, out var group);
        return group;
    }

    /// <summary>
    /// Gets the group for an alarm.
    /// </summary>
    public AlarmGroup? GetGroupForAlarm(string alarmId)
    {
        if (_alarmToGroupMap.TryGetValue(alarmId, out var groupId))
        {
            return GetGroup(groupId);
        }
        return null;
    }

    /// <summary>
    /// Acknowledges all alarms in a group.
    /// </summary>
    public async Task<bool> AcknowledgeGroupAsync(string groupId, string acknowledgedBy)
    {
        var group = GetGroup(groupId);
        if (group == null)
        {
            return false;
        }

        var allAcknowledged = true;
        foreach (var alarmId in group.AlarmIds)
        {
            var result = await _alarmService.AcknowledgeAlarmAsync(alarmId, acknowledgedBy);
            if (!result)
            {
                allAcknowledged = false;
            }
        }

        if (allAcknowledged)
        {
            group.Status = AlarmGroupStatus.Acknowledged;
            Log.Information("Alarm group {GroupId} acknowledged", groupId);
        }

        return allAcknowledged;
    }

    /// <summary>
    /// Handles alarm created event.
    /// </summary>
    private void OnAlarmCreated(object? sender, Alarm alarm)
    {
        // Grouping would be triggered by rule engine or alarm service
    }
}

