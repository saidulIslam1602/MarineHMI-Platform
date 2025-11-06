using KChief.Platform.Core.Models;

namespace KChief.Platform.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test vessel instances.
/// </summary>
public class VesselBuilder
{
    private string _id = "vessel-001";
    private string _name = "Test Vessel";
    private string _type = "Container Ship";
    private VesselStatus _status = VesselStatus.Online;
    private string _location = "Port of Test";
    private double _length = 100.0;
    private double _width = 20.0;
    private double _maxSpeed = 25.0;
    private List<Engine> _engines = new();

    public VesselBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public VesselBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public VesselBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public VesselBuilder WithLocation(string location)
    {
        _location = location;
        return this;
    }

    public VesselBuilder WithDimensions(double length, double width)
    {
        _length = length;
        _width = width;
        return this;
    }

    public VesselBuilder WithMaxSpeed(double maxSpeed)
    {
        _maxSpeed = maxSpeed;
        return this;
    }

    public VesselBuilder WithEngines(List<Engine> engines)
    {
        _engines = engines;
        return this;
    }

    public VesselBuilder AddEngine(Engine engine)
    {
        _engines.Add(engine);
        return this;
    }

    public VesselBuilder WithStatus(VesselStatus status)
    {
        _status = status;
        return this;
    }

    public VesselBuilder AsCargoVessel()
    {
        _type = "Cargo Ship";
        return this;
    }

    public VesselBuilder AsTanker()
    {
        _type = "Tanker";
        return this;
    }

    public VesselBuilder AsContainerShip()
    {
        _type = "Container Ship";
        return this;
    }

    public VesselBuilder AsCruiseShip()
    {
        _type = "Cruise Ship";
        return this;
    }

    public VesselBuilder Online()
    {
        _status = VesselStatus.Online;
        return this;
    }

    public VesselBuilder Offline()
    {
        _status = VesselStatus.Offline;
        return this;
    }

    public VesselBuilder InMaintenance()
    {
        _status = VesselStatus.Maintenance;
        return this;
    }

    public Vessel Build()
    {
        return new Vessel
        {
            Id = _id,
            Name = _name,
            Type = _type,
            Status = _status,
            Location = _location,
            Length = _length,
            Width = _width,
            MaxSpeed = _maxSpeed,
            Engines = _engines,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    public static VesselBuilder Create() => new VesselBuilder();

    public static VesselBuilder CreateDefault() => new VesselBuilder()
        .WithId("vessel-001")
        .WithName("Test Vessel")
        .AsContainerShip()
        .Online();
}

