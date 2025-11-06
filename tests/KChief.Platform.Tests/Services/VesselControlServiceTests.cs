using KChief.Platform.Core.Models;
using KChief.VesselControl.Services;
using Xunit;

namespace KChief.Platform.Tests.Services;

public class VesselControlServiceTests
{
    private readonly VesselControlService _service;

    public VesselControlServiceTests()
    {
        _service = new VesselControlService();
    }

    [Fact]
    public async Task GetAllVesselsAsync_ReturnsVessels()
    {
        // Act
        var vessels = await _service.GetAllVesselsAsync();

        // Assert
        Assert.NotNull(vessels);
        Assert.NotEmpty(vessels);
    }

    [Fact]
    public async Task GetVesselByIdAsync_WithValidId_ReturnsVessel()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var vessel = await _service.GetVesselByIdAsync(vesselId);

        // Assert
        Assert.NotNull(vessel);
        Assert.Equal(vesselId, vessel.Id);
        Assert.NotEmpty(vessel.Name);
    }

    [Fact]
    public async Task GetVesselByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var vesselId = "invalid-vessel-id";

        // Act
        var vessel = await _service.GetVesselByIdAsync(vesselId);

        // Assert
        Assert.Null(vessel);
    }

    [Fact]
    public async Task GetVesselEnginesAsync_WithValidVesselId_ReturnsEngines()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var engines = await _service.GetVesselEnginesAsync(vesselId);

        // Assert
        Assert.NotNull(engines);
        Assert.NotEmpty(engines);
    }

    [Fact]
    public async Task GetEngineByIdAsync_WithValidIds_ReturnsEngine()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";

        // Act
        var engine = await _service.GetEngineByIdAsync(vesselId, engineId);

        // Assert
        Assert.NotNull(engine);
        Assert.Equal(engineId, engine.Id);
        Assert.NotEmpty(engine.Name);
    }

    [Fact]
    public async Task StartEngineAsync_WithStoppedEngine_ReturnsTrue()
    {
        // Arrange
        var vesselId = "vessel-002";
        var engineId = "engine-003";

        // Act
        var result = await _service.StartEngineAsync(vesselId, engineId);

        // Assert
        Assert.True(result);

        // Wait a bit for async operation
        await Task.Delay(2500);

        var engine = await _service.GetEngineByIdAsync(vesselId, engineId);
        Assert.NotNull(engine);
        Assert.Equal(EngineStatus.Running, engine.Status);
        Assert.True(engine.Rpm > 0);
    }

    [Fact]
    public async Task StartEngineAsync_WithRunningEngine_ReturnsFalse()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";

        // Act
        var result = await _service.StartEngineAsync(vesselId, engineId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task StopEngineAsync_WithRunningEngine_ReturnsTrue()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";

        // Act
        var result = await _service.StopEngineAsync(vesselId, engineId);

        // Assert
        Assert.True(result);

        // Wait a bit for async operation
        await Task.Delay(2000);

        var engine = await _service.GetEngineByIdAsync(vesselId, engineId);
        Assert.NotNull(engine);
        Assert.Equal(EngineStatus.Stopped, engine.Status);
        Assert.Equal(0, engine.Rpm);
    }

    [Fact]
    public async Task StopEngineAsync_WithStoppedEngine_ReturnsFalse()
    {
        // Arrange
        var vesselId = "vessel-002";
        var engineId = "engine-003";

        // Act
        var result = await _service.StopEngineAsync(vesselId, engineId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetEngineRpmAsync_WithValidRpm_ReturnsTrue()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";
        var targetRpm = 800;

        // Act
        var result = await _service.SetEngineRpmAsync(vesselId, engineId, targetRpm);

        // Assert
        Assert.True(result);

        var engine = await _service.GetEngineByIdAsync(vesselId, engineId);
        Assert.NotNull(engine);
        Assert.Equal(targetRpm, engine.Rpm);
    }

    [Fact]
    public async Task SetEngineRpmAsync_WithInvalidRpm_ReturnsFalse()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";
        var invalidRpm = 2000; // Exceeds max RPM

        // Act
        var result = await _service.SetEngineRpmAsync(vesselId, engineId, invalidRpm);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetEngineRpmAsync_WithStoppedEngine_ReturnsFalse()
    {
        // Arrange
        var vesselId = "vessel-002";
        var engineId = "engine-003";
        var targetRpm = 500;

        // Act
        var result = await _service.SetEngineRpmAsync(vesselId, engineId, targetRpm);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetVesselSensorsAsync_WithValidVesselId_ReturnsSensors()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var sensors = await _service.GetVesselSensorsAsync(vesselId);

        // Assert
        Assert.NotNull(sensors);
        Assert.NotEmpty(sensors);
        
        var sensorList = sensors.ToList();
        Assert.True(sensorList.Count >= 3); // At least temperature, pressure, and fuel sensors
    }

    [Fact]
    public async Task GetVesselSensorsAsync_WithInvalidVesselId_ReturnsEmpty()
    {
        // Arrange
        var vesselId = "invalid-vessel-id";

        // Act
        var sensors = await _service.GetVesselSensorsAsync(vesselId);

        // Assert
        Assert.NotNull(sensors);
        Assert.Empty(sensors);
    }
}

