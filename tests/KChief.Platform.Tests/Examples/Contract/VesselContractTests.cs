using System.Text.Json;
using KChief.Platform.Core.Models;
using KChief.Platform.Tests.TestHelpers.Contract;
using Xunit;

namespace KChief.Platform.Tests.Examples.Contract;

/// <summary>
/// Example contract tests for Vessel API.
/// </summary>
public class VesselContractTests : ContractTestBase
{
    [Fact]
    public void Vessel_Contract_Should_Have_Required_Properties()
    {
        var vessel = new Vessel
        {
            Id = "vessel-001",
            Name = "Test Vessel",
            Type = "Container Ship",
            Status = VesselStatus.Online,
            Location = "Test Port",
            Length = 100.0,
            Width = 20.0,
            MaxSpeed = 25.0
        };

        AssertContractHasRequiredProperties(vessel, 
            "id", "name", "type", "status", "location", "length", "width", "maxSpeed");
    }

    [Fact]
    public void Vessel_Contract_Should_Serialize_And_Deserialize_Correctly()
    {
        var original = new Vessel
        {
            Id = "vessel-001",
            Name = "Test Vessel",
            Type = "Container Ship",
            Status = VesselStatus.Online,
            Location = "Test Port",
            Length = 100.0,
            Width = 20.0,
            MaxSpeed = 25.0,
            Engines = new List<Engine>(),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        AssertContractRoundTrip(original);
    }

    [Fact]
    public void Vessel_Contract_Should_Match_Expected_Schema()
    {
        var actual = new Vessel
        {
            Id = "vessel-001",
            Name = "Test Vessel",
            Type = "Container Ship",
            Status = VesselStatus.Online,
            Location = "Test Port",
            Length = 100.0,
            Width = 20.0,
            MaxSpeed = 25.0
        };

        var expected = new Vessel
        {
            Id = "vessel-001",
            Name = "Test Vessel",
            Type = "Container Ship",
            Status = VesselStatus.Online,
            Location = "Test Port",
            Length = 100.0,
            Width = 20.0,
            MaxSpeed = 25.0
        };

        AssertContractMatches(actual, expected, "Vessel");
    }
}

