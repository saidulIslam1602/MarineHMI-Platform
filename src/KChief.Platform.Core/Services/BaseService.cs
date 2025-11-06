using Serilog;
using Serilog.Context;

namespace KChief.Platform.Core.Services;

/// <summary>
/// Base class for services with common functionality.
/// </summary>
public abstract class BaseService
{
    protected readonly ILogger Logger;

    protected BaseService(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with logging and error handling.
    /// </summary>
    protected async Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", operationName))
        {
            try
            {
                Logger.LogDebug("Starting operation: {Operation}", operationName);
                var result = await operation();
                Logger.LogDebug("Operation completed: {Operation}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in operation: {Operation}", operationName);
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// Executes an operation with logging and error handling (void).
    /// </summary>
    protected async Task ExecuteWithLoggingAsync(
        string operationName,
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", operationName))
        {
            try
            {
                Logger.LogDebug("Starting operation: {Operation}", operationName);
                await operation();
                Logger.LogDebug("Operation completed: {Operation}", operationName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in operation: {Operation}", operationName);
                throw;
            }
        }
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    protected async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        delay ??= TimeSpan.FromSeconds(1);
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    Logger.LogError(ex, "Operation failed after {MaxRetries} attempts", maxRetries);
                    throw;
                }

                Logger.LogWarning(ex, "Operation failed, retrying ({Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(delay.Value, cancellationToken);
            }
        }

        throw new InvalidOperationException("Retry logic failed unexpectedly");
    }

    /// <summary>
    /// Measures execution time of an operation.
    /// </summary>
    protected async Task<T> MeasureExecutionTimeAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            using (LogContext.PushProperty("Operation", operationName))
            using (LogContext.PushProperty("DurationMs", stopwatch.ElapsedMilliseconds))
            {
                Logger.LogDebug("Operation {Operation} completed in {DurationMs}ms", operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Operation {Operation} failed after {DurationMs}ms", operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

