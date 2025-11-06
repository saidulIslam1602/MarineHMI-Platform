using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using KChief.Platform.Core.Services;
using Microsoft.Extensions.Options;

namespace KChief.Platform.API.Services.Background;

/// <summary>
/// Background service for polling sensor data from vessels.
/// </summary>
public class DataPollingService : BackgroundServiceBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DataPollingOptions _options;

    public DataPollingService(
        ILogger<DataPollingService> logger,
        IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<DataPollingOptions> options)
        : base(logger, serviceProvider)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Starting data polling cycle");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var vesselControlService = scope.ServiceProvider.GetRequiredService<IVesselControlService>();
            
            var vessels = await vesselControlService.GetAllVesselsAsync();
            
            foreach (var vessel in vessels)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await PollVesselDataAsync(vessel, scope.ServiceProvider, cancellationToken);
            }

            Logger.LogDebug("Data polling cycle completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during data polling cycle");
            throw;
        }
    }

    private async Task PollVesselDataAsync(Vessel vessel, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Polling data for vessel {VesselId}", vessel.Id);

            var vesselControlService = serviceProvider.GetRequiredService<IVesselControlService>();
            var alarmService = serviceProvider.GetRequiredService<IAlarmService>();

            // Poll sensors
            var sensors = await vesselControlService.GetVesselSensorsAsync(vessel.Id);
            foreach (var sensor in sensors)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Evaluate sensor value against alarm rules
                if (sensor.Value != 0) // Sensor.Value is double, not nullable
                {
                    // Create alarm if sensor value exceeds thresholds
                    if (sensor.Value > 100) // Example threshold
                    {
                        await alarmService.CreateAlarmAsync(
                            $"High {sensor.Type} Reading",
                            $"Sensor {sensor.Name} reading {sensor.Value} exceeds threshold",
                            AlarmSeverity.Warning,
                            vessel.Id,
                            null,
                            sensor.Id);
                    }
                }
            }

            // Poll engines
            var engines = await vesselControlService.GetVesselEnginesAsync(vessel.Id);
            foreach (var engine in engines)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Evaluate engine status against alarm rules
                // Create alarms based on engine conditions
                if (engine.Temperature > 90) // High temperature threshold
                {
                    await alarmService.CreateAlarmAsync(
                        "High Engine Temperature",
                        $"Engine {engine.Name} temperature {engine.Temperature}°C exceeds safe limit",
                        AlarmSeverity.Critical,
                        vessel.Id,
                        engine.Id);
                }

                if (engine.OilPressure < 2.0) // Low pressure threshold
                {
                    await alarmService.CreateAlarmAsync(
                        "Low Oil Pressure",
                        $"Engine {engine.Name} oil pressure {engine.OilPressure} bar is below safe limit",
                        AlarmSeverity.Critical,
                        vessel.Id,
                        engine.Id);
                }

                if (engine.RPM > engine.MaxRPM * 0.95) // RPM near maximum
                {
                    await alarmService.CreateAlarmAsync(
                        "High Engine RPM",
                        $"Engine {engine.Name} RPM {engine.RPM} is near maximum limit",
                        AlarmSeverity.Warning,
                        vessel.Id,
                        engine.Id);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error polling data for vessel {VesselId}", vessel.Id);
        }
    }

    protected override TimeSpan GetDelayInterval()
    {
        return _options.PollingInterval;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Data polling service started with interval {Interval}", _options.PollingInterval);
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Data polling service stopped");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for data polling service.
/// </summary>
public class DataPollingOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentVessels { get; set; } = 10;
    public bool EnableSensorPolling { get; set; } = true;
    public bool EnableEnginePolling { get; set; } = true;
}

