using KChief.AlarmSystem.Services;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;

namespace KChief.Platform.API.Controllers;

/// <summary>
/// Controller for managing alarm rules.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireOperator")]
[Produces("application/json")]
public class AlarmRulesController : ControllerBase
{
    private readonly AlarmRuleEngine _ruleEngine;
    private readonly ILogger<AlarmRulesController> _logger;

    public AlarmRulesController(
        AlarmRuleEngine ruleEngine,
        ILogger<AlarmRulesController> logger)
    {
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all alarm rules.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlarmRule>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AlarmRule>> GetRules()
    {
        var rules = _ruleEngine.GetRules();
        return Ok(rules);
    }

    /// <summary>
    /// Gets a specific alarm rule by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlarmRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AlarmRule> GetRule(string id)
    {
        var rule = _ruleEngine.GetRule(id);
        if (rule == null)
        {
            return NotFound($"Rule with ID '{id}' not found.");
        }
        return Ok(rule);
    }

    /// <summary>
    /// Creates a new alarm rule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlarmRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AlarmRule> CreateRule([FromBody] CreateAlarmRuleRequest request)
    {
        using (LogContext.PushProperty("RuleName", request.Name))
        {
            var rule = new AlarmRule
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                RuleType = request.RuleType,
                IsEnabled = request.IsEnabled,
                SourceType = request.SourceType,
                SourceIdPattern = request.SourceIdPattern,
                Condition = request.Condition ?? string.Empty,
                ThresholdValue = request.ThresholdValue,
                ThresholdOperator = request.ThresholdOperator,
                Severity = request.Severity,
                AlarmTitleTemplate = request.AlarmTitleTemplate,
                AlarmDescriptionTemplate = request.AlarmDescriptionTemplate,
                DurationThresholdSeconds = request.DurationThresholdSeconds,
                CooldownSeconds = request.CooldownSeconds,
                Escalation = request.Escalation,
                Grouping = request.Grouping,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _ruleEngine.RegisterRule(rule);
            Log.Information("Alarm rule created: {RuleId} - {RuleName}", rule.Id, rule.Name);

            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
        }
    }

    /// <summary>
    /// Updates an alarm rule.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AlarmRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AlarmRule> UpdateRule(string id, [FromBody] UpdateAlarmRuleRequest request)
    {
        var existingRule = _ruleEngine.GetRule(id);
        if (existingRule == null)
        {
            return NotFound($"Rule with ID '{id}' not found.");
        }

        // Update properties
        existingRule.Name = request.Name ?? existingRule.Name;
        existingRule.Description = request.Description ?? existingRule.Description;
        existingRule.IsEnabled = request.IsEnabled ?? existingRule.IsEnabled;
        existingRule.Severity = request.Severity ?? existingRule.Severity;
        existingRule.ThresholdValue = request.ThresholdValue ?? existingRule.ThresholdValue;
        existingRule.ThresholdOperator = request.ThresholdOperator ?? existingRule.ThresholdOperator;
        existingRule.CooldownSeconds = request.CooldownSeconds ?? existingRule.CooldownSeconds;
        existingRule.LastModified = DateTime.UtcNow;

        _ruleEngine.RegisterRule(existingRule); // Re-register with updates
        Log.Information("Alarm rule updated: {RuleId}", id);

        return Ok(existingRule);
    }

    /// <summary>
    /// Deletes an alarm rule.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteRule(string id)
    {
        var rule = _ruleEngine.GetRule(id);
        if (rule == null)
        {
            return NotFound($"Rule with ID '{id}' not found.");
        }

        _ruleEngine.UnregisterRule(id);
        Log.Information("Alarm rule deleted: {RuleId}", id);

        return NoContent();
    }
}

/// <summary>
/// Request model for creating an alarm rule.
/// </summary>
public record CreateAlarmRuleRequest(
    string Name,
    string Description,
    AlarmRuleType RuleType,
    string SourceType,
    bool IsEnabled = true,
    string? SourceIdPattern = null,
    string? Condition = null,
    double? ThresholdValue = null,
    ThresholdOperator? ThresholdOperator = null,
    AlarmSeverity Severity = AlarmSeverity.Warning,
    string AlarmTitleTemplate = "{SourceId} threshold exceeded",
    string AlarmDescriptionTemplate = "Value {Value} exceeded threshold",
    int? DurationThresholdSeconds = null,
    int CooldownSeconds = 60,
    AlarmEscalationConfig? Escalation = null,
    AlarmGroupingConfig? Grouping = null);

/// <summary>
/// Request model for updating an alarm rule.
/// </summary>
public record UpdateAlarmRuleRequest(
    string? Name = null,
    string? Description = null,
    bool? IsEnabled = null,
    AlarmSeverity? Severity = null,
    double? ThresholdValue = null,
    ThresholdOperator? ThresholdOperator = null,
    int? CooldownSeconds = null);

