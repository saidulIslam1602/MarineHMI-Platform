using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;

namespace KChief.VesselControl.Services;

/// <summary>
/// Service implementation for vessel control operations.
/// This is a simulation service for MVP purposes.
/// </summary>
public class VesselControlService : IVesselControlService
{
    private readonly Dictionary<string, Vessel> _vessels;
    private readonly Random _random = new();

    public VesselControlService()
    {
        _vessels = InitializeVessels();
    }

    public Task<IEnumerable<Vessel>> GetAllVesselsAsync()
    {
        UpdateVesselData();
        return Task.FromResult<IEnumerable<Vessel>>(_vessels.Values);
    }

    public Task<Vessel?> GetVesselByIdAsync(string vesselId)
    {
        UpdateVesselData();
        _vessels.TryGetValue(vesselId, out var vessel);
        return Task.FromResult(vessel);
    }

    public Task<IEnumerable<Engine>> GetVesselEnginesAsync(string vesselId)
    {
        UpdateVesselData();
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            return Task.FromResult<IEnumerable<Engine>>(vessel.Engines);
        }
        return Task.FromResult<IEnumerable<Engine>>(Array.Empty<Engine>());
    }

    public Task<Engine?> GetEngineByIdAsync(string vesselId, string engineId)
    {
        UpdateVesselData();
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            return Task.FromResult(vessel.Engines.FirstOrDefault(e => e.Id == engineId));
        }
        return Task.FromResult<Engine?>(null);
    }

    public Task<bool> StartEngineAsync(string vesselId, string engineId)
    {
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            var engine = vessel.Engines.FirstOrDefault(e => e.Id == engineId);
            if (engine != null && engine.Status == EngineStatus.Stopped)
            {
                engine.Status = EngineStatus.Starting;
                engine.LastUpdated = DateTime.UtcNow;

                // Simulate engine starting process
                Task.Delay(2000).ContinueWith(_ =>
                {
                    engine.Status = EngineStatus.Running;
                    engine.Rpm = 500;
                    engine.Temperature = 60 + _random.NextDouble() * 20;
                    engine.OilPressure = 3.0 + _random.NextDouble() * 1.0;
                    engine.FuelConsumption = 50 + _random.NextDouble() * 20;
                    engine.LastUpdated = DateTime.UtcNow;
                });

                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<bool> StopEngineAsync(string vesselId, string engineId)
    {
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            var engine = vessel.Engines.FirstOrDefault(e => e.Id == engineId);
            if (engine != null && engine.Status == EngineStatus.Running)
            {
                engine.Status = EngineStatus.Stopping;
                engine.LastUpdated = DateTime.UtcNow;

                // Simulate engine stopping process
                Task.Delay(1500).ContinueWith(_ =>
                {
                    engine.Status = EngineStatus.Stopped;
                    engine.Rpm = 0;
                    engine.Temperature = 25 + _random.NextDouble() * 10;
                    engine.OilPressure = 0;
                    engine.FuelConsumption = 0;
                    engine.LastUpdated = DateTime.UtcNow;
                });

                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<bool> SetEngineRpmAsync(string vesselId, string engineId, int rpm)
    {
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            var engine = vessel.Engines.FirstOrDefault(e => e.Id == engineId);
            if (engine != null && engine.Status == EngineStatus.Running)
            {
                if (rpm < 0 || rpm > engine.MaxRpm)
                {
                    return Task.FromResult(false);
                }

                engine.Rpm = rpm;
                engine.Temperature = 60 + (rpm / engine.MaxRpm) * 40 + _random.NextDouble() * 10;
                engine.FuelConsumption = 30 + (rpm / engine.MaxRpm) * 70 + _random.NextDouble() * 10;
                engine.LastUpdated = DateTime.UtcNow;

                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<IEnumerable<Sensor>> GetVesselSensorsAsync(string vesselId)
    {
        if (_vessels.TryGetValue(vesselId, out var vessel))
        {
            var sensors = new List<Sensor>
            {
                new Sensor
                {
                    Id = $"{vesselId}-temp-1",
                    Name = "Main Engine Temperature",
                    Type = "Temperature",
                    Unit = "Celsius",
                    Value = vessel.Engines.FirstOrDefault()?.Temperature ?? 25.0,
                    MinValue = 0,
                    MaxValue = 120,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                },
                new Sensor
                {
                    Id = $"{vesselId}-pressure-1",
                    Name = "Oil Pressure",
                    Type = "Pressure",
                    Unit = "Bar",
                    Value = vessel.Engines.FirstOrDefault()?.OilPressure ?? 0.0,
                    MinValue = 0,
                    MaxValue = 5.0,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                },
                new Sensor
                {
                    Id = $"{vesselId}-fuel-1",
                    Name = "Fuel Flow Rate",
                    Type = "Flow",
                    Unit = "L/h",
                    Value = vessel.Engines.FirstOrDefault()?.FuelConsumption ?? 0.0,
                    MinValue = 0,
                    MaxValue = 150,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                }
            };

            return Task.FromResult<IEnumerable<Sensor>>(sensors);
        }
        return Task.FromResult<IEnumerable<Sensor>>(Array.Empty<Sensor>());
    }

    private Dictionary<string, Vessel> InitializeVessels()
    {
        var vessels = new Dictionary<string, Vessel>
        {
            {
                "vessel-001",
                new Vessel
                {
                    Id = "vessel-001",
                    Name = "MS Atlantic Explorer",
                    Type = "Container Ship",
                    Status = VesselStatus.Online,
                    Engines = new List<Engine>
                    {
                        new Engine
                        {
                            Id = "engine-001",
                            Name = "Main Engine 1",
                            Type = "Diesel",
                            Status = EngineStatus.Running,
                            Rpm = 750,
                            MaxRpm = 1000,
                            Temperature = 85.5,
                            OilPressure = 3.5,
                            FuelConsumption = 75.2,
                            LastUpdated = DateTime.UtcNow
                        },
                        new Engine
                        {
                            Id = "engine-002",
                            Name = "Auxiliary Engine 1",
                            Type = "Diesel",
                            Status = EngineStatus.Running,
                            Rpm = 600,
                            MaxRpm = 800,
                            Temperature = 70.0,
                            OilPressure = 3.2,
                            FuelConsumption = 45.0,
                            LastUpdated = DateTime.UtcNow
                        }
                    },
                    LastUpdated = DateTime.UtcNow
                }
            },
            {
                "vessel-002",
                new Vessel
                {
                    Id = "vessel-002",
                    Name = "MV Pacific Star",
                    Type = "Tanker",
                    Status = VesselStatus.Online,
                    Engines = new List<Engine>
                    {
                        new Engine
                        {
                            Id = "engine-003",
                            Name = "Main Engine 1",
                            Type = "Diesel",
                            Status = EngineStatus.Stopped,
                            Rpm = 0,
                            MaxRpm = 1200,
                            Temperature = 25.0,
                            OilPressure = 0,
                            FuelConsumption = 0,
                            LastUpdated = DateTime.UtcNow
                        }
                    },
                    LastUpdated = DateTime.UtcNow
                }
            }
        };

        return vessels;
    }

    private void UpdateVesselData()
    {
        foreach (var vessel in _vessels.Values)
        {
            foreach (var engine in vessel.Engines)
            {
                if (engine.Status == EngineStatus.Running)
                {
                    // Simulate slight variations in running engines
                    engine.Temperature += (_random.NextDouble() - 0.5) * 2;
                    engine.Temperature = Math.Max(60, Math.Min(110, engine.Temperature));
                    engine.OilPressure += (_random.NextDouble() - 0.5) * 0.2;
                    engine.OilPressure = Math.Max(2.5, Math.Min(4.5, engine.OilPressure));
                    engine.FuelConsumption += (_random.NextDouble() - 0.5) * 3;
                    engine.FuelConsumption = Math.Max(30, Math.Min(120, engine.FuelConsumption));
                    engine.LastUpdated = DateTime.UtcNow;
                }
            }
            vessel.LastUpdated = DateTime.UtcNow;
        }
    }
}

