using KChief.Platform.Core.Models;

namespace KChief.Platform.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test alarm instances.
/// </summary>
public class AlarmBuilder
{
    private string _id = "alarm-001";
    private string _title = "Test Alarm";
    private string _description = "Test alarm description";
    private AlarmSeverity _severity = AlarmSeverity.Warning;
    private AlarmStatus _status = AlarmStatus.Active;
    private string? _vesselId = "vessel-001";
    private string? _engineId = null;
    private string? _sensorId = null;
    private DateTime _triggeredAt = DateTime.UtcNow;
    private DateTime? _acknowledgedAt = null;
    private string? _acknowledgedBy = null;
    private DateTime? _clearedAt = null;
    private string? _clearedBy = null;

    public AlarmBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public AlarmBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public AlarmBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AlarmBuilder WithSeverity(AlarmSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public AlarmBuilder WithStatus(AlarmStatus status)
    {
        _status = status;
        return this;
    }

    public AlarmBuilder WithVesselId(string vesselId)
    {
        _vesselId = vesselId;
        return this;
    }

    public AlarmBuilder WithEngineId(string engineId)
    {
        _engineId = engineId;
        return this;
    }

    public AlarmBuilder WithSensorId(string sensorId)
    {
        _sensorId = sensorId;
        return this;
    }

    public AlarmBuilder WithTriggeredAt(DateTime triggeredAt)
    {
        _triggeredAt = triggeredAt;
        return this;
    }

    public AlarmBuilder Acknowledged(string acknowledgedBy, DateTime? acknowledgedAt = null)
    {
        _status = AlarmStatus.Acknowledged;
        _acknowledgedBy = acknowledgedBy;
        _acknowledgedAt = acknowledgedAt ?? DateTime.UtcNow;
        return this;
    }

    public AlarmBuilder Cleared(string clearedBy, DateTime? clearedAt = null)
    {
        _status = AlarmStatus.Cleared;
        _clearedBy = clearedBy;
        _clearedAt = clearedAt ?? DateTime.UtcNow;
        return this;
    }

    public AlarmBuilder AsWarning()
    {
        _severity = AlarmSeverity.Warning;
        return this;
    }

    public AlarmBuilder AsCritical()
    {
        _severity = AlarmSeverity.Critical;
        return this;
    }

    public AlarmBuilder AsInfo()
    {
        _severity = AlarmSeverity.Info;
        return this;
    }

    public AlarmBuilder Active()
    {
        _status = AlarmStatus.Active;
        return this;
    }

    public Alarm Build()
    {
        return new Alarm
        {
            Id = _id,
            Title = _title,
            Description = _description,
            Severity = _severity,
            Status = _status,
            VesselId = _vesselId,
            EngineId = _engineId,
            SensorId = _sensorId,
            TriggeredAt = _triggeredAt,
            AcknowledgedAt = _acknowledgedAt,
            AcknowledgedBy = _acknowledgedBy,
            ClearedAt = _clearedAt,
            ClearedBy = _clearedBy
        };
    }

    public static AlarmBuilder Create() => new AlarmBuilder();

    public static AlarmBuilder CreateDefault() => new AlarmBuilder()
        .WithId("alarm-001")
        .WithTitle("Test Alarm")
        .WithDescription("Test alarm description")
        .AsWarning()
        .Active();
}

