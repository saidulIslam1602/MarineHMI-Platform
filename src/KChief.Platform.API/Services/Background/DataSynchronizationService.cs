using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using KChief.Platform.Core.Services;
using Microsoft.Extensions.Options;

namespace KChief.Platform.API.Services.Background;

/// <summary>
/// Background service for synchronizing data between systems.
/// </summary>
public class DataSynchronizationService : BackgroundServiceBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DataSynchronizationOptions _options;

    public DataSynchronizationService(
        ILogger<DataSynchronizationService> logger,
        IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<DataSynchronizationOptions> options)
        : base(logger, serviceProvider)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Starting data synchronization cycle");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            
            if (_options.SyncVessels)
            {
                await SynchronizeVesselsAsync(scope.ServiceProvider, cancellationToken);
            }

            if (_options.SyncAlarms)
            {
                await SynchronizeAlarmsAsync(scope.ServiceProvider, cancellationToken);
            }

            if (_options.SyncEngineStatus)
            {
                await SynchronizeEngineStatusAsync(scope.ServiceProvider, cancellationToken);
            }

            Logger.LogDebug("Data synchronization cycle completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during data synchronization cycle");
            throw;
        }
    }

    private async Task SynchronizeVesselsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            var vesselControlService = serviceProvider.GetRequiredService<IVesselControlService>();
            var vessels = await vesselControlService.GetAllVesselsAsync();
            Logger.LogDebug("Synchronized {Count} vessels", vessels.Count());
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error synchronizing vessels");
        }
    }

    private async Task SynchronizeAlarmsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            var alarmService = serviceProvider.GetRequiredService<IAlarmService>();
            var alarms = await alarmService.GetAllAlarmsAsync();
            var activeAlarms = alarms.Where(a => a.Status == AlarmStatus.Active);
            Logger.LogDebug("Synchronized {Count} active alarms", activeAlarms.Count());
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error synchronizing alarms");
        }
    }

    private async Task SynchronizeEngineStatusAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            var vesselControlService = serviceProvider.GetRequiredService<IVesselControlService>();
            var vessels = await vesselControlService.GetAllVesselsAsync();
            int engineCount = 0;

            foreach (var vessel in vessels)
            {
                var engines = await vesselControlService.GetVesselEnginesAsync(vessel.Id);
                engineCount += engines.Count();
            }

            Logger.LogDebug("Synchronized status for {Count} engines", engineCount);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error synchronizing engine status");
        }
    }

    protected override TimeSpan GetDelayInterval()
    {
        return _options.SyncInterval;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Data synchronization service started with interval {Interval}", _options.SyncInterval);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for data synchronization service.
/// </summary>
public class DataSynchronizationOptions
{
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(10);
    public bool SyncVessels { get; set; } = true;
    public bool SyncAlarms { get; set; } = true;
    public bool SyncEngineStatus { get; set; } = true;
}

