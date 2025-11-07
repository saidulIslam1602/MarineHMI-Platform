using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using HMI.Platform.Core.Interfaces;

namespace HMI.Platform.API.Middleware;

/// <summary>
/// Middleware for API key authentication.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    
    public const string ApiKeyHeaderName = "X-API-Key";
    public const string ApiKeyQueryParameter = "apikey";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IHMIAuthenticationService authenticationService)
    {
        // Skip API key authentication if user is already authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var apiKey = ExtractApiKey(context);
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            using (LogContext.PushProperty("AuthenticationMethod", "ApiKey"))
            using (LogContext.PushProperty("ApiKeyPrefix", apiKey.Length > 8 ? apiKey[..8] + "..." : "short"))
            {
                try
                {
                    var authResult = await authenticationService.AuthenticateApiKeyAsync(apiKey);
                    
                    if (authResult.IsSuccess && authResult.ApiKey != null)
                    {
                        // Create claims for the API key
                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, authResult.ApiKey.Id),
                            new(ClaimTypes.Name, authResult.ApiKey.Name),
                            new("ApiKeyId", authResult.ApiKey.Id),
                            new("ApiKeyName", authResult.ApiKey.Name),
                            new("OwnerId", authResult.ApiKey.OwnerId),
                            new("OwnerType", authResult.ApiKey.OwnerType),
                            new("AuthenticationMethod", "ApiKey")
                        };

                        // Add scope claims
                        foreach (var scope in authResult.ApiKey.Scopes)
                        {
                            claims.Add(new Claim("Scope", scope));
                        }

                        var identity = new ClaimsIdentity(claims, "ApiKey");
                        var principal = new ClaimsPrincipal(identity);
                        
                        context.User = principal;
                        
                        Log.Information("API key authentication successful for key {ApiKeyName}", authResult.ApiKey.Name);
                    }
                    else
                    {
                        Log.Warning("API key authentication failed: {ErrorMessage}", authResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during API key authentication");
                }
            }
        }

        await _next(context);
    }

    private string? ExtractApiKey(HttpContext context)
    {
        // Try to get API key from header
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            var apiKey = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                return apiKey;
            }
        }

        // Try to get API key from query parameter
        if (context.Request.Query.TryGetValue(ApiKeyQueryParameter, out var queryValue))
        {
            var apiKey = queryValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                return apiKey;
            }
        }

        // Try to get API key from Authorization header (Bearer format)
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["ApiKey ".Length..].Trim();
        }

        return null;
    }
}

/// <summary>
/// Authentication scheme handler for API key authentication.
/// </summary>
public class ApiKeyAuthenticationSchemeHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly IHMIAuthenticationService _authenticationService;

    public ApiKeyAuthenticationSchemeHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHMIAuthenticationService authenticationService)
        : base(options, logger, encoder)
    {
        _authenticationService = authenticationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = ExtractApiKey();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var authResult = await _authenticationService.AuthenticateApiKeyAsync(apiKey);
            
            if (authResult.IsSuccess && authResult.ApiKey != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, authResult.ApiKey.Id),
                    new(ClaimTypes.Name, authResult.ApiKey.Name),
                    new("ApiKeyId", authResult.ApiKey.Id),
                    new("ApiKeyName", authResult.ApiKey.Name),
                    new("OwnerId", authResult.ApiKey.OwnerId),
                    new("OwnerType", authResult.ApiKey.OwnerType)
                };

                foreach (var scope in authResult.ApiKey.Scopes)
                {
                    claims.Add(new Claim("Scope", scope));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                
                return AuthenticateResult.Success(ticket);
            }
            
            return AuthenticateResult.Fail(authResult.ErrorMessage ?? "API key authentication failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("Authentication error occurred");
        }
    }

    private string? ExtractApiKey()
    {
        // Try header first
        if (Request.Headers.TryGetValue(ApiKeyAuthenticationMiddleware.ApiKeyHeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Try query parameter
        if (Request.Query.TryGetValue(ApiKeyAuthenticationMiddleware.ApiKeyQueryParameter, out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        // Try Authorization header
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["ApiKey ".Length..].Trim();
        }

        return null;
    }
}

/// <summary>
/// Options for API key authentication scheme.
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string AuthenticationType = DefaultScheme;
}
