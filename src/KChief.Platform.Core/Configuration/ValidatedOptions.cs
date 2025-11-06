using System.ComponentModel.DataAnnotations;

namespace KChief.Platform.Core.Configuration;

/// <summary>
/// Base class for validated configuration options.
/// </summary>
public abstract class ValidatedOptions
{
    /// <summary>
    /// Validates the options instance.
    /// </summary>
    public virtual ValidationResult Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(this, context, results, true);
        
        return new ValidationResult
        {
            IsValid = isValid,
            Errors = results.Select(r => r.ErrorMessage ?? string.Empty).ToList()
        };
    }
}

/// <summary>
/// Validation result for options.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Application-wide configuration options with validation.
/// </summary>
public class ApplicationOptions : ValidatedOptions
{
    [Required]
    [StringLength(100)]
    public string ApplicationName { get; set; } = "K-Chief Marine Automation Platform";

    [Required]
    [StringLength(20)]
    public string Version { get; set; } = "1.0.0";

    [Required]
    public string Environment { get; set; } = "Production";

    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    [Url]
    public string? BaseUrl { get; set; }

    public bool EnableSwagger { get; set; } = false;

    public bool EnableDetailedErrors { get; set; } = false;
}

/// <summary>
/// Database configuration options with validation.
/// </summary>
public class DatabaseOptions : ValidatedOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableRetryOnFailure { get; set; } = true;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// Authentication configuration options with validation.
/// </summary>
public class AuthenticationOptions : ValidatedOptions
{
    public JwtOptions JWT { get; set; } = new();
    public ApiKeyOptions ApiKey { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}

/// <summary>
/// JWT configuration options with validation.
/// </summary>
public class JwtOptions : ValidatedOptions
{
    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Issuer { get; set; } = "KChief.Platform.API";

    [Required]
    [StringLength(200)]
    public string Audience { get; set; } = "KChief.Platform.API";

    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// API Key configuration options with validation.
/// </summary>
public class ApiKeyOptions : ValidatedOptions
{
    [Range(1, 10000)]
    public int DefaultRateLimitPerMinute { get; set; } = 1000;

    [Range(16, 256)]
    public int MaxKeyLength { get; set; } = 128;

    public bool RequireHttps { get; set; } = true;
}

/// <summary>
/// Security configuration options with validation.
/// </summary>
public class SecurityOptions : ValidatedOptions
{
    public bool RequireHttpsMetadata { get; set; } = true;

    [Range(1, 20)]
    public int MaxFailedLoginAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int AccountLockoutMinutes { get; set; } = 30;

    public PasswordRequirementsOptions PasswordRequirements { get; set; } = new();
}

/// <summary>
/// Password requirements configuration options with validation.
/// </summary>
public class PasswordRequirementsOptions : ValidatedOptions
{
    [Range(6, 128)]
    public int MinLength { get; set; } = 8;

    public bool RequireDigit { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;
}

/// <summary>
/// Monitoring configuration options with validation.
/// </summary>
public class MonitoringOptions : ValidatedOptions
{
    public bool EnablePerformanceCounters { get; set; } = true;

    [Range(1, 300)]
    public int MetricsCollectionIntervalSeconds { get; set; } = 30;

    [Range(0.1, 60.0)]
    public double SlowRequestThresholdSeconds { get; set; } = 1.0;

    public string? ApplicationInsightsConnectionString { get; set; }
}

/// <summary>
/// Configuration source options.
/// </summary>
public class ConfigurationSourceOptions : ValidatedOptions
{
    public bool UseAzureKeyVault { get; set; } = false;

    public string? AzureKeyVaultUrl { get; set; }

    public string? AzureKeyVaultClientId { get; set; }

    public string? AzureKeyVaultClientSecret { get; set; }

    public bool UseEnvironmentVariables { get; set; } = true;

    public bool EnableHotReload { get; set; } = true;

    public bool UseUserSecrets { get; set; } = false;

    public string? UserSecretsId { get; set; }
}

