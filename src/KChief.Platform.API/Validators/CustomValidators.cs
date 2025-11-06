using FluentValidation;
using FluentValidation.Validators;
using System.Text.RegularExpressions;

namespace KChief.Platform.API.Validators;

/// <summary>
/// Custom validator for maritime vessel identifiers.
/// </summary>
public class VesselIdValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "VesselIdValidator";

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Vessel ID must be in the format 'vessel-XXX' where XXX is a 3-digit number";
    }

    protected override bool IsValid(PropertyValidatorContext context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Format: vessel-001, vessel-002, etc.
        var pattern = @"^vessel-\d{3}$";
        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// Custom validator for engine identifiers.
/// </summary>
public class EngineIdValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "EngineIdValidator";

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Engine ID must be in the format 'engine-XXX' where XXX is a 3-digit number";
    }

    protected override bool IsValid(PropertyValidatorContext context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Format: engine-001, engine-002, etc.
        var pattern = @"^engine-\d{3}$";
        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// Custom validator for maritime email addresses (company domain validation).
/// </summary>
public class MaritimeEmailValidator<T> : PropertyValidator<T, string>
{
    private readonly string[] _allowedDomains;

    public MaritimeEmailValidator(params string[] allowedDomains)
    {
        _allowedDomains = allowedDomains ?? new[] { "kchief.com", "maritime.com" };
    }

    public override string Name => "MaritimeEmailValidator";

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return $"Email must be from an allowed maritime domain: {string.Join(", ", _allowedDomains)}";
    }

    protected override bool IsValid(PropertyValidatorContext context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Basic email format check
        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // Check domain
        var domain = value.Split('@').LastOrDefault();
        return _allowedDomains.Any(d => domain?.EndsWith(d, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>
/// Custom validator for maritime coordinates (latitude/longitude).
/// </summary>
public class MaritimeCoordinateValidator<T> : PropertyValidator<T, double>
{
    private readonly bool _isLatitude;

    public MaritimeCoordinateValidator(bool isLatitude)
    {
        _isLatitude = isLatitude;
    }

    public override string Name => "MaritimeCoordinateValidator";

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return _isLatitude
            ? "Latitude must be between -90 and 90 degrees"
            : "Longitude must be between -180 and 180 degrees";
    }

    protected override bool IsValid(PropertyValidatorContext context, double value)
    {
        return _isLatitude
            ? value >= -90 && value <= 90
            : value >= -180 && value <= 180;
    }
}

/// <summary>
/// Custom validator for maritime vessel names.
/// </summary>
public class VesselNameValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "VesselNameValidator";

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Vessel name must start with a maritime prefix (MS, MV, SS, etc.) and be between 3 and 100 characters";
    }

    protected override bool IsValid(PropertyValidatorContext context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length < 3 || value.Length > 100)
        {
            return false;
        }

        // Maritime vessel name prefixes
        var prefixes = new[] { "MS", "MV", "SS", "M/V", "S/S", "MT", "RV", "USNS", "USS" };
        return prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for FluentValidation to use custom validators.
/// </summary>
public static class CustomValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> VesselId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new VesselIdValidator<T>());
    }

    public static IRuleBuilderOptions<T, string> EngineId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new EngineIdValidator<T>());
    }

    public static IRuleBuilderOptions<T, string> MaritimeEmail<T>(this IRuleBuilder<T, string> ruleBuilder, params string[] allowedDomains)
    {
        return ruleBuilder.SetValidator(new MaritimeEmailValidator<T>(allowedDomains));
    }

    public static IRuleBuilderOptions<T, double> MaritimeLatitude<T>(this IRuleBuilder<T, double> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new MaritimeCoordinateValidator<T>(isLatitude: true));
    }

    public static IRuleBuilderOptions<T, double> MaritimeLongitude<T>(this IRuleBuilder<T, double> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new MaritimeCoordinateValidator<T>(isLatitude: false));
    }

    public static IRuleBuilderOptions<T, string> VesselName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new VesselNameValidator<T>());
    }
}

