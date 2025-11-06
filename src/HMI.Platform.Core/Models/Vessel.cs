// ================================================================
// HMI Marine Automation Platform
// ================================================================
// File: Vessel.cs
// Project: HMI.Platform.Core
// Created: 2025
// Author: HMI Development Team
// 
// Description:
// Core domain model representing a marine vessel in the automation
// platform. Contains vessel identification, operational status,
// location tracking, and engine management properties.
//
// Dependencies:
// - System.ComponentModel.DataAnnotations: Validation attributes
//
// Copyright (c) 2025 HMI Marine Automation Platform
// Licensed under MIT License
// ================================================================

namespace HMI.Platform.Core.Models;

/// <summary>
/// Represents a marine vessel in the HMI automation system with comprehensive operational data.
/// </summary>
/// <remarks>
/// This class serves as the primary domain model for vessel entities within the marine
/// automation platform. It encapsulates all essential vessel information including
/// identification, operational status, location tracking, and engine management data.
/// 
/// Key Features:
/// - Unique vessel identification and classification
/// - Real-time operational status tracking
/// - Geographic location monitoring with coordinates
/// - Engine collection management for multi-engine vessels
/// - Timestamp tracking for operational events
/// 
/// The vessel model supports various vessel types including:
/// - Container Ships
/// - Tankers  
/// - Cruise Ships
/// - Cargo Vessels
/// - Naval Vessels
/// 
/// Status Management:
/// The vessel maintains operational status through the VesselStatus enumeration,
/// supporting states from Offline through Operational to Emergency conditions.
/// </remarks>
/// <example>
/// <code>
/// var vessel = new Vessel
/// {
///     Id = "VESSEL-001",
///     Name = "Atlantic Explorer",
///     Type = "Container Ship",
///     Status = VesselStatus.Operational,
///     Location = new Location 
///     { 
///         Latitude = 40.7128, 
///         Longitude = -74.0060,
///         Timestamp = DateTime.UtcNow
///     }
/// };
/// </code>
/// </example>
public class Vessel
{
    /// <summary>
    /// Unique identifier for the vessel.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the vessel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vessel type (e.g., "Container Ship", "Tanker", "Cruise Ship").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Current operational status of the vessel.
    /// </summary>
    public VesselStatus Status { get; set; } = VesselStatus.Offline;

    /// <summary>
    /// Current location of the vessel.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Length of the vessel in meters.
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    /// Width of the vessel in meters.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Maximum speed of the vessel in knots.
    /// </summary>
    public double MaxSpeed { get; set; }

    /// <summary>
    /// List of engines on the vessel.
    /// </summary>
    public List<Engine> Engines { get; set; } = new();

    /// <summary>
    /// Timestamp when the vessel was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the vessel was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Operational status of a vessel.
/// </summary>
public enum VesselStatus
{
    Offline,
    Online,
    Maintenance,
    Emergency
}

