using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KChief.Platform.API.Controllers;

/// <summary>
/// Controller for engine control operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnginesController : ControllerBase
{
    private readonly IVesselControlService _vesselControlService;
    private readonly ILogger<EnginesController> _logger;

    public EnginesController(IVesselControlService vesselControlService, ILogger<EnginesController> logger)
    {
        _vesselControlService = vesselControlService;
        _logger = logger;
    }

    /// <summary>
    /// Starts an engine.
    /// </summary>
    /// <param name="vesselId">Vessel identifier</param>
    /// <param name="engineId">Engine identifier</param>
    /// <returns>Operation result</returns>
    /// <response code="200">Engine start command accepted</response>
    /// <response code="400">Engine cannot be started (already running or invalid state)</response>
    /// <response code="404">Vessel or engine not found</response>
    [HttpPost("{vesselId}/engines/{engineId}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartEngine(string vesselId, string engineId)
    {
        _logger.LogInformation("Starting engine {EngineId} for vessel {VesselId}", engineId, vesselId);
        
        var engine = await _vesselControlService.GetEngineByIdAsync(vesselId, engineId);
        if (engine == null)
        {
            return NotFound($"Engine with ID '{engineId}' not found for vessel '{vesselId}'.");
        }

        if (engine.Status != EngineStatus.Stopped)
        {
            return BadRequest($"Engine cannot be started. Current status: {engine.Status}");
        }

        var result = await _vesselControlService.StartEngineAsync(vesselId, engineId);
        
        if (result)
        {
            return Ok(new { message = "Engine start command accepted", vesselId, engineId });
        }

        return BadRequest("Failed to start engine.");
    }

    /// <summary>
    /// Stops an engine.
    /// </summary>
    /// <param name="vesselId">Vessel identifier</param>
    /// <param name="engineId">Engine identifier</param>
    /// <returns>Operation result</returns>
    /// <response code="200">Engine stop command accepted</response>
    /// <response code="400">Engine cannot be stopped (already stopped or invalid state)</response>
    /// <response code="404">Vessel or engine not found</response>
    [HttpPost("{vesselId}/engines/{engineId}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StopEngine(string vesselId, string engineId)
    {
        _logger.LogInformation("Stopping engine {EngineId} for vessel {VesselId}", engineId, vesselId);
        
        var engine = await _vesselControlService.GetEngineByIdAsync(vesselId, engineId);
        if (engine == null)
        {
            return NotFound($"Engine with ID '{engineId}' not found for vessel '{vesselId}'.");
        }

        if (engine.Status != EngineStatus.Running)
        {
            return BadRequest($"Engine cannot be stopped. Current status: {engine.Status}");
        }

        var result = await _vesselControlService.StopEngineAsync(vesselId, engineId);
        
        if (result)
        {
            return Ok(new { message = "Engine stop command accepted", vesselId, engineId });
        }

        return BadRequest("Failed to stop engine.");
    }

    /// <summary>
    /// Sets the RPM for an engine.
    /// </summary>
    /// <param name="vesselId">Vessel identifier</param>
    /// <param name="engineId">Engine identifier</param>
    /// <param name="request">RPM set request</param>
    /// <returns>Operation result</returns>
    /// <response code="200">RPM set successfully</response>
    /// <response code="400">Invalid RPM value or engine not running</response>
    /// <response code="404">Vessel or engine not found</response>
    [HttpPost("{vesselId}/engines/{engineId}/rpm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetEngineRpm(string vesselId, string engineId, [FromBody] SetRpmRequest request)
    {
        _logger.LogInformation("Setting RPM to {Rpm} for engine {EngineId} on vessel {VesselId}", 
            request.Rpm, engineId, vesselId);
        
        var engine = await _vesselControlService.GetEngineByIdAsync(vesselId, engineId);
        if (engine == null)
        {
            return NotFound($"Engine with ID '{engineId}' not found for vessel '{vesselId}'.");
        }

        if (engine.Status != EngineStatus.Running)
        {
            return BadRequest($"Engine must be running to set RPM. Current status: {engine.Status}");
        }

        if (request.Rpm < 0 || request.Rpm > engine.MaxRpm)
        {
            return BadRequest($"RPM must be between 0 and {engine.MaxRpm}.");
        }

        var result = await _vesselControlService.SetEngineRpmAsync(vesselId, engineId, request.Rpm);
        
        if (result)
        {
            return Ok(new { message = "RPM set successfully", vesselId, engineId, rpm = request.Rpm });
        }

        return BadRequest("Failed to set engine RPM.");
    }
}

/// <summary>
/// Request model for setting engine RPM.
/// </summary>
public record SetRpmRequest(int Rpm);

