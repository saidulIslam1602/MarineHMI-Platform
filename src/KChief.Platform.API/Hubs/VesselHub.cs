using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace KChief.Platform.API.Hubs;

/// <summary>
/// SignalR hub for real-time vessel and engine updates.
/// </summary>
public class VesselHub : Hub
{
    /// <summary>
    /// Sends vessel status update to all connected clients.
    /// </summary>
    public async Task SendVesselUpdate(Vessel vessel)
    {
        await Clients.All.SendAsync("VesselUpdated", vessel);
    }

    /// <summary>
    /// Sends engine status update to all connected clients.
    /// </summary>
    public async Task SendEngineUpdate(string vesselId, Engine engine)
    {
        await Clients.All.SendAsync("EngineUpdated", vesselId, engine);
    }

    /// <summary>
    /// Sends sensor data update to all connected clients.
    /// </summary>
    public async Task SendSensorUpdate(string vesselId, Sensor sensor)
    {
        await Clients.All.SendAsync("SensorUpdated", vesselId, sensor);
    }

    /// <summary>
    /// Sends alarm notification to all connected clients.
    /// </summary>
    public async Task SendAlarmNotification(Alarm alarm)
    {
        await Clients.All.SendAsync("AlarmCreated", alarm);
    }

    /// <summary>
    /// Sends alarm acknowledgment notification.
    /// </summary>
    public async Task SendAlarmAcknowledged(Alarm alarm)
    {
        await Clients.All.SendAsync("AlarmAcknowledged", alarm);
    }

    /// <summary>
    /// Sends alarm cleared notification.
    /// </summary>
    public async Task SendAlarmCleared(Alarm alarm)
    {
        await Clients.All.SendAsync("AlarmCleared", alarm);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

