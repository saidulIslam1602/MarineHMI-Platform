using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Configuration;

namespace KChief.Platform.API.Services.Configuration;

/// <summary>
/// Service for validating configuration options at startup and runtime.
/// </summary>
public class ConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConfigurationValidationService(
        ILogger<ConfigurationValidationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Validates all registered options.
    /// </summary>
    public void ValidateAllOptions()
    {
        using (LogContext.PushProperty("Operation", "ValidateAllOptions"))
        {
            Log.Information("Validating all configuration options");

            var validationErrors = new List<string>();

            // Validate ApplicationOptions
            ValidateOptions<ApplicationOptions>(ref validationErrors);
            ValidateOptions<DatabaseOptions>(ref validationErrors);
            ValidateOptions<AuthenticationOptions>(ref validationErrors);
            ValidateOptions<MonitoringOptions>(ref validationErrors);
            ValidateOptions<CacheOptions>(ref validationErrors);
            ValidateOptions<ConfigurationSourceOptions>(ref validationErrors);

            if (validationErrors.Any())
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                Log.Error("Configuration validation failed:{NewLine}{Errors}", Environment.NewLine, errorMessage);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            Log.Information("All configuration options validated successfully");
        }
    }

    /// <summary>
    /// Validates a specific options type.
    /// </summary>
    private void ValidateOptions<TOptions>(ref List<string> validationErrors) where TOptions : class
    {
        try
        {
            var options = _serviceProvider.GetService<IOptions<TOptions>>();
            if (options == null)
            {
                Log.Warning("Options type {OptionsType} is not registered", typeof(TOptions).Name);
                return;
            }

            var optionsValue = options.Value;
            if (optionsValue is ValidatedOptions validatedOptions)
            {
                var result = validatedOptions.Validate();
                if (!result.IsValid)
                {
                    var errors = string.Join(", ", result.Errors);
                    validationErrors.Add($"{typeof(TOptions).Name}: {errors}");
                    Log.Warning("Validation failed for {OptionsType}: {Errors}", typeof(TOptions).Name, errors);
                }
                else
                {
                    Log.Debug("Validation passed for {OptionsType}", typeof(TOptions).Name);
                }
            }
            else
            {
                // Use DataAnnotations validation
                var context = new ValidationContext(optionsValue);
                var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var isValid = Validator.TryValidateObject(optionsValue, context, results, true);

                if (!isValid)
                {
                    var errors = string.Join(", ", results.Select(r => r.ErrorMessage ?? string.Empty));
                    validationErrors.Add($"{typeof(TOptions).Name}: {errors}");
                    Log.Warning("Validation failed for {OptionsType}: {Errors}", typeof(TOptions).Name, errors);
                }
                else
                {
                    Log.Debug("Validation passed for {OptionsType}", typeof(TOptions).Name);
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error validating {typeof(TOptions).Name}: {ex.Message}";
            validationErrors.Add(errorMessage);
            Log.Error(ex, "Error validating options type {OptionsType}", typeof(TOptions).Name);
        }
    }
}

/// <summary>
/// Startup filter to validate configuration on application startup.
/// </summary>
public class ConfigurationValidationStartupFilter : IHostedService
{
    private readonly ConfigurationValidationService _validationService;

    public ConfigurationValidationStartupFilter(ConfigurationValidationService validationService)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _validationService.ValidateAllOptions();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

