using KChief.Platform.Core.Models;

namespace KChief.Platform.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test engine instances.
/// </summary>
public class EngineBuilder
{
    private string _id = "engine-001";
    private string _vesselId = "vessel-001";
    private string _name = "Main Engine";
    private string _type = "Diesel";
    private int _maxRpm = 1000;
    private int _rpm = 0;
    private EngineStatus _status = EngineStatus.Stopped;
    private double _temperature = 20.0;
    private double _oilPressure = 1.0;
    private double _fuelConsumption = 0.0;
    private bool _isRunning = false;

    public EngineBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public EngineBuilder WithVesselId(string vesselId)
    {
        _vesselId = vesselId;
        return this;
    }

    public EngineBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public EngineBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public EngineBuilder WithMaxRpm(int maxRpm)
    {
        _maxRpm = maxRpm;
        return this;
    }

    public EngineBuilder WithRpm(int rpm)
    {
        _rpm = rpm;
        return this;
    }

    public EngineBuilder WithStatus(EngineStatus status)
    {
        _status = status;
        return this;
    }

    public EngineBuilder WithTemperature(double temperature)
    {
        _temperature = temperature;
        return this;
    }

    public EngineBuilder WithOilPressure(double oilPressure)
    {
        _oilPressure = oilPressure;
        return this;
    }

    public EngineBuilder WithFuelConsumption(double fuelConsumption)
    {
        _fuelConsumption = fuelConsumption;
        return this;
    }

    public EngineBuilder WithIsRunning(bool isRunning)
    {
        _isRunning = isRunning;
        return this;
    }

    public EngineBuilder AsDiesel()
    {
        _type = "Diesel";
        return this;
    }

    public EngineBuilder AsGasTurbine()
    {
        _type = "Gas Turbine";
        return this;
    }

    public EngineBuilder AsElectric()
    {
        _type = "Electric";
        return this;
    }

    public EngineBuilder Running()
    {
        _status = EngineStatus.Running;
        _isRunning = true;
        _rpm = (int)(_maxRpm * 0.8);
        return this;
    }

    public EngineBuilder Stopped()
    {
        _status = EngineStatus.Stopped;
        _isRunning = false;
        _rpm = 0;
        return this;
    }

    public EngineBuilder Overheated()
    {
        _status = EngineStatus.Overheated;
        _temperature = 120.0;
        return this;
    }

    public Engine Build()
    {
        return new Engine
        {
            Id = _id,
            VesselId = _vesselId,
            Name = _name,
            Type = _type,
            MaxRPM = _maxRpm,
            RPM = _rpm,
            Status = _status,
            Temperature = _temperature,
            OilPressure = _oilPressure,
            FuelConsumption = _fuelConsumption,
            IsRunning = _isRunning,
            LastUpdated = DateTime.UtcNow
        };
    }

    public static EngineBuilder Create() => new EngineBuilder();

    public static EngineBuilder CreateDefault() => new EngineBuilder()
        .WithId("engine-001")
        .WithVesselId("vessel-001")
        .WithName("Main Engine")
        .AsDiesel()
        .Stopped();
}

