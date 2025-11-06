namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Utility class for generating unique identifiers.
/// </summary>
public static class IdGenerator
{
    /// <summary>
    /// Generates a new GUID as a string.
    /// </summary>
    public static string GenerateGuid()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Generates a short GUID (12 characters).
    /// </summary>
    public static string GenerateShortGuid()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>
    /// Generates a sequential ID based on timestamp and random component.
    /// </summary>
    public static string GenerateSequentialId(string prefix = "")
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}{timestamp}{random}";
    }

    /// <summary>
    /// Generates a maritime vessel ID.
    /// </summary>
    public static string GenerateVesselId()
    {
        return $"vessel-{GenerateShortGuid()}";
    }

    /// <summary>
    /// Generates an engine ID.
    /// </summary>
    public static string GenerateEngineId()
    {
        return $"engine-{GenerateShortGuid()}";
    }

    /// <summary>
    /// Generates a sensor ID.
    /// </summary>
    public static string GenerateSensorId()
    {
        return $"sensor-{GenerateShortGuid()}";
    }

    /// <summary>
    /// Generates an alarm ID.
    /// </summary>
    public static string GenerateAlarmId()
    {
        return $"alarm-{GenerateShortGuid()}";
    }

    /// <summary>
    /// Generates a correlation ID.
    /// </summary>
    public static string GenerateCorrelationId()
    {
        return GenerateShortGuid();
    }
}

