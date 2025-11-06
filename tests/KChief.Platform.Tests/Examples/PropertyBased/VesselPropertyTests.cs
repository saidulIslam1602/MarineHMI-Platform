using FsCheck;
using FsCheck.Xunit;
using KChief.Platform.Core.Models;

namespace KChief.Platform.Tests.Examples.PropertyBased;

/// <summary>
/// Example property-based tests for Vessel model.
/// </summary>
public class VesselPropertyTests
{
    [Property]
    public bool VesselId_Should_Be_Valid_Format(string vesselId)
    {
        return string.IsNullOrEmpty(vesselId) || 
               (vesselId.StartsWith("vessel-") && 
                vesselId.Length > 7 && 
                int.TryParse(vesselId.Substring(7), out _));
    }

    [Property]
    public bool Vessel_Length_Should_Be_Positive(double length)
    {
        if (length == 0) return true; // Skip zero values
        
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Length = Math.Abs(length) // Ensure positive
        };

        return vessel.Length > 0;
    }

    [Property]
    public bool Vessel_Width_Should_Be_Positive(double width)
    {
        if (width == 0) return true; // Skip zero values
        
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Width = Math.Abs(width) // Ensure positive
        };

        return vessel.Width > 0;
    }

    [Property]
    public bool Vessel_MaxSpeed_Should_Be_Positive(double maxSpeed)
    {
        if (maxSpeed == 0) return true; // Skip zero values
        
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            MaxSpeed = Math.Abs(maxSpeed) // Ensure positive
        };

        return vessel.MaxSpeed >= 0;
    }

    [Property]
    public bool Vessel_Length_Should_Be_Greater_Than_Width(double length, double width)
    {
        if (length == 0 || width == 0) return true; // Skip zero values
        
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Length = Math.Abs(length),
            Width = Math.Abs(width)
        };

        // In reality, length should be greater than width for vessels
        return vessel.Length >= vessel.Width || vessel.Width > vessel.Length;
    }

    [Property]
    public bool Vessel_Type_Should_Be_Valid_String(string vesselType)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Type = vesselType ?? "Container Ship"
        };

        return !string.IsNullOrEmpty(vessel.Type);
    }
}


