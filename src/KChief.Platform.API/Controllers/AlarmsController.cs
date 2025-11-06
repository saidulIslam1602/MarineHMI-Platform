using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KChief.Platform.API.Controllers;

/// <summary>
/// Controller for alarm management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlarmsController : ControllerBase
{
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmsController> _logger;

    public AlarmsController(IAlarmService alarmService, ILogger<AlarmsController> logger)
    {
        _alarmService = alarmService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all alarms.
    /// </summary>
    /// <returns>List of alarms</returns>
    /// <response code="200">Returns the list of alarms</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Alarm>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetAllAlarms()
    {
        _logger.LogInformation("Getting all alarms");
        var alarms = await _alarmService.GetAllAlarmsAsync();
        return Ok(alarms);
    }

    /// <summary>
    /// Gets all active alarms.
    /// </summary>
    /// <returns>List of active alarms</returns>
    /// <response code="200">Returns the list of active alarms</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<Alarm>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetActiveAlarms()
    {
        _logger.LogInformation("Getting active alarms");
        var alarms = await _alarmService.GetActiveAlarmsAsync();
        return Ok(alarms);
    }

    /// <summary>
    /// Gets a specific alarm by its ID.
    /// </summary>
    /// <param name="id">Alarm identifier</param>
    /// <returns>Alarm information</returns>
    /// <response code="200">Returns the alarm</response>
    /// <response code="404">Alarm not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Alarm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Alarm>> GetAlarmById(string id)
    {
        _logger.LogInformation("Getting alarm with ID: {AlarmId}", id);
        var alarm = await _alarmService.GetAlarmByIdAsync(id);
        
        if (alarm == null)
        {
            return NotFound($"Alarm with ID '{id}' not found.");
        }

        return Ok(alarm);
    }

    /// <summary>
    /// Creates a new alarm.
    /// </summary>
    /// <param name="request">Alarm creation request</param>
    /// <returns>Created alarm</returns>
    /// <response code="201">Alarm created successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost]
    [ProducesResponseType(typeof(Alarm), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Alarm>> CreateAlarm([FromBody] CreateAlarmRequest request)
    {
        _logger.LogInformation("Creating alarm: {Title}", request.Title);
        
        var alarm = await _alarmService.CreateAlarmAsync(
            request.Title,
            request.Description,
            request.Severity,
            request.VesselId,
            request.EngineId,
            request.SensorId);

        return CreatedAtAction(nameof(GetAlarmById), new { id = alarm.Id }, alarm);
    }

    /// <summary>
    /// Acknowledges an alarm.
    /// </summary>
    /// <param name="id">Alarm identifier</param>
    /// <param name="request">Acknowledgment request</param>
    /// <returns>Operation result</returns>
    /// <response code="200">Alarm acknowledged successfully</response>
    /// <response code="404">Alarm not found</response>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AcknowledgeAlarm(string id, [FromBody] AcknowledgeAlarmRequest request)
    {
        _logger.LogInformation("Acknowledging alarm {AlarmId} by {User}", id, request.AcknowledgedBy);
        
        var result = await _alarmService.AcknowledgeAlarmAsync(id, request.AcknowledgedBy);
        
        if (!result)
        {
            return NotFound($"Alarm with ID '{id}' not found or cannot be acknowledged.");
        }

        return Ok(new { message = "Alarm acknowledged successfully", alarmId = id });
    }

    /// <summary>
    /// Clears an alarm.
    /// </summary>
    /// <param name="id">Alarm identifier</param>
    /// <returns>Operation result</returns>
    /// <response code="200">Alarm cleared successfully</response>
    /// <response code="404">Alarm not found</response>
    [HttpPost("{id}/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ClearAlarm(string id)
    {
        _logger.LogInformation("Clearing alarm {AlarmId}", id);
        
        var result = await _alarmService.ClearAlarmAsync(id);
        
        if (!result)
        {
            return NotFound($"Alarm with ID '{id}' not found or cannot be cleared.");
        }

        return Ok(new { message = "Alarm cleared successfully", alarmId = id });
    }
}

/// <summary>
/// Request model for creating an alarm.
/// </summary>
public record CreateAlarmRequest(
    string Title,
    string Description,
    AlarmSeverity Severity,
    string? VesselId = null,
    string? EngineId = null,
    string? SensorId = null);

/// <summary>
/// Request model for acknowledging an alarm.
/// </summary>
public record AcknowledgeAlarmRequest(string AcknowledgedBy);

