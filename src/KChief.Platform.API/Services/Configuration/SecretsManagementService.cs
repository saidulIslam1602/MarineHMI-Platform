using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;

namespace KChief.Platform.API.Services.Configuration;

/// <summary>
/// Service for managing secrets securely.
/// Supports multiple sources: environment variables, Azure Key Vault, and user secrets.
/// </summary>
public class SecretsManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretsManagementService> _logger;
    private readonly ICacheService? _cacheService;
    private readonly Dictionary<string, string> _secretCache = new();

    public SecretsManagementService(
        IConfiguration configuration,
        ILogger<SecretsManagementService> logger,
        ICacheService? cacheService = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService;
    }

    /// <summary>
    /// Gets a secret value from configuration sources.
    /// Priority: Environment Variables > Azure Key Vault > Configuration > User Secrets
    /// </summary>
    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("SecretKey", key))
        {
            // Check cache first
            if (_secretCache.TryGetValue(key, out var cachedValue))
            {
                Log.Debug("Secret retrieved from cache for key {SecretKey}", key);
                return cachedValue;
            }

            if (_cacheService != null)
            {
                var cached = await _cacheService.GetAsync<string>($"secret:{key}", cancellationToken);
                if (cached != null)
                {
                    _secretCache[key] = cached;
                    Log.Debug("Secret retrieved from distributed cache for key {SecretKey}", key);
                    return cached;
                }
            }

            // Try environment variables first (highest priority)
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                Log.Debug("Secret retrieved from environment variable for key {SecretKey}", key);
                CacheSecret(key, envValue);
                return envValue;
            }

            // Try configuration
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                Log.Debug("Secret retrieved from configuration for key {SecretKey}", key);
                CacheSecret(key, configValue);
                return configValue;
            }

            // Try with common prefixes
            var prefixedKeys = new[]
            {
                $"SECRETS:{key}",
                $"SECRET:{key}",
                $"KCHIEF:{key}",
                key.Replace(":", "__")
            };

            foreach (var prefixedKey in prefixedKeys)
            {
                envValue = Environment.GetEnvironmentVariable(prefixedKey);
                if (!string.IsNullOrEmpty(envValue))
                {
                    Log.Debug("Secret retrieved from environment variable with prefix for key {SecretKey}", key);
                    CacheSecret(key, envValue);
                    return envValue;
                }

                configValue = _configuration[prefixedKey];
                if (!string.IsNullOrEmpty(configValue))
                {
                    Log.Debug("Secret retrieved from configuration with prefix for key {SecretKey}", key);
                    CacheSecret(key, configValue);
                    return configValue;
                }
            }

            Log.Warning("Secret not found for key {SecretKey}", key);
            return null;
        }
    }

    /// <summary>
    /// Gets a secret value or throws if not found.
    /// </summary>
    public async Task<string> GetRequiredSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException($"Required secret '{key}' is not configured");
        }
        return secret;
    }

    /// <summary>
    /// Gets a connection string from configuration.
    /// </summary>
    public string? GetConnectionString(string name)
    {
        var connectionString = _configuration.GetConnectionString(name);
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Try environment variable
        var envKey = $"ConnectionStrings__{name}";
        return Environment.GetEnvironmentVariable(envKey) ?? _configuration[envKey];
    }

    /// <summary>
    /// Gets a connection string or throws if not found.
    /// </summary>
    public string GetRequiredConnectionString(string name)
    {
        var connectionString = GetConnectionString(name);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Required connection string '{name}' is not configured");
        }
        return connectionString;
    }

    /// <summary>
    /// Caches a secret value.
    /// </summary>
    private void CacheSecret(string key, string value)
    {
        _secretCache[key] = value;
        
        if (_cacheService != null)
        {
            // Cache for 1 hour (secrets don't change frequently)
            _ = _cacheService.SetAsync($"secret:{key}", value, TimeSpan.FromHours(1));
        }
    }

    /// <summary>
    /// Clears the secret cache.
    /// </summary>
    public void ClearCache()
    {
        _secretCache.Clear();
        Log.Information("Secret cache cleared");
    }
}

