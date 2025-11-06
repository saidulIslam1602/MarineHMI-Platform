using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Exceptions;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for validating incoming requests before they reach controllers.
/// Validates content type, size limits, required headers, and basic security checks.
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;
    private readonly RequestValidationOptions _options;

    public RequestValidationMiddleware(
        RequestDelegate next,
        ILogger<RequestValidationMiddleware> logger,
        RequestValidationOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RequestValidationOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? "unknown";
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Path", context.Request.Path))
        using (LogContext.PushProperty("Method", context.Request.Method))
        {
            try
            {
                // Validate request
                var validationResult = await ValidateRequestAsync(context);
                
                if (!validationResult.IsValid)
                {
                    Log.Warning("Request validation failed: {Reason}", validationResult.ErrorMessage);
                    
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        title = "Bad Request",
                        status = StatusCodes.Status400BadRequest,
                        detail = validationResult.ErrorMessage,
                        instance = context.Request.Path,
                        correlationId = correlationId,
                        timestamp = DateTime.UtcNow
                    };
                    
                    var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    await context.Response.WriteAsync(json, Encoding.UTF8);
                    return;
                }
                
                Log.Debug("Request validation passed");
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during request validation");
                throw;
            }
        }
    }

    private async Task<ValidationResult> ValidateRequestAsync(HttpContext context)
    {
        var request = context.Request;
        
        // 1. Validate Content-Length
        if (request.ContentLength.HasValue && request.ContentLength.Value > _options.MaxRequestSize)
        {
            return ValidationResult.Failure(
                $"Request body size ({request.ContentLength.Value} bytes) exceeds maximum allowed size ({_options.MaxRequestSize} bytes)");
        }
        
        // 2. Validate Content-Type for POST/PUT/PATCH requests
        if (request.Method is "POST" or "PUT" or "PATCH")
        {
            var contentType = request.ContentType?.ToLowerInvariant() ?? string.Empty;
            
            if (string.IsNullOrEmpty(contentType))
            {
                return ValidationResult.Failure("Content-Type header is required for POST/PUT/PATCH requests");
            }
            
            if (!_options.AllowedContentTypes.Any(ct => contentType.Contains(ct, StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Failure(
                    $"Content-Type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _options.AllowedContentTypes)}");
            }
        }
        
        // 3. Validate required headers
        foreach (var requiredHeader in _options.RequiredHeaders)
        {
            if (!request.Headers.ContainsKey(requiredHeader))
            {
                return ValidationResult.Failure($"Required header '{requiredHeader}' is missing");
            }
        }
        
        // 4. Validate path length
        if (request.Path.Value?.Length > _options.MaxPathLength)
        {
            return ValidationResult.Failure($"Request path length exceeds maximum allowed length ({_options.MaxPathLength} characters)");
        }
        
        // 5. Validate query string length
        if (request.QueryString.Value?.Length > _options.MaxQueryStringLength)
        {
            return ValidationResult.Failure($"Query string length exceeds maximum allowed length ({_options.MaxQueryStringLength} characters)");
        }
        
        // 6. Validate against blocked user agents
        var userAgent = request.Headers.UserAgent.ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            foreach (var blockedPattern in _options.BlockedUserAgents)
            {
                if (userAgent.Contains(blockedPattern, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("Blocked user agent detected: {UserAgent}", userAgent);
                    return ValidationResult.Failure("Request blocked due to security policy");
                }
            }
        }
        
        // 7. Validate request body format (if JSON)
        if (request.ContentType?.Contains("application/json") == true && 
            request.ContentLength.HasValue && 
            request.ContentLength.Value > 0)
        {
            try
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var bodyContent = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                
                if (!string.IsNullOrWhiteSpace(bodyContent))
                {
                    // Try to parse as JSON
                    JsonDocument.Parse(bodyContent);
                }
            }
            catch (JsonException ex)
            {
                return ValidationResult.Failure($"Invalid JSON format in request body: {ex.Message}");
            }
        }
        
        // 8. Validate against path-based rules
        foreach (var rule in _options.PathRules)
        {
            if (rule.PathPattern.IsMatch(request.Path.Value ?? string.Empty))
            {
                if (!rule.AllowedMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase))
                {
                    return ValidationResult.Failure(
                        $"Method '{request.Method}' is not allowed for path '{request.Path}'");
                }
            }
        }
        
        return ValidationResult.Success();
    }
}

/// <summary>
/// Options for request validation middleware.
/// </summary>
public class RequestValidationOptions
{
    /// <summary>
    /// Maximum request body size in bytes.
    /// </summary>
    public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB default

    /// <summary>
    /// Maximum path length in characters.
    /// </summary>
    public int MaxPathLength { get; set; } = 2048;

    /// <summary>
    /// Maximum query string length in characters.
    /// </summary>
    public int MaxQueryStringLength { get; set; } = 2048;

    /// <summary>
    /// Allowed content types for POST/PUT/PATCH requests.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "application/json",
        "application/xml",
        "text/xml",
        "multipart/form-data",
        "application/x-www-form-urlencoded"
    };

    /// <summary>
    /// Required headers for all requests.
    /// </summary>
    public List<string> RequiredHeaders { get; set; } = new();

    /// <summary>
    /// Blocked user agent patterns.
    /// </summary>
    public List<string> BlockedUserAgents { get; set; } = new();

    /// <summary>
    /// Path-based validation rules.
    /// </summary>
    public List<PathValidationRule> PathRules { get; set; } = new();
}

/// <summary>
/// Path-based validation rule.
/// </summary>
public class PathValidationRule
{
    /// <summary>
    /// Regular expression pattern for matching paths.
    /// </summary>
    public System.Text.RegularExpressions.Regex PathPattern { get; set; } = null!;

    /// <summary>
    /// Allowed HTTP methods for this path.
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new();
}

/// <summary>
/// Result of request validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

