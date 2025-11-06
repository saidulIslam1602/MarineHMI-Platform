using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Context;
using System.Collections.Concurrent;

namespace KChief.Platform.API.Services.Configuration;

/// <summary>
/// Service for managing configuration hot reload and change notifications.
/// </summary>
public class ConfigurationHotReloadService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHotReloadService> _logger;
    private readonly ConcurrentDictionary<string, List<Action<IConfigurationSection>>> _changeHandlers = new();
    private IChangeToken? _changeToken;
    private IDisposable? _changeTokenRegistration;

    public ConfigurationHotReloadService(
        IConfiguration configuration,
        ILogger<ConfigurationHotReloadService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeChangeTracking();
    }

    /// <summary>
    /// Registers a handler for configuration changes.
    /// </summary>
    public void RegisterChangeHandler(string sectionKey, Action<IConfigurationSection> handler)
    {
        using (LogContext.PushProperty("SectionKey", sectionKey))
        {
            _changeHandlers.AddOrUpdate(
                sectionKey,
                new List<Action<IConfigurationSection>> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });

            Log.Debug("Registered change handler for configuration section {SectionKey}", sectionKey);
        }
    }

    /// <summary>
    /// Unregisters a change handler.
    /// </summary>
    public void UnregisterChangeHandler(string sectionKey, Action<IConfigurationSection> handler)
    {
        if (_changeHandlers.TryGetValue(sectionKey, out var handlers))
        {
            handlers.Remove(handler);
            Log.Debug("Unregistered change handler for configuration section {SectionKey}", sectionKey);
        }
    }

    /// <summary>
    /// Triggers a manual reload of configuration.
    /// </summary>
    public void Reload()
    {
        using (LogContext.PushProperty("Operation", "Reload"))
        {
            Log.Information("Manual configuration reload triggered");

            if (_configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
                NotifyChangeHandlers();
                Log.Information("Configuration reloaded successfully");
            }
            else
            {
                Log.Warning("Configuration does not support reload");
            }
        }
    }

    /// <summary>
    /// Initializes change tracking.
    /// </summary>
    private void InitializeChangeTracking()
    {
        if (_configuration is IConfigurationRoot configurationRoot)
        {
            _changeToken = configurationRoot.GetReloadToken();
            _changeTokenRegistration = ChangeToken.OnChange(
                () => configurationRoot.GetReloadToken(),
                () =>
                {
                    Log.Information("Configuration change detected, notifying handlers");
                    NotifyChangeHandlers();
                });

            Log.Information("Configuration hot reload initialized");
        }
        else
        {
            Log.Warning("Configuration does not support change tracking");
        }
    }

    /// <summary>
    /// Notifies all registered change handlers.
    /// </summary>
    private void NotifyChangeHandlers()
    {
        foreach (var (sectionKey, handlers) in _changeHandlers)
        {
            try
            {
                var section = _configuration.GetSection(sectionKey);
                foreach (var handler in handlers)
                {
                    handler(section);
                }

                Log.Debug("Notified {HandlerCount} handlers for section {SectionKey}", handlers.Count, sectionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error notifying change handlers for section {SectionKey}", sectionKey);
            }
        }
    }

    public void Dispose()
    {
        _changeTokenRegistration?.Dispose();
        _changeHandlers.Clear();
    }
}

