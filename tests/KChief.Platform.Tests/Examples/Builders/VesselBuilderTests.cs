using KChief.Platform.Core.Models;
using KChief.Platform.Tests.TestHelpers.Builders;
using Xunit;

namespace KChief.Platform.Tests.Examples.Builders;

/// <summary>
/// Example tests demonstrating the builder pattern.
/// </summary>
public class VesselBuilderTests
{
    [Fact]
    public void VesselBuilder_Should_Create_Default_Vessel()
    {
        var vessel = VesselBuilder.CreateDefault().Build();

        Assert.NotNull(vessel);
        Assert.Equal("vessel-001", vessel.Id);
        Assert.Equal("Test Vessel", vessel.Name);
        Assert.Equal("Container Ship", vessel.Type);
        Assert.Equal(VesselStatus.Online, vessel.Status);
    }

    [Fact]
    public void VesselBuilder_Should_Create_Custom_Vessel()
    {
        var vessel = VesselBuilder.Create()
            .WithId("vessel-999")
            .WithName("Custom Vessel")
            .AsTanker()
            .Offline()
            .WithDimensions(200.0, 30.0)
            .WithMaxSpeed(20.0)
            .WithLocation("Custom Port")
            .Build();

        Assert.Equal("vessel-999", vessel.Id);
        Assert.Equal("Custom Vessel", vessel.Name);
        Assert.Equal("Tanker", vessel.Type);
        Assert.Equal(VesselStatus.Offline, vessel.Status);
        Assert.Equal(200.0, vessel.Length);
        Assert.Equal(30.0, vessel.Width);
        Assert.Equal(20.0, vessel.MaxSpeed);
        Assert.Equal("Custom Port", vessel.Location);
    }

    [Fact]
    public void EngineBuilder_Should_Create_Default_Engine()
    {
        var engine = EngineBuilder.CreateDefault().Build();

        Assert.NotNull(engine);
        Assert.Equal("engine-001", engine.Id);
        Assert.Equal("vessel-001", engine.VesselId);
        Assert.Equal("Main Engine", engine.Name);
        Assert.Equal("Diesel", engine.Type);
        Assert.Equal(EngineStatus.Stopped, engine.Status);
    }

    [Fact]
    public void EngineBuilder_Should_Create_Running_Engine()
    {
        var engine = EngineBuilder.Create()
            .WithId("engine-002")
            .WithVesselId("vessel-001")
            .WithMaxRpm(1500)
            .Running()
            .Build();

        Assert.Equal(EngineStatus.Running, engine.Status);
        Assert.True(engine.RPM > 0);
    }

    [Fact]
    public void AlarmBuilder_Should_Create_Default_Alarm()
    {
        var alarm = AlarmBuilder.CreateDefault().Build();

        Assert.NotNull(alarm);
        Assert.Equal("alarm-001", alarm.Id);
        Assert.Equal("Test Alarm", alarm.Title);
        Assert.Equal(AlarmSeverity.Warning, alarm.Severity);
        Assert.Equal(AlarmStatus.Active, alarm.Status);
    }

    [Fact]
    public void AlarmBuilder_Should_Create_Acknowledged_Alarm()
    {
        var alarm = AlarmBuilder.Create()
            .WithTitle("High Temperature")
            .AsCritical()
            .Acknowledged("operator-001")
            .Build();

        Assert.Equal(AlarmStatus.Acknowledged, alarm.Status);
        Assert.Equal("operator-001", alarm.AcknowledgedBy);
        Assert.NotNull(alarm.AcknowledgedAt);
        Assert.Equal(AlarmSeverity.Critical, alarm.Severity);
    }
}

