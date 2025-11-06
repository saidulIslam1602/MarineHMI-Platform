using Bogus;
using KChief.Platform.Core.Models;

namespace KChief.Platform.Tests.TestHelpers.DataGenerators;

/// <summary>
/// Extensions for generating test data using Bogus.
/// </summary>
public static class FakerExtensions
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a random vessel.
    /// </summary>
    public static Vessel GenerateVessel(this Faker faker, string? id = null)
    {
        return new Vessel
        {
            Id = id ?? $"vessel-{faker.Random.Int(1, 999):D3}",
            Name = faker.Company.CompanyName() + " Vessel",
            Type = faker.PickRandom("Container Ship", "Tanker", "Cargo Ship", "Cruise Ship", "Bulk Carrier"),
            Status = faker.PickRandom<VesselStatus>(),
            Location = faker.Address.City() + " Port",
            Length = faker.Random.Double(50, 400),
            Width = faker.Random.Double(10, 60),
            MaxSpeed = faker.Random.Double(10, 35),
            Engines = new List<Engine>(),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a random engine.
    /// </summary>
    public static Engine GenerateEngine(this Faker faker, string? vesselId = null, string? id = null)
    {
        return new Engine
        {
            Id = id ?? $"engine-{faker.Random.Int(1, 999):D3}",
            VesselId = vesselId ?? $"vessel-{faker.Random.Int(1, 999):D3}",
            Name = faker.PickRandom("Main Engine", "Auxiliary Engine", "Generator Engine"),
            Type = faker.PickRandom("Diesel", "Gas Turbine", "Electric", "Steam"),
            MaxRPM = faker.Random.Int(500, 2000),
            RPM = faker.Random.Int(0, 1000),
            Status = faker.PickRandom<EngineStatus>(),
            Temperature = faker.Random.Double(20, 100),
            OilPressure = faker.Random.Double(0.5, 2.0),
            FuelConsumption = faker.Random.Double(10, 100),
            IsRunning = faker.Random.Bool(),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a random alarm.
    /// </summary>
    public static Alarm GenerateAlarm(this Faker faker, string? vesselId = null, string? id = null)
    {
        return new Alarm
        {
            Id = id ?? $"alarm-{faker.Random.Int(1, 999):D3}",
            Title = faker.Lorem.Sentence(),
            Description = faker.Lorem.Paragraph(),
            Severity = faker.PickRandom<AlarmSeverity>(),
            Status = faker.PickRandom<AlarmStatus>(),
            VesselId = vesselId ?? $"vessel-{faker.Random.Int(1, 999):D3}",
            EngineId = faker.Random.Bool(0.5f) ? $"engine-{faker.Random.Int(1, 999):D3}" : null,
            SensorId = faker.Random.Bool(0.3f) ? $"sensor-{faker.Random.Int(1, 999):D3}" : null,
            TriggeredAt = faker.Date.Recent(),
            AcknowledgedAt = faker.Random.Bool(0.3f) ? faker.Date.Recent() : null,
            AcknowledgedBy = faker.Random.Bool(0.3f) ? faker.Person.UserName : null,
            ClearedAt = faker.Random.Bool(0.2f) ? faker.Date.Recent() : null,
            ClearedBy = faker.Random.Bool(0.2f) ? faker.Person.UserName : null
        };
    }

    /// <summary>
    /// Generates multiple vessels.
    /// </summary>
    public static IEnumerable<Vessel> GenerateVessels(this Faker faker, int count)
    {
        return Enumerable.Range(0, count).Select(_ => faker.GenerateVessel());
    }

    /// <summary>
    /// Generates multiple engines.
    /// </summary>
    public static IEnumerable<Engine> GenerateEngines(this Faker faker, int count, string? vesselId = null)
    {
        return Enumerable.Range(0, count).Select(_ => faker.GenerateEngine(vesselId));
    }

    /// <summary>
    /// Generates multiple alarms.
    /// </summary>
    public static IEnumerable<Alarm> GenerateAlarms(this Faker faker, int count, string? vesselId = null)
    {
        return Enumerable.Range(0, count).Select(_ => faker.GenerateAlarm(vesselId));
    }
}

