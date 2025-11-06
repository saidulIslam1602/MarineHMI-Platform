using KChief.Platform.Core.Models;
using Xunit;

namespace KChief.Platform.Tests.Models;

public class VesselTests
{
    [Fact]
    public void Vessel_Initialization_SetsDefaultValues()
    {
        // Act
        var vessel = new Vessel();

        // Assert
        Assert.NotNull(vessel);
        Assert.Equal(string.Empty, vessel.Id);
        Assert.Equal(string.Empty, vessel.Name);
        Assert.Equal(string.Empty, vessel.Type);
        Assert.Equal(VesselStatus.Offline, vessel.Status);
        Assert.NotNull(vessel.Engines);
        Assert.Empty(vessel.Engines);
    }

    [Fact]
    public void Vessel_WithEngines_ContainsEngines()
    {
        // Arrange
        var vessel = new Vessel
        {
            Id = "test-vessel",
            Name = "Test Vessel",
            Engines = new List<Engine>
            {
                new Engine { Id = "engine-1", Name = "Engine 1" },
                new Engine { Id = "engine-2", Name = "Engine 2" }
            }
        };

        // Assert
        Assert.Equal(2, vessel.Engines.Count);
        Assert.Equal("engine-1", vessel.Engines[0].Id);
        Assert.Equal("engine-2", vessel.Engines[1].Id);
    }
}

public class EngineTests
{
    [Fact]
    public void Engine_Initialization_SetsDefaultValues()
    {
        // Act
        var engine = new Engine();

        // Assert
        Assert.NotNull(engine);
        Assert.Equal(string.Empty, engine.Id);
        Assert.Equal(string.Empty, engine.Name);
        Assert.Equal(string.Empty, engine.Type);
        Assert.Equal(EngineStatus.Stopped, engine.Status);
        Assert.Equal(0, engine.Rpm);
        Assert.Equal(1000, engine.MaxRpm);
        Assert.Equal(0, engine.Temperature);
        Assert.Equal(0, engine.OilPressure);
        Assert.Equal(0, engine.FuelConsumption);
    }

    [Fact]
    public void Engine_WithValidRpm_IsWithinLimits()
    {
        // Arrange
        var engine = new Engine
        {
            MaxRpm = 1000,
            Rpm = 750
        };

        // Assert
        Assert.True(engine.Rpm >= 0);
        Assert.True(engine.Rpm <= engine.MaxRpm);
    }
}

public class SensorTests
{
    [Fact]
    public void Sensor_Initialization_SetsDefaultValues()
    {
        // Act
        var sensor = new Sensor();

        // Assert
        Assert.NotNull(sensor);
        Assert.Equal(string.Empty, sensor.Id);
        Assert.Equal(string.Empty, sensor.Name);
        Assert.Equal(string.Empty, sensor.Type);
        Assert.Equal(string.Empty, sensor.Unit);
        Assert.Equal(0, sensor.Value);
        Assert.Equal(0, sensor.MinValue);
        Assert.Equal(0, sensor.MaxValue);
        Assert.True(sensor.IsActive);
    }

    [Fact]
    public void Sensor_ValueWithinRange_IsValid()
    {
        // Arrange
        var sensor = new Sensor
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 50
        };

        // Assert
        Assert.True(sensor.Value >= sensor.MinValue);
        Assert.True(sensor.Value <= sensor.MaxValue);
    }
}

