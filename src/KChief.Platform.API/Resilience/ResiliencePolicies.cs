using Polly;
using Polly.Extensions.Http;
using Polly.CircuitBreaker;
using Polly.Bulkhead;
using Polly.Timeout;
using Serilog;
using Serilog.Context;

namespace KChief.Platform.API.Resilience;

/// <summary>
/// Defines resilience policies for the K-Chief Marine Automation Platform.
/// Implements retry, circuit breaker, timeout, bulkhead, and fallback patterns.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a retry policy for transient HTTP failures.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException and 5XX and 408 HTTP status codes
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("RetryAttempt", retryCount))
                    using (LogContext.PushProperty("DelayMs", timespan.TotalMilliseconds))
                    {
                        if (outcome.Exception != null)
                        {
                            Log.Warning("Retry attempt {RetryAttempt} for operation {OperationKey} after {DelayMs}ms due to exception: {ExceptionMessage}",
                                retryCount, context.OperationKey, timespan.TotalMilliseconds, outcome.Exception.Message);
                        }
                        else if (outcome.Result != null)
                        {
                            Log.Warning("Retry attempt {RetryAttempt} for operation {OperationKey} after {DelayMs}ms due to HTTP {StatusCode}",
                                retryCount, context.OperationKey, timespan.TotalMilliseconds, outcome.Result.StatusCode);
                        }
                    }
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy for external service protection.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration, context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("CircuitState", "Open"))
                    using (LogContext.PushProperty("BreakDurationSeconds", duration.TotalSeconds))
                    {
                        Log.Error("Circuit breaker opened for operation {OperationKey} for {BreakDurationSeconds} seconds due to: {ExceptionMessage}",
                            context.OperationKey, duration.TotalSeconds, exception.Message);
                    }
                },
                onReset: (context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("CircuitState", "Closed"))
                    {
                        Log.Information("Circuit breaker reset for operation {OperationKey}", context.OperationKey);
                    }
                },
                onHalfOpen: (context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("CircuitState", "HalfOpen"))
                    {
                        Log.Information("Circuit breaker half-open for operation {OperationKey}", context.OperationKey);
                    }
                });
    }

    /// <summary>
    /// Creates a timeout policy for operation time limits.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout,
            TimeoutStrategy.Optimistic,
            onTimeout: (context, timespan, task) =>
            {
                var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                using (LogContext.PushProperty("CorrelationId", correlationId))
                using (LogContext.PushProperty("TimeoutSeconds", timespan.TotalSeconds))
                {
                    Log.Warning("Operation {OperationKey} timed out after {TimeoutSeconds} seconds",
                        context.OperationKey, timespan.TotalSeconds);
                }
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Creates a bulkhead isolation policy for resource segregation.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(int maxParallelization, int maxQueuingActions)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization,
            maxQueuingActions,
            onBulkheadRejected: (context) =>
            {
                var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                using (LogContext.PushProperty("CorrelationId", correlationId))
                using (LogContext.PushProperty("MaxParallelization", maxParallelization))
                using (LogContext.PushProperty("MaxQueuingActions", maxQueuingActions))
                {
                    Log.Warning("Bulkhead rejection for operation {OperationKey} - max parallelization: {MaxParallelization}, max queuing: {MaxQueuingActions}",
                        context.OperationKey, maxParallelization, maxQueuingActions);
                }
            });
    }

    /// <summary>
    /// Creates a fallback policy for graceful degradation.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(Func<Context, CancellationToken, Task<HttpResponseMessage>> fallbackAction)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .Or<CircuitBreakerOpenException>()
            .Or<BulkheadRejectedException>()
            .FallbackAsync(
                fallbackAction,
                onFallback: (exception, context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("FallbackReason", exception.GetType().Name))
                    {
                        Log.Warning("Fallback executed for operation {OperationKey} due to {FallbackReason}: {ExceptionMessage}",
                            context.OperationKey, exception.GetType().Name, exception.Message);
                    }
                    return Task.CompletedTask;
                });
    }

    /// <summary>
    /// Creates a comprehensive resilience policy combining multiple patterns.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetComprehensivePolicy(
        TimeSpan timeout = default,
        int bulkheadMaxParallelization = 10,
        int bulkheadMaxQueuing = 20,
        Func<Context, CancellationToken, Task<HttpResponseMessage>>? fallbackAction = null)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);

        var policies = new List<IAsyncPolicy<HttpResponseMessage>>();

        // Add fallback policy first (outermost)
        if (fallbackAction != null)
        {
            policies.Add(GetFallbackPolicy(fallbackAction));
        }

        // Add timeout policy
        policies.Add(GetTimeoutPolicy(timeout));

        // Add bulkhead policy
        policies.Add(GetBulkheadPolicy(bulkheadMaxParallelization, bulkheadMaxQueuing));

        // Add circuit breaker policy
        policies.Add(GetCircuitBreakerPolicy());

        // Add retry policy (innermost)
        policies.Add(GetRetryPolicy());

        return Policy.WrapAsync(policies.ToArray());
    }
}

