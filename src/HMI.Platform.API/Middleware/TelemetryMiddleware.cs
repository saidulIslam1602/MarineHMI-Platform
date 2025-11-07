using System.Diagnostics;
using HMI.Platform.Core.Middleware;
using HMI.Platform.Core.Telemetry;
using HMI.Platform.API.Services.Telemetry;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog.Context;

namespace HMI.Platform.API.Middleware;

/// <summary>
/// Middleware for comprehensive telemetry collection.
/// </summary>
public class TelemetryMiddleware : BaseMiddleware
{
    private readonly ITelemetryService _telemetryService;
    private readonly CustomMetricsService _metricsService;
    private readonly DistributedTracingService _tracingService;

    public TelemetryMiddleware(
        RequestDelegate next,
        ILogger<TelemetryMiddleware> logger,
        ITelemetryService telemetryService,
        CustomMetricsService metricsService,
        DistributedTracingService tracingService)
        : base(next, logger)
    {
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldProcess(context))
        {
            await Next(context);
            return;
        }

        var correlationId = GetCorrelationId(context);
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Start distributed trace
        var traceParent = context.Request.Headers["traceparent"].FirstOrDefault();
        var activity = _tracingService.StartSpan(
            $"{context.Request.Method} {context.Request.Path}",
            traceParent,
            new Dictionary<string, string>
            {
                ["http.method"] = context.Request.Method,
                ["http.path"] = context.Request.Path,
                ["http.route"] = context.Request.Path,
                ["correlation.id"] = correlationId
            });

        try
        {
            // Add trace context to response headers
            if (activity != null)
            {
                var traceContext = _tracingService.GetTraceContext();
                if (!string.IsNullOrEmpty(traceContext))
                {
                    context.Response.Headers.Append("traceresponse", traceContext);
                }
            }

            await Next(context);

            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            var statusCode = context.Response.StatusCode;
            var success = statusCode < 400;

            // Record metrics
            var endpoint = GetEndpoint(context);
            _metricsService.RecordHttpRequest(
                context.Request.Method,
                endpoint,
                statusCode,
                duration.TotalSeconds);

            // Track request in Application Insights
            _telemetryService.TrackRequest(
                $"{context.Request.Method} {endpoint}",
                startTime,
                duration,
                statusCode.ToString(),
                success);

            // Add tags to activity
            if (activity != null)
            {
                _tracingService.AddTag(activity, "http.status_code", statusCode.ToString());
                _tracingService.AddTag(activity, "http.duration_ms", duration.TotalMilliseconds.ToString("F2"));
                _tracingService.AddTag(activity, "success", success.ToString());
            }

            // Track slow requests
            if (duration.TotalSeconds > 2.0)
            {
                _telemetryService.TrackEvent("SlowRequest", new Dictionary<string, string>
                {
                    ["method"] = context.Request.Method,
                    ["endpoint"] = endpoint,
                    ["duration_ms"] = duration.TotalMilliseconds.ToString("F2"),
                    ["status_code"] = statusCode.ToString(),
                    ["correlation_id"] = correlationId
                });
            }

            Logger.LogDebug(
                "Request telemetry: {Method} {Endpoint} - {StatusCode} ({DurationMs}ms)",
                context.Request.Method, endpoint, statusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            // Track exception
            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                ["method"] = context.Request.Method,
                ["endpoint"] = GetEndpoint(context),
                ["correlation_id"] = correlationId
            });

            // Record error metrics
            _metricsService.RecordHttpRequest(
                context.Request.Method,
                GetEndpoint(context),
                500,
                duration.TotalSeconds);

            // Add error to activity
            if (activity != null)
            {
                _tracingService.AddTag(activity, "error", "true");
                _tracingService.AddTag(activity, "error.message", ex.Message);
                _tracingService.AddEvent(activity, "exception", new Dictionary<string, string>
                {
                    ["exception.type"] = ex.GetType().Name,
                    ["exception.message"] = ex.Message
                });
            }

            throw;
        }
        finally
        {
            // End trace span
            if (activity != null)
            {
                _tracingService.EndSpan(activity, context.Response.StatusCode < 400);
            }
        }
    }

    private string GetEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        
        // Normalize path (remove IDs, etc.)
        if (path.Contains("/api/"))
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                // Return controller/action pattern
                return $"/{string.Join("/", parts.Take(2))}";
            }
        }

        return path;
    }
}

