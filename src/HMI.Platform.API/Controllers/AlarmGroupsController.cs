using HMI.Platform.Core.Interfaces;
using HMI.Platform.Core.Models;
using HMI.AlarmSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;

namespace HMI.Platform.API.Controllers;

/// <summary>
/// Controller for alarm groups and correlation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireObserver")]
[Produces("application/json")]
public class AlarmGroupsController : ControllerBase
{
    private readonly AlarmGroupingService _groupingService;
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmGroupsController> _logger;

    public AlarmGroupsController(
        AlarmGroupingService groupingService,
        IAlarmService alarmService,
        ILogger<AlarmGroupsController> logger)
    {
        _groupingService = groupingService ?? throw new ArgumentNullException(nameof(groupingService));
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all alarm groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlarmGroup>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AlarmGroup>> GetGroups()
    {
        var groups = _groupingService.GetGroups();
        return Ok(groups);
    }

    /// <summary>
    /// Gets a specific alarm group by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlarmGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AlarmGroup> GetGroup(string id)
    {
        var group = _groupingService.GetGroup(id);
        if (group == null)
        {
            return NotFound($"Group with ID '{id}' not found.");
        }
        return Ok(group);
    }

    /// <summary>
    /// Gets the group for a specific alarm.
    /// </summary>
    [HttpGet("alarm/{alarmId}")]
    [ProducesResponseType(typeof(AlarmGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AlarmGroup> GetGroupForAlarm(string alarmId)
    {
        var alarm = _alarmService.GetAlarmByIdAsync(alarmId).Result;
        if (alarm == null)
        {
            return NotFound($"Alarm with ID '{alarmId}' not found.");
        }

        var group = _groupingService.GetGroupForAlarm(alarmId);
        if (group == null)
        {
            return NotFound($"No group found for alarm '{alarmId}'.");
        }

        return Ok(group);
    }

    /// <summary>
    /// Acknowledges all alarms in a group.
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AcknowledgeGroup(string id, [FromBody] AcknowledgeGroupRequest request)
    {
        var result = await _groupingService.AcknowledgeGroupAsync(id, request.AcknowledgedBy);
        
        if (!result)
        {
            return NotFound($"Group with ID '{id}' not found or could not be acknowledged.");
        }

        _logger.LogInformation("Alarm group {GroupId} acknowledged by {User}", id, request.AcknowledgedBy);
        return Ok(new { message = "Group acknowledged successfully", groupId = id });
    }
}

/// <summary>
/// Request model for acknowledging an alarm group.
/// </summary>
public record AcknowledgeGroupRequest(string AcknowledgedBy);

