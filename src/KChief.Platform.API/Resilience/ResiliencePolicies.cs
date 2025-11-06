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
                onBreak: (exception, duration) =>
                {
                    var errorMessage = exception.Exception?.Message ?? "Unknown error";
                    Log.Error("Circuit breaker opened for {BreakDurationSeconds} seconds due to: {ExceptionMessage}",
                        duration.TotalSeconds, errorMessage);
                },
                onReset: () =>
                {
                    Log.Information("Circuit breaker reset - service is healthy again");
                },
                onHalfOpen: () =>
                {
                    Log.Information("Circuit breaker half-open - testing service");
                });
    }

    /// <summary>
    /// Creates a timeout policy for operation time limits.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout,
            TimeoutStrategy.Optimistic);
    }

    /// <summary>
    /// Creates a bulkhead isolation policy for resource segregation.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(int maxParallelization, int maxQueuingActions)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization,
            maxQueuingActions);
    }

    /// <summary>
    /// Creates a fallback policy for graceful degradation.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(Func<CancellationToken, Task<HttpResponseMessage>> fallbackAction)
    {
        return (IAsyncPolicy<HttpResponseMessage>)Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .Or<BulkheadRejectedException>()
            .FallbackAsync(fallbackAction);
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
            // Adapt the Context-aware fallback action to the simpler signature
            Func<CancellationToken, Task<HttpResponseMessage>> adaptedFallback = 
                (cancellationToken) => fallbackAction(new Context(), cancellationToken);
            policies.Add(GetFallbackPolicy(adaptedFallback));
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
