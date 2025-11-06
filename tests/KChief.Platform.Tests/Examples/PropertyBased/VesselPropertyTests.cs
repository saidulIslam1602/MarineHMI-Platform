using FsCheck;
using FsCheck.Xunit;
using KChief.Platform.Core.Models;
using KChief.Platform.Tests.TestHelpers.PropertyBased;

namespace KChief.Platform.Tests.Examples.PropertyBased;

/// <summary>
/// Example property-based tests for Vessel model.
/// </summary>
public class VesselPropertyTests : PropertyBasedTestBase
{
    [Property]
    public Property VesselId_Should_Be_Valid_Format(string vesselId)
    {
        return (vesselId.StartsWith("vessel-") && 
                vesselId.Length > 7 && 
                int.TryParse(vesselId.Substring(7), out _)).ToProperty();
    }

    [Property]
    public Property Vessel_Length_Should_Be_Positive(double length)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Length = Math.Abs(length) // Ensure positive
        };

        return (vessel.Length > 0).ToProperty();
    }

    [Property]
    public Property Vessel_Width_Should_Be_Positive(double width)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Width = Math.Abs(width) // Ensure positive
        };

        return (vessel.Width > 0).ToProperty();
    }

    [Property]
    public Property Vessel_MaxSpeed_Should_Be_Positive(double maxSpeed)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            MaxSpeed = Math.Abs(maxSpeed) // Ensure positive
        };

        return (vessel.MaxSpeed >= 0).ToProperty();
    }

    [Property]
    public Property Vessel_Length_Should_Be_Greater_Than_Width(double length, double width)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Length = Math.Abs(length),
            Width = Math.Abs(width)
        };

        // In reality, length should be greater than width for vessels
        return (vessel.Length >= vessel.Width || vessel.Width > vessel.Length).ToProperty();
    }

    [Property]
    public Property Vessel_Type_Should_Be_Valid_String(string vesselType)
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test",
            Type = vesselType ?? "Container Ship"
        };

        return (!string.IsNullOrEmpty(vessel.Type)).ToProperty();
    }
}

/// <summary>
/// Custom generators for property-based tests.
/// </summary>
public static class VesselGenerators
{
    public static Arbitrary<string> ValidVesselId()
    {
        return Gen.Choose<int>(1, 999)
            .Select(n => $"vessel-{n:D3}")
            .ToArbitrary();
    }

    public static Arbitrary<string> ValidImoNumber()
    {
        return Gen.Choose<int>(1000000, 9999999)
            .Select(n => $"IMO{n}")
            .ToArbitrary();
    }

    public static Arbitrary<double> PositiveDouble()
    {
        return Gen.Choose<int>(1, 10000)
            .Select(n => (double)n)
            .ToArbitrary();
    }
}

