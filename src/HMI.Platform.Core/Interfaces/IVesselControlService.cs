// ================================================================
// HMI Marine Automation Platform
// ================================================================
// File: IVesselControlService.cs
// Project: HMI.Platform.Core
// Created: 2025
// Author: HMI Development Team
// 
// Description:
// Core service interface defining vessel control operations for
// the marine automation platform. Provides contract for vessel
// management, monitoring, and control functionality.
//
// Dependencies:
// - HMI.Platform.Core.Models: Core domain models
//
// Copyright (c) 2025 HMI Marine Automation Platform
// Licensed under MIT License
// ================================================================

using HMI.Platform.Core.Models;

namespace HMI.Platform.Core.Interfaces;

/// <summary>
/// Defines the contract for vessel control operations in the marine automation platform.
/// </summary>
/// <remarks>
/// This interface establishes the standard operations for vessel management including
/// discovery, monitoring, control, and status management. Implementations should
/// provide thread-safe, asynchronous operations with proper error handling.
/// 
/// Core Responsibilities:
/// - Vessel lifecycle management (discovery, registration, decommissioning)
/// - Real-time status monitoring and updates
/// - Engine control and parameter management
/// - Operational data retrieval and persistence
/// 
/// Implementation Guidelines:
/// - All methods should be implemented asynchronously using Task/Task&lt;T&gt;
/// - Null return values indicate vessel not found conditions
/// - Exceptions should be thrown for operational failures
/// - Thread safety must be maintained across all operations
/// </remarks>
public interface IVesselControlService
{
    /// <summary>
    /// Gets all vessels in the system.
    /// </summary>
    Task<IEnumerable<Vessel>> GetAllVesselsAsync();

    /// <summary>
    /// Gets a vessel by its unique identifier.
    /// </summary>
    Task<Vessel?> GetVesselByIdAsync(string vesselId);

    /// <summary>
    /// Gets all engines for a specific vessel.
    /// </summary>
    Task<IEnumerable<Engine>> GetVesselEnginesAsync(string vesselId);

    /// <summary>
    /// Gets a specific engine by vessel and engine ID.
    /// </summary>
    Task<Engine?> GetEngineByIdAsync(string vesselId, string engineId);

    /// <summary>
    /// Starts an engine.
    /// </summary>
    Task<bool> StartEngineAsync(string vesselId, string engineId);

    /// <summary>
    /// Stops an engine.
    /// </summary>
    Task<bool> StopEngineAsync(string vesselId, string engineId);

    /// <summary>
    /// Sets the RPM for an engine.
    /// </summary>
    Task<bool> SetEngineRPMAsync(string vesselId, string engineId, int rpm);

    /// <summary>
    /// Gets all sensors for a specific vessel.
    /// </summary>
    Task<IEnumerable<Sensor>> GetVesselSensorsAsync(string vesselId);
}