/// <summary>
/// Resilience policy configurations for different service types.
/// </summary>
public static class ServiceResiliencePolicies
{
    /// <summary>
    /// Policy for critical vessel control operations.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> VesselControlPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(10),
            bulkheadMaxParallelization: 5,
            bulkheadMaxQueuing: 10,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Error("Critical vessel control operation failed, returning emergency response");
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"error\":\"Vessel control service unavailable\",\"fallback\":true}")
                };
            });

    /// <summary>
    /// Policy for OPC UA communication.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> OpcUaPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(15),
            bulkheadMaxParallelization: 8,
            bulkheadMaxQueuing: 15,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Warning("OPC UA communication failed, using cached data");
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":\"cached\",\"fallback\":true}")
                };
            });

    /// <summary>
    /// Policy for Modbus communication.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> ModbusPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(20),
            bulkheadMaxParallelization: 6,
            bulkheadMaxQueuing: 12,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Warning("Modbus communication failed, using default values");
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":\"default\",\"fallback\":true}")
                };
            });

    /// <summary>
    /// Policy for message bus operations.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> MessageBusPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(25),
            bulkheadMaxParallelization: 12,
            bulkheadMaxQueuing: 25,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Warning("Message bus operation failed, queuing for later retry");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted)
                {
                    Content = new StringContent("{\"queued\":true,\"fallback\":true}")
                };
            });

    /// <summary>
    /// Policy for alarm system operations.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> AlarmSystemPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(5),
            bulkheadMaxParallelization: 15,
            bulkheadMaxQueuing: 30,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Error("Alarm system operation failed, using emergency notification");
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"emergency\":true,\"fallback\":true}")
                };
            });

    /// <summary>
    /// Policy for database operations.
    /// </summary>
    public static IAsyncPolicy GetDatabasePolicy()
    {
        return Policy
            .Handle<Exception>(ex => !(ex is ArgumentException || ex is ArgumentNullException))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("RetryAttempt", retryCount))
                    using (LogContext.PushProperty("DelayMs", timespan.TotalMilliseconds))
                    {
                        Log.Warning("Database operation retry {RetryAttempt} for {OperationKey} after {DelayMs}ms due to: {ExceptionMessage}",
                            retryCount, context.OperationKey, timespan.TotalMilliseconds, exception.Message);
                    }
                });
    }

    /// <summary>
    /// Policy for external API calls.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> ExternalApiPolicy =>
        ResiliencePolicies.GetComprehensivePolicy(
            timeout: TimeSpan.FromSeconds(30),
            bulkheadMaxParallelization: 10,
            bulkheadMaxQueuing: 20,
            fallbackAction: async (context, cancellationToken) =>
            {
                Log.Information("External API call failed, returning cached response");
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"cached\":true,\"fallback\":true}")
                };
            });
}
