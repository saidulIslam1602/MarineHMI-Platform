using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace KChief.Platform.API.Services;

/// <summary>
/// Service for monitoring application performance metrics.
/// </summary>
public class PerformanceMonitoringService : IDisposable
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _vesselOperationDuration;
    private readonly Histogram<double> _databaseOperationDuration;
    private readonly UpDownCounter<long> _activeConnections;
    private readonly ObservableGauge<double> _memoryUsage;
    private readonly ObservableGauge<double> _cpuUsage;
    private readonly Timer _metricsTimer;
    private bool _disposed = false;

    // Performance counters
    private readonly PerformanceCounter? _cpuCounter;
    private readonly Process _currentProcess;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();
        
        // Initialize meter and instruments
        _meter = new Meter("KChief.Platform.API", "1.0.0");
        
        _requestCounter = _meter.CreateCounter<long>(
            "kchief_http_requests_total",
            description: "Total number of HTTP requests");
            
        _errorCounter = _meter.CreateCounter<long>(
            "kchief_http_errors_total", 
            description: "Total number of HTTP errors");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "kchief_http_request_duration_seconds",
            unit: "s",
            description: "HTTP request duration in seconds");
            
        _vesselOperationDuration = _meter.CreateHistogram<double>(
            "kchief_vessel_operation_duration_seconds",
            unit: "s", 
            description: "Vessel operation duration in seconds");
            
        _databaseOperationDuration = _meter.CreateHistogram<double>(
            "kchief_database_operation_duration_seconds",
            unit: "s",
            description: "Database operation duration in seconds");
            
        _activeConnections = _meter.CreateUpDownCounter<long>(
            "kchief_active_connections",
            description: "Number of active connections");

        _memoryUsage = _meter.CreateObservableGauge<double>(
            "kchief_memory_usage_bytes",
            observeValue: () => GC.GetTotalMemory(false),
            unit: "bytes",
            description: "Current memory usage in bytes");

        _cpuUsage = _meter.CreateObservableGauge<double>(
            "kchief_cpu_usage_percent",
            observeValue: GetCpuUsage,
            unit: "%",
            description: "Current CPU usage percentage");

        // Try to initialize CPU counter (may not work on all platforms)
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call returns 0, so we call it once
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize CPU performance counter");
        }

        // Start metrics collection timer
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Performance monitoring service initialized");
    }

    /// <summary>
    /// Records an HTTP request.
    /// </summary>
    public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
    {
        var tags = new TagList();
        tags.Add("method", method);
        tags.Add("endpoint", endpoint);
        tags.Add("status_code", statusCode.ToString());

        _requestCounter.Add(1, tags);
        _requestDuration.Record(durationSeconds, tags);

        if (statusCode >= 400)
        {
            _errorCounter.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a vessel operation.
    /// </summary>
    public void RecordVesselOperation(string operation, string vesselId, bool success, double durationSeconds)
    {
        var tags = new TagList();
        tags.Add("operation", operation);
        tags.Add("vessel_id", vesselId);
        tags.Add("success", success.ToString());

        _vesselOperationDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records a database operation.
    /// </summary>
    public void RecordDatabaseOperation(string operation, string table, bool success, double durationSeconds)
    {
        var tags = new TagList();
        tags.Add("operation", operation);
        tags.Add("table", table);
        tags.Add("success", success.ToString());

        _databaseOperationDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records active connection change.
    /// </summary>
    public void RecordConnectionChange(int delta, string connectionType)
    {
        var tags = new TagList();
        tags.Add("connection_type", connectionType);

        _activeConnections.Add(delta, tags);
    }

    /// <summary>
    /// Gets current CPU usage percentage.
    /// </summary>
    private double GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
                return _cpuCounter.NextValue();
            }
            else
            {
                // Fallback method using Process.TotalProcessorTime
                return _currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get CPU usage");
            return 0;
        }
    }

    /// <summary>
    /// Collects and logs performance metrics periodically.
    /// </summary>
    private void CollectMetrics(object? state)
    {
        try
        {
            var memoryUsage = GC.GetTotalMemory(false);
            var cpuUsage = GetCpuUsage();
            var threadCount = _currentProcess.Threads.Count;
            var handleCount = _currentProcess.HandleCount;

            _logger.LogInformation(
                "Performance Metrics - Memory: {MemoryMB:F2} MB, CPU: {CpuUsage:F2}%, Threads: {ThreadCount}, Handles: {HandleCount}",
                memoryUsage / 1024.0 / 1024.0,
                cpuUsage,
                threadCount,
                handleCount);

            // Force garbage collection every 10 minutes to clean up metrics
            if (DateTime.UtcNow.Minute % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    /// <summary>
    /// Gets current performance statistics.
    /// </summary>
    public object GetPerformanceStats()
    {
        try
        {
            return new
            {
                MemoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                CpuUsagePercent = GetCpuUsage(),
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount,
                WorkingSetMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0,
                PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0,
                GCGen0Collections = GC.CollectionCount(0),
                GCGen1Collections = GC.CollectionCount(1),
                GCGen2Collections = GC.CollectionCount(2),
                ProcessorTime = _currentProcess.TotalProcessorTime,
                StartTime = _currentProcess.StartTime,
                Uptime = DateTime.UtcNow - _currentProcess.StartTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance statistics");
            return new { Error = "Could not retrieve performance statistics" };
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _metricsTimer?.Dispose();
                _cpuCounter?.Dispose();
                _meter?.Dispose();
                _currentProcess?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
