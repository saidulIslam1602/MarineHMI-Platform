// ================================================================
// HMI Marine Automation Platform
// ================================================================
// File: VesselsController.cs
// Project: HMI.Platform.API
// Created: 2025
// Author: HMI Development Team
// 
// Description:
// RESTful API controller for vessel management operations in the
// marine automation platform. Provides endpoints for vessel
// monitoring, control, and status management.
//
// Dependencies:
// - HMI.Platform.Core: Core interfaces and models
// - Microsoft.AspNetCore.Mvc: ASP.NET Core MVC framework
//
// API Version: v1
// Base Route: /api/vessels
//
// Copyright (c) 2025 HMI Marine Automation Platform
// Licensed under MIT License
// ================================================================

using HMI.Platform.Core.Interfaces;
using HMI.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HMI.Platform.API.Controllers;

/// <summary>
/// RESTful API controller providing comprehensive vessel management operations for marine automation systems.
/// </summary>
/// <remarks>
/// This controller exposes HTTP endpoints for managing vessel operations including:
/// - Vessel discovery and registration
/// - Real-time status monitoring and updates
/// - Engine control and parameter management
/// - Operational data retrieval and analysis
/// 
/// All endpoints return standardized HTTP status codes and JSON responses.
/// Authentication and authorization are handled by the platform middleware.
/// 
/// Supported Operations:
/// - GET /api/vessels - Retrieve all registered vessels
/// - GET /api/vessels/{id} - Get specific vessel details
/// - POST /api/vessels/{id}/start - Start vessel operations
/// - POST /api/vessels/{id}/stop - Stop vessel operations
/// - PUT /api/vessels/{id}/rpm - Update engine RPM settings
/// 
/// Response Formats:
/// - Success responses include vessel data in JSON format
/// - Error responses follow RFC 7807 Problem Details standard
/// - All timestamps are in UTC format (ISO 8601)
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// GET /api/vessels
/// Accept: application/json
/// 
/// Response:
/// [
///   {
///     "id": "vessel-001",
///     "name": "Atlantic Explorer",
///     "type": "Container Ship",
///     "status": "Operational",
///     "location": { "latitude": 40.7128, "longitude": -74.0060 }
///   }
/// ]
/// </code>
/// </example>
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
    /// <remarks>
    /// Retrieves a list of all vessels registered in the system.
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/vessels
    /// Authorization: Bearer {token}
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// [
    ///   {
    ///     "id": "vessel-001",
    ///     "name": "MV Atlantic",
    ///     "imoNumber": "IMO1234567",
    ///     "callSign": "ATL1",
    ///     "type": "Cargo",
    ///     "length": 200.5,
    ///     "width": 32.0,
    ///     "draft": 12.5,
    ///     "status": "InService"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <returns>List of vessels</returns>
    /// <response code="200">Returns the list of vessels</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Vessel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Vessel>>> GetAllVessels()
    {
        _logger.LogInformation("Getting all vessels");
        var vessels = await _vesselControlService.GetAllVesselsAsync();
        return Ok(vessels);
    }

    /// <summary>
    /// Gets a specific vessel by its ID.
    /// </summary>
    /// <remarks>
    /// Retrieves detailed information about a specific vessel.
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/vessels/vessel-001
    /// Authorization: Bearer {token}
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "id": "vessel-001",
    ///   "name": "MV Atlantic",
    ///   "imoNumber": "IMO1234567",
    ///   "callSign": "ATL1",
    ///   "type": "Cargo",
    ///   "length": 200.5,
    ///   "width": 32.0,
    ///   "draft": 12.5,
    ///   "grossTonnage": 15000.0,
    ///   "flag": "US",
    ///   "status": "InService"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">Vessel identifier (e.g., "vessel-001")</param>
    /// <returns>Vessel information</returns>
    /// <response code="200">Returns the vessel</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Vessel not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Vessel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

