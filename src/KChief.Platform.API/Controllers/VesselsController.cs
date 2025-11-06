using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KChief.Platform.API.Controllers;

/// <summary>
/// Controller for vessel management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VesselsController : ControllerBase
{
    private readonly IVesselControlService _vesselControlService;
    private readonly ILogger<VesselsController> _logger;

    public VesselsController(IVesselControlService vesselControlService, ILogger<VesselsController> logger)
    {
        _vesselControlService = vesselControlService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all vessels in the system.
    /// </summary>
    /// <returns>List of vessels</returns>
    /// <response code="200">Returns the list of vessels</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Vessel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Vessel>>> GetAllVessels()
    {
        _logger.LogInformation("Getting all vessels");
        var vessels = await _vesselControlService.GetAllVesselsAsync();
        return Ok(vessels);
    }

    /// <summary>
    /// Gets a specific vessel by its ID.
    /// </summary>
    /// <param name="id">Vessel identifier</param>
    /// <returns>Vessel information</returns>
    /// <response code="200">Returns the vessel</response>
    /// <response code="404">Vessel not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Vessel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vessel>> GetVesselById(string id)
    {
        _logger.LogInformation("Getting vessel with ID: {VesselId}", id);
        var vessel = await _vesselControlService.GetVesselByIdAsync(id);
        
        if (vessel == null)
        {
            return NotFound($"Vessel with ID '{id}' not found.");
        }

        return Ok(vessel);
    }

    /// <summary>
    /// Gets all engines for a specific vessel.
    /// </summary>
    /// <param name="id">Vessel identifier</param>
    /// <returns>List of engines</returns>
    /// <response code="200">Returns the list of engines</response>
    /// <response code="404">Vessel not found</response>
    [HttpGet("{id}/engines")]
    [ProducesResponseType(typeof(IEnumerable<Engine>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Engine>>> GetVesselEngines(string id)
    {
        _logger.LogInformation("Getting engines for vessel ID: {VesselId}", id);
        
        var vessel = await _vesselControlService.GetVesselByIdAsync(id);
        if (vessel == null)
        {
            return NotFound($"Vessel with ID '{id}' not found.");
        }

        var engines = await _vesselControlService.GetVesselEnginesAsync(id);
        return Ok(engines);
    }

    /// <summary>
    /// Gets a specific engine by vessel ID and engine ID.
    /// </summary>
    /// <param name="vesselId">Vessel identifier</param>
    /// <param name="engineId">Engine identifier</param>
    /// <returns>Engine information</returns>
    /// <response code="200">Returns the engine</response>
    /// <response code="404">Vessel or engine not found</response>
    [HttpGet("{vesselId}/engines/{engineId}")]
    [ProducesResponseType(typeof(Engine), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Engine>> GetEngineById(string vesselId, string engineId)
    {
        _logger.LogInformation("Getting engine {EngineId} for vessel {VesselId}", engineId, vesselId);
        
        var engine = await _vesselControlService.GetEngineByIdAsync(vesselId, engineId);
        
        if (engine == null)
        {
            return NotFound($"Engine with ID '{engineId}' not found for vessel '{vesselId}'.");
        }

        return Ok(engine);
    }

    /// <summary>
    /// Gets all sensors for a specific vessel.
    /// </summary>
    /// <param name="id">Vessel identifier</param>
    /// <returns>List of sensors</returns>
    /// <response code="200">Returns the list of sensors</response>
    /// <response code="404">Vessel not found</response>
    [HttpGet("{id}/sensors")]
    [ProducesResponseType(typeof(IEnumerable<Sensor>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetVesselSensors(string id)
    {
        _logger.LogInformation("Getting sensors for vessel ID: {VesselId}", id);
        
        var vessel = await _vesselControlService.GetVesselByIdAsync(id);
        if (vessel == null)
        {
            return NotFound($"Vessel with ID '{id}' not found.");
        }

        var sensors = await _vesselControlService.GetVesselSensorsAsync(id);
        return Ok(sensors);
    }
}

