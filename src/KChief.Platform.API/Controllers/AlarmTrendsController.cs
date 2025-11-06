using KChief.AlarmSystem.Services;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;

namespace KChief.Platform.API.Controllers;

/// <summary>
/// Controller for alarm trends and analytics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireObserver")]
[Produces("application/json")]
public class AlarmTrendsController : ControllerBase
{
    private readonly AlarmHistoryService _historyService;
    private readonly IAlarmService _alarmService;
    private readonly ILogger<AlarmTrendsController> _logger;

    public AlarmTrendsController(
        AlarmHistoryService historyService,
        IAlarmService alarmService,
        ILogger<AlarmTrendsController> logger)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets alarm trends for a time period.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AlarmTrend), StatusCodes.Status200OK)]
    public ActionResult<AlarmTrend> GetTrends(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        using (LogContext.PushProperty("StartDate", start))
        using (LogContext.PushProperty("EndDate", end))
        {
            var trend = _historyService.GetTrends(start, end);
            Log.Information("Alarm trends retrieved for period {StartDate} to {EndDate}", start, end);
            return Ok(trend);
        }
    }

    /// <summary>
    /// Gets alarm history for a specific alarm.
    /// </summary>
    [HttpGet("alarm/{alarmId}/history")]
    [ProducesResponseType(typeof(IEnumerable<AlarmHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<AlarmHistory>> GetAlarmHistory(string alarmId)
    {
        var alarm = _alarmService.GetAlarmByIdAsync(alarmId).Result;
        if (alarm == null)
        {
            return NotFound($"Alarm with ID '{alarmId}' not found.");
        }

        var history = _historyService.GetAlarmHistory(alarmId);
        return Ok(history);
    }
}

