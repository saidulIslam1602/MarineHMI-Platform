namespace KChief.Platform.Core.Models;

/// <summary>
/// Represents alarm history and trend data.
/// </summary>
public class AlarmHistory
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Related alarm ID.
    /// </summary>
    public string AlarmId { get; set; } = string.Empty;

    /// <summary>
    /// Event type (Created, Acknowledged, Escalated, Cleared, etc.).
    /// </summary>
    public AlarmHistoryEventType EventType { get; set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who triggered the event (if applicable).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Event details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Previous severity (for escalation events).
    /// </summary>
    public AlarmSeverity? PreviousSeverity { get; set; }

    /// <summary>
    /// New severity (for escalation events).
    /// </summary>
    public AlarmSeverity? NewSeverity { get; set; }

    /// <summary>
    /// Source value that triggered the alarm (for threshold-based alarms).
    /// </summary>
    public double? SourceValue { get; set; }

    /// <summary>
    /// Threshold value (for threshold-based alarms).
    /// </summary>
    public double? ThresholdValue { get; set; }
}

/// <summary>
/// Alarm history event types.
/// </summary>
public enum AlarmHistoryEventType
{
    Created,
    Acknowledged,
    Escalated,
    Cleared,
    Grouped,
    Correlated,
    Suppressed
}

/// <summary>
/// Alarm trend analysis data.
/// </summary>
public class AlarmTrend
{
    /// <summary>
    /// Time period start.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Time period end.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total alarms in period.
    /// </summary>
    public int TotalAlarms { get; set; }

    /// <summary>
    /// Alarms by severity.
    /// </summary>
    public Dictionary<AlarmSeverity, int> AlarmsBySeverity { get; set; } = new();

    /// <summary>
    /// Alarms by source type.
    /// </summary>
    public Dictionary<string, int> AlarmsBySourceType { get; set; } = new();

    /// <summary>
    /// Average time to acknowledge (in seconds).
    /// </summary>
    public double? AverageAcknowledgeTimeSeconds { get; set; }

    /// <summary>
    /// Average time to clear (in seconds).
    /// </summary>
    public double? AverageClearTimeSeconds { get; set; }

    /// <summary>
    /// Most frequent alarm types.
    /// </summary>
    public List<AlarmFrequency> MostFrequentAlarms { get; set; } = new();
}

/// <summary>
/// Alarm frequency data.
/// </summary>
public class AlarmFrequency
{
    public string AlarmTitle { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Alarm group for correlated alarms.
/// </summary>
public class AlarmGroup
{
    /// <summary>
    /// Group identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Grouping strategy used.
    /// </summary>
    public AlarmGroupingStrategy Strategy { get; set; }

    /// <summary>
    /// Alarms in this group.
    /// </summary>
    public List<string> AlarmIds { get; set; } = new();

    /// <summary>
    /// Group created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Group status.
    /// </summary>
    public AlarmGroupStatus Status { get; set; } = AlarmGroupStatus.Active;
}

/// <summary>
/// Alarm group status.
/// </summary>
public enum AlarmGroupStatus
{
    Active,
    Acknowledged,
    Resolved
}

