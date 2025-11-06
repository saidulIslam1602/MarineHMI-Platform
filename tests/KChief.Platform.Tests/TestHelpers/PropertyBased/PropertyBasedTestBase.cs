using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace KChief.Platform.Tests.TestHelpers.PropertyBased;

/// <summary>
/// Base class for property-based tests.
/// </summary>
public abstract class PropertyBasedTestBase
{
    protected static Gen<string> ValidVesselId => 
        Gen.Choose<int>(1, 999).Select(n => $"vessel-{n:D3}");

    protected static Gen<string> ValidEngineId => 
        Gen.Choose<int>(1, 999).Select(n => $"engine-{n:D3}");

    protected static Gen<string> ValidSensorId => 
        Gen.Choose<int>(1, 999).Select(n => $"sensor-{n:D3}");

    protected static Gen<string> ValidImoNumber => 
        Gen.Choose<int>(1000000, 9999999).Select(n => $"IMO{n}");

    protected static Gen<string> ValidCallSign => 
        Gen.Choose<int>(4, 8)
            .SelectMany(length => 
                Gen.ArrayOf<char>(length, Gen.Choose<char>('A', 'Z'))
                    .Select(chars => new string(chars)));

    protected static Gen<double> PositiveDouble => 
        Gen.Choose<int>(1, 10000).Select(n => (double)n);

    protected static Gen<double> ValidRpm => 
        Gen.Choose<int>(0, 2000).Select(n => (double)n);

    protected static Gen<double> ValidTemperature => 
        Gen.Choose<int>(-50, 200).Select(n => (double)n);

    protected static Gen<DateTime> ValidDate => 
        Gen.Choose<int>(1900, DateTime.UtcNow.Year)
            .SelectMany(year => 
                Gen.Choose<int>(1, 12).SelectMany(month =>
                    Gen.Choose<int>(1, DateTime.DaysInMonth(year, month))
                        .Select(day => new DateTime(year, month, day))));

    protected static Gen<string> VesselTypeGen => 
        Gen.Elements("Cargo Ship", "Tanker", "Container Ship", "Cruise Ship", "Bulk Carrier");

    protected static Gen<string> EngineTypeGen => 
        Gen.Elements("Diesel", "Gas Turbine", "Electric", "Steam");

    protected static Gen<string> AlarmSeverityGen => 
        Gen.Elements("Info", "Warning", "Critical", "Emergency");
}

