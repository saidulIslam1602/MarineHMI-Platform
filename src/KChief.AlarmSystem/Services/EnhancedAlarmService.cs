using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Serilog;
using Serilog.Context;

namespace KChief.AlarmSystem.Services;

/// <summary>
/// Enhanced alarm service that integrates rule engine, escalation, grouping, and history.
/// </summary>
public class EnhancedAlarmService : IAlarmService
{
    private readonly AlarmService _baseAlarmService;
    private readonly AlarmRuleEngine _ruleEngine;
    private readonly AlarmEscalationService _escalationService;
    private readonly AlarmGroupingService _groupingService;
    private readonly AlarmHistoryService _historyService;
    private readonly ILogger<EnhancedAlarmService> _logger;

    public EnhancedAlarmService(
        AlarmService baseAlarmService,
        AlarmRuleEngine ruleEngine,
        AlarmEscalationService escalationService,
        AlarmGroupingService groupingService,
        AlarmHistoryService historyService,
        ILogger<EnhancedAlarmService> logger)
    {
        _baseAlarmService = baseAlarmService ?? throw new ArgumentNullException(nameof(baseAlarmService));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _escalationService = escalationService ?? throw new ArgumentNullException(nameof(escalationService));
        _groupingService = groupingService ?? throw new ArgumentNullException(nameof(groupingService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Forward events from base service
        _baseAlarmService.AlarmCreated += (sender, alarm) => AlarmCreated?.Invoke(this, alarm);
        _baseAlarmService.AlarmAcknowledged += (sender, alarm) => AlarmAcknowledged?.Invoke(this, alarm);
        _baseAlarmService.AlarmCleared += (sender, alarm) => AlarmCleared?.Invoke(this, alarm);
    }

    public event EventHandler<Alarm>? AlarmCreated;
    public event EventHandler<Alarm>? AlarmAcknowledged;
    public event EventHandler<Alarm>? AlarmCleared;

    public Task<IEnumerable<Alarm>> GetActiveAlarmsAsync()
    {
        return _baseAlarmService.GetActiveAlarmsAsync();
    }

    public Task<IEnumerable<Alarm>> GetAllAlarmsAsync()
    {
        return _baseAlarmService.GetAllAlarmsAsync();
    }

    public Task<Alarm?> GetAlarmByIdAsync(string alarmId)
    {
        return _baseAlarmService.GetAlarmByIdAsync(alarmId);
    }

    public async Task<Alarm> CreateAlarmAsync(
        string title,
        string description,
        AlarmSeverity severity,
        string? vesselId = null,
        string? engineId = null,
        string? sensorId = null,
        string? ruleId = null,
        double? sourceValue = null,
        double? thresholdValue = null,
        AlarmEscalationConfig? escalationConfig = null,
        AlarmGroupingConfig? groupingConfig = null)
    {
        using (LogContext.PushProperty("Title", title))
        using (LogContext.PushProperty("Severity", severity))
        {
            // Create alarm using base service
            var alarm = await _baseAlarmService.CreateAlarmAsync(
                title, description, severity, vesselId, engineId, sensorId);

            // Set additional properties
            alarm.RuleId = ruleId;
            alarm.SourceValue = sourceValue;
            alarm.ThresholdValue = thresholdValue;

            // Register for escalation if configured
            if (escalationConfig != null)
            {
                _escalationService.RegisterAlarmForEscalation(alarm, escalationConfig);
            }

            // Group alarm if configured
            if (groupingConfig != null)
            {
                var groupId = await _groupingService.GroupAlarmAsync(alarm, groupingConfig);
                alarm.GroupId = groupId;
            }

            Log.Information("Enhanced alarm created: {AlarmId} - {Title}", alarm.Id, title);
            return alarm;
        }
    }

    public Task<bool> AcknowledgeAlarmAsync(string alarmId, string acknowledgedBy)
    {
        return _baseAlarmService.AcknowledgeAlarmAsync(alarmId, acknowledgedBy);
    }

    public Task<bool> ClearAlarmAsync(string alarmId)
    {
        return _baseAlarmService.ClearAlarmAsync(alarmId);
    }

    /// <summary>
    /// Evaluates sensor value against alarm rules.
    /// </summary>
    public async Task EvaluateSensorValueAsync(string sensorId, double value, string? vesselId = null, string? engineId = null)
    {
        await _ruleEngine.EvaluateSensorValueAsync(sensorId, value, vesselId, engineId);
    }

    /// <summary>
    /// Evaluates engine status against alarm rules.
    /// </summary>
    public async Task EvaluateEngineStatusAsync(string engineId, EngineStatus status, Dictionary<string, double>? metrics = null, string? vesselId = null)
    {
        await _ruleEngine.EvaluateEngineStatusAsync(engineId, status, metrics, vesselId);
    }

    /// <summary>
    /// Registers an alarm rule.
    /// </summary>
    public void RegisterRule(AlarmRule rule)
    {
        _ruleEngine.RegisterRule(rule);
    }

    /// <summary>
    /// Gets alarm trends for a time period.
    /// </summary>
    public AlarmTrend GetTrends(DateTime startDate, DateTime endDate)
    {
        return _historyService.GetTrends(startDate, endDate);
    }

    /// <summary>
    /// Gets alarm history for an alarm.
    /// </summary>
    public IEnumerable<AlarmHistory> GetAlarmHistory(string alarmId)
    {
        return _historyService.GetAlarmHistory(alarmId);
    }

    /// <summary>
    /// Gets alarm groups.
    /// </summary>
    public IEnumerable<AlarmGroup> GetGroups()
    {
        return _groupingService.GetGroups();
    }
}

