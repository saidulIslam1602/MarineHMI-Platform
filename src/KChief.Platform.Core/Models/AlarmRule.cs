namespace KChief.Platform.Core.Models;

/// <summary>
/// Represents an alarm rule that defines when and how alarms should be triggered.
/// </summary>
public class AlarmRule
{
    /// <summary>
    /// Unique identifier for the rule.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Rule type (Threshold, Pattern, Condition, etc.).
    /// </summary>
    public AlarmRuleType RuleType { get; set; }

    /// <summary>
    /// Whether the rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Source entity type (Sensor, Engine, Vessel, etc.).
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Source entity ID pattern (can use wildcards).
    /// </summary>
    public string? SourceIdPattern { get; set; }

    /// <summary>
    /// Condition expression (e.g., "Temperature > 100").
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Threshold value for threshold-based rules.
    /// </summary>
    public double? ThresholdValue { get; set; }

    /// <summary>
    /// Threshold comparison operator.
    /// </summary>
    public ThresholdOperator? ThresholdOperator { get; set; }

    /// <summary>
    /// Alarm severity when rule triggers.
    /// </summary>
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;

    /// <summary>
    /// Alarm title template (can use placeholders like {SourceId}, {Value}).
    /// </summary>
    public string AlarmTitleTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Alarm description template.
    /// </summary>
    public string AlarmDescriptionTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Duration threshold in seconds before triggering (for sustained conditions).
    /// </summary>
    public int? DurationThresholdSeconds { get; set; }

    /// <summary>
    /// Cooldown period in seconds before rule can trigger again.
    /// </summary>
    public int CooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Escalation configuration.
    /// </summary>
    public AlarmEscalationConfig? Escalation { get; set; }

    /// <summary>
    /// Grouping configuration.
    /// </summary>
    public AlarmGroupingConfig? Grouping { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Alarm rule types.
/// </summary>
public enum AlarmRuleType
{
    /// <summary>
    /// Threshold-based rule (value crosses threshold).
    /// </summary>
    Threshold,

    /// <summary>
    /// Pattern-based rule (matches pattern).
    /// </summary>
    Pattern,

    /// <summary>
    /// Condition-based rule (evaluates expression).
    /// </summary>
    Condition,

    /// <summary>
    /// Rate-based rule (rate of change).
    /// </summary>
    RateOfChange,

    /// <summary>
    /// Correlation-based rule (multiple conditions).
    /// </summary>
    Correlation
}

/// <summary>
/// Threshold comparison operators.
/// </summary>
public enum ThresholdOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual
}

/// <summary>
/// Alarm escalation configuration.
/// </summary>
public class AlarmEscalationConfig
{
    /// <summary>
    /// Whether escalation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Time in seconds before escalation.
    /// </summary>
    public int EscalationTimeSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Severity to escalate to.
    /// </summary>
    public AlarmSeverity EscalateToSeverity { get; set; } = AlarmSeverity.Critical;

    /// <summary>
    /// Maximum escalation level.
    /// </summary>
    public int MaxEscalationLevel { get; set; } = 3;

    /// <summary>
    /// Notification channels for escalation.
    /// </summary>
    public List<string> NotificationChannels { get; set; } = new();
}

/// <summary>
/// Alarm grouping configuration.
/// </summary>
public class AlarmGroupingConfig
{
    /// <summary>
    /// Whether grouping is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Grouping strategy.
    /// </summary>
    public AlarmGroupingStrategy Strategy { get; set; } = AlarmGroupingStrategy.BySource;

    /// <summary>
    /// Time window in seconds for grouping alarms.
    /// </summary>
    public int TimeWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum alarms per group.
    /// </summary>
    public int MaxAlarmsPerGroup { get; set; } = 10;
}

/// <summary>
/// Alarm grouping strategies.
/// </summary>
public enum AlarmGroupingStrategy
{
    /// <summary>
    /// Group by source entity.
    /// </summary>
    BySource,

    /// <summary>
    /// Group by severity.
    /// </summary>
    BySeverity,

    /// <summary>
    /// Group by rule.
    /// </summary>
    ByRule,

    /// <summary>
    /// Group by time window.
    /// </summary>
    ByTimeWindow,

    /// <summary>
    /// Group by vessel.
    /// </summary>
    ByVessel
}

