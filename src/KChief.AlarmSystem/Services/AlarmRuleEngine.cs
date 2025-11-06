using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Serilog;
using Serilog.Context;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Engine for evaluating alarm rules and triggering alarms automatically.
/// </summary>
public class AlarmRuleEngine
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmRuleEngine> _logger;
    private readonly ConcurrentDictionary<string, AlarmRule> _rules = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastTriggered = new();
    private readonly ConcurrentDictionary<string, DateTime> _conditionStartTimes = new();

    public AlarmRuleEngine(
        IAlarmService alarmService,
        ILogger<AlarmRuleEngine> logger)
    {
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers an alarm rule.
    /// </summary>
    public void RegisterRule(AlarmRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Id))
        {
            rule.Id = Guid.NewGuid().ToString();
        }

        _rules[rule.Id] = rule;
        Log.Information("Alarm rule registered: {RuleId} - {RuleName}", rule.Id, rule.Name);
    }

    /// <summary>
    /// Unregisters an alarm rule.
    /// </summary>
    public void UnregisterRule(string ruleId)
    {
        if (_rules.TryRemove(ruleId, out var rule))
        {
            Log.Information("Alarm rule unregistered: {RuleId} - {RuleName}", ruleId, rule.Name);
        }
    }

    /// <summary>
    /// Gets all registered rules.
    /// </summary>
    public IEnumerable<AlarmRule> GetRules()
    {
        return _rules.Values;
    }

    /// <summary>
    /// Gets a rule by ID.
    /// </summary>
    public AlarmRule? GetRule(string ruleId)
    {
        _rules.TryGetValue(ruleId, out var rule);
        return rule;
    }

    /// <summary>
    /// Evaluates a sensor value against all applicable rules.
    /// </summary>
    public async Task EvaluateSensorValueAsync(string sensorId, double value, string? vesselId = null, string? engineId = null)
    {
        using (LogContext.PushProperty("SensorId", sensorId))
        using (LogContext.PushProperty("Value", value))
        {
            var applicableRules = _rules.Values
                .Where(r => r.IsEnabled && 
                           r.SourceType == "Sensor" &&
                           (r.SourceIdPattern == null || MatchesPattern(sensorId, r.SourceIdPattern)))
                .ToList();

            foreach (var rule in applicableRules)
            {
                await EvaluateRuleAsync(rule, value, sensorId, vesselId, engineId);
            }
        }
    }

    /// <summary>
    /// Evaluates an engine status against all applicable rules.
    /// </summary>
    public async Task EvaluateEngineStatusAsync(string engineId, EngineStatus status, Dictionary<string, double>? metrics = null, string? vesselId = null)
    {
        using (LogContext.PushProperty("EngineId", engineId))
        using (LogContext.PushProperty("Status", status))
        {
            var applicableRules = _rules.Values
                .Where(r => r.IsEnabled && 
                           r.SourceType == "Engine" &&
                           (r.SourceIdPattern == null || MatchesPattern(engineId, r.SourceIdPattern)))
                .ToList();

            foreach (var rule in applicableRules)
            {
                await EvaluateRuleAsync(rule, status, engineId, vesselId, metrics);
            }
        }
    }

    /// <summary>
    /// Evaluates a rule against a value.
    /// </summary>
    private async Task EvaluateRuleAsync(AlarmRule rule, double value, string sourceId, string? vesselId, string? engineId)
    {
        // Check cooldown
        if (_lastTriggered.TryGetValue(rule.Id, out var lastTriggered) &&
            DateTime.UtcNow - lastTriggered < TimeSpan.FromSeconds(rule.CooldownSeconds))
        {
            return;
        }

        bool shouldTrigger = false;

        switch (rule.RuleType)
        {
            case AlarmRuleType.Threshold:
                shouldTrigger = EvaluateThresholdRule(rule, value);
                break;

            case AlarmRuleType.Condition:
                shouldTrigger = EvaluateConditionRule(rule, value, sourceId);
                break;

            case AlarmRuleType.RateOfChange:
                // Rate of change evaluation would require historical data
                shouldTrigger = false; // Placeholder
                break;
        }

        if (shouldTrigger)
        {
            // Check duration threshold
            if (rule.DurationThresholdSeconds.HasValue)
            {
                var conditionKey = $"{rule.Id}:{sourceId}";
                if (!_conditionStartTimes.ContainsKey(conditionKey))
                {
                    _conditionStartTimes[conditionKey] = DateTime.UtcNow;
                    return; // Wait for duration threshold
                }

                var duration = DateTime.UtcNow - _conditionStartTimes[conditionKey];
                if (duration.TotalSeconds < rule.DurationThresholdSeconds.Value)
                {
                    return; // Condition hasn't persisted long enough
                }
            }

            await TriggerAlarmAsync(rule, value, sourceId, vesselId, engineId);
            _lastTriggered[rule.Id] = DateTime.UtcNow;
        }
        else
        {
            // Clear condition start time if condition is no longer met
            var conditionKey = $"{rule.Id}:{sourceId}";
            _conditionStartTimes.TryRemove(conditionKey, out _);
        }
    }

    /// <summary>
    /// Evaluates a threshold-based rule.
    /// </summary>
    private bool EvaluateThresholdRule(AlarmRule rule, double value)
    {
        if (!rule.ThresholdValue.HasValue || !rule.ThresholdOperator.HasValue)
        {
            return false;
        }

        var threshold = rule.ThresholdValue.Value;
        var op = rule.ThresholdOperator.Value;

        return op switch
        {
            ThresholdOperator.GreaterThan => value > threshold,
            ThresholdOperator.GreaterThanOrEqual => value >= threshold,
            ThresholdOperator.LessThan => value < threshold,
            ThresholdOperator.LessThanOrEqual => value <= threshold,
            ThresholdOperator.Equal => Math.Abs(value - threshold) < 0.001,
            ThresholdOperator.NotEqual => Math.Abs(value - threshold) >= 0.001,
            _ => false
        };
    }

    /// <summary>
    /// Evaluates a condition-based rule.
    /// </summary>
    private bool EvaluateConditionRule(AlarmRule rule, double value, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(rule.Condition))
        {
            return false;
        }

        try
        {
            // Simple condition evaluation (can be enhanced with expression parser)
            var condition = rule.Condition
                .Replace("{value}", value.ToString())
                .Replace("{Value}", value.ToString())
                .Replace("{sourceId}", sourceId)
                .Replace("{SourceId}", sourceId);

            // Basic comparison evaluation
            if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out var threshold))
                {
                    return value > threshold;
                }
            }
            else if (condition.Contains("<"))
            {
                var parts = condition.Split('<');
                if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out var threshold))
                {
                    return value < threshold;
                }
            }
            else if (condition.Contains(">="))
            {
                var parts = condition.Split(new[] { ">=" }, StringSplitOptions.None);
                if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out var threshold))
                {
                    return value >= threshold;
                }
            }
            else if (condition.Contains("<="))
            {
                var parts = condition.Split(new[] { "<=" }, StringSplitOptions.None);
                if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out var threshold))
                {
                    return value <= threshold;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error evaluating condition rule {RuleId}: {Condition}", rule.Id, rule.Condition);
            return false;
        }
    }

    /// <summary>
    /// Evaluates a rule against engine status.
    /// </summary>
    private async Task EvaluateRuleAsync(AlarmRule rule, EngineStatus status, string engineId, string? vesselId, Dictionary<string, double>? metrics)
    {
        // Engine-specific rule evaluation
        // This can be extended based on engine status and metrics
        if (status == EngineStatus.Error || status == EngineStatus.Overheated)
        {
            await TriggerAlarmAsync(rule, 0, engineId, vesselId, null);
        }
    }

    /// <summary>
    /// Triggers an alarm based on a rule.
    /// </summary>
    private async Task TriggerAlarmAsync(AlarmRule rule, double value, string sourceId, string? vesselId, string? engineId)
    {
        using (LogContext.PushProperty("RuleId", rule.Id))
        using (LogContext.PushProperty("SourceId", sourceId))
        {
            var title = FormatTemplate(rule.AlarmTitleTemplate, value, sourceId, vesselId, engineId);
            var description = FormatTemplate(rule.AlarmDescriptionTemplate, value, sourceId, vesselId, engineId);

            string? sensorId = rule.SourceType == "Sensor" ? sourceId : null;
            string? engineIdForAlarm = rule.SourceType == "Engine" ? sourceId : engineId;

            var alarm = await _alarmService.CreateAlarmAsync(
                title,
                description,
                rule.Severity,
                vesselId,
                engineIdForAlarm,
                sensorId);

            Log.Information("Alarm triggered by rule {RuleId}: {AlarmId} - {Title}", rule.Id, alarm.Id, title);
        }
    }

    /// <summary>
    /// Formats a template string with placeholders.
    /// </summary>
    private string FormatTemplate(string template, double value, string sourceId, string? vesselId, string? engineId)
    {
        return template
            .Replace("{Value}", value.ToString("F2"))
            .Replace("{value}", value.ToString("F2"))
            .Replace("{SourceId}", sourceId)
            .Replace("{sourceId}", sourceId)
            .Replace("{VesselId}", vesselId ?? "Unknown")
            .Replace("{vesselId}", vesselId ?? "Unknown")
            .Replace("{EngineId}", engineId ?? "Unknown")
            .Replace("{engineId}", engineId ?? "Unknown");
    }

    /// <summary>
    /// Checks if a string matches a pattern (supports wildcards).
    /// </summary>
    private bool MatchesPattern(string input, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}

