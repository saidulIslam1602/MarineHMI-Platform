using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using KChief.Platform.API.Services;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for detailed response time tracking and metrics collection.
/// Tracks request processing time, database query time, and other performance metrics.
/// </summary>
public class ResponseTimeTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeTrackingMiddleware> _logger;
    private readonly PerformanceMonitoringService _performanceService;
    private readonly ResponseTimeTrackingOptions _options;

    public ResponseTimeTrackingMiddleware(
        RequestDelegate next,
        ILogger<ResponseTimeTrackingMiddleware> logger,
        PerformanceMonitoringService performanceService,
        ResponseTimeTrackingOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _options = options ?? new ResponseTimeTrackingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? "unknown";
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        // Track different phases of request processing
        var timings = new RequestTimings
        {
            RequestId = correlationId,
            Path = path,
            Method = method,
            StartTime = DateTimeOffset.UtcNow
        };

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Path", path))
        using (LogContext.PushProperty("Method", method))
        {
            try
            {
                // Track middleware pipeline time
                var middlewareStart = Stopwatch.StartNew();
                await _next(context);
                middlewareStart.Stop();
                timings.MiddlewareTime = middlewareStart.ElapsedMilliseconds;

                stopwatch.Stop();
                timings.TotalTime = stopwatch.ElapsedMilliseconds;
                timings.EndTime = DateTimeOffset.UtcNow;
                timings.StatusCode = context.Response.StatusCode;

                // Record metrics
                RecordMetrics(timings);

                // Log response time
                LogResponseTime(timings);

                // Add timing headers to response
                AddTimingHeaders(context, timings);

                // Check for slow requests
                if (timings.TotalTime > _options.SlowRequestThresholdMs)
                {
                    LogSlowRequest(timings);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                timings.TotalTime = stopwatch.ElapsedMilliseconds;
                timings.EndTime = DateTimeOffset.UtcNow;
                timings.HasError = true;

                Log.Error(ex, "Request processing error after {TotalTime}ms", timings.TotalTime);
                throw;
            }
        }
    }

    private void RecordMetrics(RequestTimings timings)
    {
        // Record overall request time
        _performanceService.RecordHttpRequest(
            timings.Method,
            timings.Path,
            timings.StatusCode,
            timings.TotalTime / 1000.0);

        // Record timing breakdown if available
        if (timings.MiddlewareTime > 0)
        {
            _performanceService.RecordMetric("middleware_time_ms", timings.MiddlewareTime);
        }

        // Record by status code
        var statusCodeCategory = GetStatusCodeCategory(timings.StatusCode);
        _performanceService.RecordMetric($"response_time_{statusCodeCategory}_ms", timings.TotalTime);

        // Record by endpoint
        var endpointKey = $"{timings.Method}:{timings.Path}";
        _performanceService.RecordMetric($"endpoint_time_{endpointKey}_ms", timings.TotalTime);
    }

    private void LogResponseTime(RequestTimings timings)
    {
        var logLevel = DetermineLogLevel(timings);

        using (LogContext.PushProperty("TotalTimeMs", timings.TotalTime))
        using (LogContext.PushProperty("MiddlewareTimeMs", timings.MiddlewareTime))
        using (LogContext.PushProperty("StatusCode", timings.StatusCode))
        {
            Log.Write(logLevel,
                "Request completed: {Method} {Path} {StatusCode} in {TotalTime}ms (Middleware: {MiddlewareTime}ms)",
                timings.Method, timings.Path, timings.StatusCode, timings.TotalTime, timings.MiddlewareTime);
        }
    }

    private void AddTimingHeaders(HttpContext context, RequestTimings timings)
    {
        if (_options.IncludeTimingHeaders)
        {
            context.Response.Headers.Append("X-Response-Time-Ms", timings.TotalTime.ToString());
            context.Response.Headers.Append("X-Middleware-Time-Ms", timings.MiddlewareTime.ToString());
            context.Response.Headers.Append("X-Request-Start-Time", timings.StartTime.ToUnixTimeMilliseconds().ToString());
        }
    }

    private void LogSlowRequest(RequestTimings timings)
    {
        using (LogContext.PushProperty("TotalTimeMs", timings.TotalTime))
        using (LogContext.PushProperty("SlowRequestThresholdMs", _options.SlowRequestThresholdMs))
        {
            Log.Warning(
                "Slow request detected: {Method} {Path} took {TotalTime}ms (threshold: {Threshold}ms)",
                timings.Method, timings.Path, timings.TotalTime, _options.SlowRequestThresholdMs);
        }
    }

    private Serilog.Events.LogEventLevel DetermineLogLevel(RequestTimings timings)
    {
        if (timings.HasError)
            return Serilog.Events.LogEventLevel.Error;

        if (timings.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;

        if (timings.StatusCode >= 400)
            return Serilog.Events.LogEventLevel.Warning;

        if (timings.TotalTime > _options.SlowRequestThresholdMs)
            return Serilog.Events.LogEventLevel.Warning;

        return Serilog.Events.LogEventLevel.Information;
    }

    private string GetStatusCodeCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => "5xx",
            >= 400 => "4xx",
            >= 300 => "3xx",
            >= 200 => "2xx",
            _ => "unknown"
        };
    }
}

/// <summary>
/// Options for response time tracking middleware.
/// </summary>
public class ResponseTimeTrackingOptions
{
    /// <summary>
    /// Threshold in milliseconds for considering a request slow.
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to include timing headers in responses.
    /// </summary>
    public bool IncludeTimingHeaders { get; set; } = true;

    /// <summary>
    /// Whether to track detailed timing breakdown.
    /// </summary>
    public bool TrackDetailedTimings { get; set; } = true;
}

/// <summary>
/// Request timing information.
/// </summary>
public class RequestTimings
{
    public string RequestId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public long TotalTime { get; set; }
    public long MiddlewareTime { get; set; }
    public int StatusCode { get; set; }
    public bool HasError { get; set; }
}

