using System.Text.Json;
using Serilog;
using Serilog.Context;
using HMI.Platform.Core.Exceptions;

namespace HMI.Platform.API.Services;

/// <summary>
/// Service for comprehensive error logging with structured data and correlation tracking.
/// </summary>
public class ErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Logs an exception with comprehensive context information.
    /// </summary>
    public void LogException(Exception exception, HttpContext? httpContext = null, string? correlationId = null)
    {
        correlationId ??= httpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];

        // Create comprehensive log data
        var logData = new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["exception"] = new Dictionary<string, object?>
            {
                ["type"] = exception.GetType().Name,
                ["message"] = exception.Message,
                ["stackTrace"] = exception.StackTrace,
                ["source"] = exception.Source,
                ["hResult"] = exception.HResult,
                ["errorCode"] = exception is HMIException kEx ? kEx.ErrorCode : null,
                ["context"] = exception is HMIException kEx2 ? kEx2.Context : null,
                ["innerException"] = exception.InnerException != null ? new Dictionary<string, object?>
                {
                    ["type"] = exception.InnerException.GetType().Name,
                    ["message"] = exception.InnerException.Message,
                    ["stackTrace"] = exception.InnerException.StackTrace ?? "No stack trace available"
                } : null
            },
            ["request"] = httpContext != null ? new Dictionary<string, object?>
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = httpContext.Request.Path.Value,
                ["queryString"] = httpContext.Request.QueryString.Value,
                ["headers"] = GetSafeHeaders(httpContext.Request.Headers),
                ["userAgent"] = httpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                ["remoteIpAddress"] = httpContext.Connection.RemoteIpAddress?.ToString(),
                ["contentType"] = httpContext.Request.ContentType
            } : null,
            ["user"] = httpContext?.User?.Identity?.Name,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["environment"] = _environment.EnvironmentName
        };

        // Use Serilog for structured logging with rich context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        using (LogContext.PushProperty("ExceptionSource", exception.Source))
        using (LogContext.PushProperty("ExceptionHResult", exception.HResult))
        {
            // Add custom exception context if available
            if (exception is HMIException kchiefEx)
            {
                using (LogContext.PushProperty("ErrorCode", kchiefEx.ErrorCode))
                using (LogContext.PushProperty("ExceptionContext", kchiefEx.Context, destructureObjects: true))
                {
                    LogExceptionWithSerilog(exception, correlationId, httpContext);
                }
            }
            else
            {
                LogExceptionWithSerilog(exception, correlationId, httpContext);
            }
        }
    }

    /// <summary>
    /// Logs a business operation error with context.
    /// </summary>
    public void LogBusinessError(string operation, string errorMessage, object? context = null, string? correlationId = null)
    {
        correlationId ??= Guid.NewGuid().ToString("N")[..8];

        var logData = new
        {
            CorrelationId = correlationId,
            Operation = operation,
            ErrorMessage = errorMessage,
            Context = context,
            Timestamp = DateTimeOffset.UtcNow,
            Environment = _environment.EnvironmentName
        };

        _logger.LogWarning("Business operation error: {Operation} - {ErrorMessage} [CorrelationId: {CorrelationId}]",
            operation, errorMessage, correlationId);

        _logger.LogInformation("Business Error Details: {BusinessErrorDetails}",
            JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    /// <summary>
    /// Logs a security-related event.
    /// </summary>
    public void LogSecurityEvent(string eventType, string description, HttpContext? httpContext = null, string? correlationId = null)
    {
        correlationId ??= httpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];

        var logData = new
        {
            CorrelationId = correlationId,
            EventType = eventType,
            Description = description,
            Request = httpContext != null ? new
            {
                Method = httpContext.Request.Method,
                Path = httpContext.Request.Path.Value,
                RemoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString()
            } : null,
            User = httpContext?.User?.Identity?.Name,
            Timestamp = DateTimeOffset.UtcNow,
            Environment = _environment.EnvironmentName
        };

        _logger.LogWarning("Security event: {EventType} - {Description} [CorrelationId: {CorrelationId}]",
            eventType, description, correlationId);

        _logger.LogInformation("Security Event Details: {SecurityEventDetails}",
            JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static LogLevel DetermineLogLevel(Exception exception)
    {
        return exception switch
        {
            VesselNotFoundException or EngineNotFoundException => LogLevel.Information,
            ValidationException or ArgumentException or ArgumentNullException => LogLevel.Warning,
            VesselOperationException or ProtocolException => LogLevel.Warning,
            UnauthorizedAccessException => LogLevel.Warning,
            TimeoutException => LogLevel.Warning,
            OperationCanceledException => LogLevel.Information,
            _ => LogLevel.Error
        };
    }

    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "Set-Cookie", "X-API-Key", "X-Auth-Token"
        };

        foreach (var header in headers)
        {
            if (!sensitiveHeaders.Contains(header.Key))
            {
                safeHeaders[header.Key] = string.Join(", ", header.Value.AsEnumerable());
            }
            else
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
        }

        return safeHeaders;
    }

    private void LogExceptionWithSerilog(Exception exception, string correlationId, HttpContext? httpContext)
    {
        var logLevel = DetermineLogLevel(exception);
        
        // Add request context if available
        if (httpContext != null)
        {
            using (LogContext.PushProperty("RequestMethod", httpContext.Request.Method))
            using (LogContext.PushProperty("RequestPath", httpContext.Request.Path.Value))
            using (LogContext.PushProperty("RequestQueryString", httpContext.Request.QueryString.Value))
            using (LogContext.PushProperty("ClientIP", GetClientIpAddress(httpContext)))
            using (LogContext.PushProperty("UserAgent", httpContext.Request.Headers.UserAgent.ToString()))
            using (LogContext.PushProperty("UserId", httpContext.User?.Identity?.Name))
            {
                Log.Write(ConvertToSerilogLevel(logLevel), exception,
                    "Exception occurred during {RequestMethod} {RequestPath}: {ExceptionMessage}",
                    httpContext.Request.Method, httpContext.Request.Path, exception.Message);
            }
        }
        else
        {
            Log.Write(ConvertToSerilogLevel(logLevel), exception,
                "Exception occurred: {ExceptionMessage}", exception.Message);
        }
    }

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check for forwarded IP addresses (load balancers, proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static Serilog.Events.LogEventLevel ConvertToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }
}
