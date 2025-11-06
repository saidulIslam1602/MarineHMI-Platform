using KChief.Platform.API.Hubs;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace KChief.Platform.API.Services;

/// <summary>
/// Service for broadcasting real-time updates via SignalR.
/// </summary>
public class RealtimeUpdateService
{
    private readonly IHubContext<VesselHub> _hubContext;
    private readonly IAlarmService _alarmService;

    public RealtimeUpdateService(IHubContext<VesselHub> hubContext, IAlarmService alarmService)
    {
        _hubContext = hubContext;
        _alarmService = alarmService;

        // Subscribe to alarm events
        _alarmService.AlarmCreated += OnAlarmCreated;
        _alarmService.AlarmAcknowledged += OnAlarmAcknowledged;
        _alarmService.AlarmCleared += OnAlarmCleared;
    }

    public async Task BroadcastVesselUpdateAsync(Vessel vessel)
    {
        await _hubContext.Clients.All.SendAsync("VesselUpdated", vessel);
    }

    public async Task BroadcastEngineUpdateAsync(string vesselId, Engine engine)
    {
        await _hubContext.Clients.All.SendAsync("EngineUpdated", vesselId, engine);
    }

    public async Task BroadcastSensorUpdateAsync(string vesselId, Sensor sensor)
    {
        await _hubContext.Clients.All.SendAsync("SensorUpdated", vesselId, sensor);
    }

    private async void OnAlarmCreated(object? sender, Alarm alarm)
    {
        await _hubContext.Clients.All.SendAsync("AlarmCreated", alarm);
    }

    private async void OnAlarmAcknowledged(object? sender, Alarm alarm)
    {
        await _hubContext.Clients.All.SendAsync("AlarmAcknowledged", alarm);
    }

    private async void OnAlarmCleared(object? sender, Alarm alarm)
    {
        await _hubContext.Clients.All.SendAsync("AlarmCleared", alarm);
    }
}

