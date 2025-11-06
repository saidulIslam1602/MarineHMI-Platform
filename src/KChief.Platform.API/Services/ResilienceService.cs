using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Context;
using KChief.Platform.API.Resilience;
using KChief.Platform.Core.Exceptions;

namespace KChief.Platform.API.Services;

/// <summary>
/// Service for executing operations with resilience patterns.
/// Provides a centralized way to apply retry, circuit breaker, timeout, and fallback policies.
/// </summary>
public class ResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResilienceService(ILogger<ResilienceService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Executes an operation with vessel control resilience policy.
    /// </summary>
    public async Task<T> ExecuteVesselControlAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "VesselControl");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "VesselControl"))
        {
            try
            {
                Log.Information("Executing vessel control operation: {OperationName}", operationName);
                
                // For non-HTTP operations, we need to create a generic policy
                var policy = CreateGenericPolicy(
                    retryCount: 3,
                    circuitBreakerFailures: 5,
                    circuitBreakerDuration: TimeSpan.FromSeconds(30),
                    timeout: TimeSpan.FromSeconds(10));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("Vessel control operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Vessel control operation failed: {OperationName}", operationName);
                throw new VesselOperationException("unknown", operationName, $"Vessel control operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "VesselControl");
            }
        }
    }

    /// <summary>
    /// Executes an operation with OPC UA resilience policy.
    /// </summary>
    public async Task<T> ExecuteOpcUaAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "OpcUa");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "OpcUa"))
        {
            try
            {
                Log.Information("Executing OPC UA operation: {OperationName}", operationName);
                
                var policy = CreateGenericPolicy(
                    retryCount: 3,
                    circuitBreakerFailures: 5,
                    circuitBreakerDuration: TimeSpan.FromSeconds(30),
                    timeout: TimeSpan.FromSeconds(15));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("OPC UA operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OPC UA operation failed: {OperationName}", operationName);
                throw new ProtocolException("OPC UA", $"Operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "OpcUa");
            }
        }
    }

    /// <summary>
    /// Executes an operation with Modbus resilience policy.
    /// </summary>
    public async Task<T> ExecuteModbusAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "Modbus");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "Modbus"))
        {
            try
            {
                Log.Information("Executing Modbus operation: {OperationName}", operationName);
                
                var policy = CreateGenericPolicy(
                    retryCount: 3,
                    circuitBreakerFailures: 4,
                    circuitBreakerDuration: TimeSpan.FromSeconds(45),
                    timeout: TimeSpan.FromSeconds(20));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("Modbus operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Modbus operation failed: {OperationName}", operationName);
                throw new ProtocolException("Modbus", $"Operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "Modbus");
            }
        }
    }

    /// <summary>
    /// Executes an operation with database resilience policy.
    /// </summary>
    public async Task<T> ExecuteDatabaseAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "Database");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "Database"))
        {
            try
            {
                Log.Debug("Executing database operation: {OperationName}", operationName);
                
                var policy = ServiceResiliencePolicies.GetDatabasePolicy();
                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Debug("Database operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database operation failed: {OperationName}", operationName);
                throw new ProtocolException("Database", $"Database operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "Database");
            }
        }
    }

    /// <summary>
    /// Executes an operation with alarm system resilience policy.
    /// </summary>
    public async Task<T> ExecuteAlarmSystemAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "AlarmSystem");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "AlarmSystem"))
        {
            try
            {
                Log.Information("Executing alarm system operation: {OperationName}", operationName);
                
                var policy = CreateGenericPolicy(
                    retryCount: 2,
                    circuitBreakerFailures: 3,
                    circuitBreakerDuration: TimeSpan.FromSeconds(15),
                    timeout: TimeSpan.FromSeconds(5));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("Alarm system operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Alarm system operation failed: {OperationName}", operationName);
                throw new ProtocolException("AlarmSystem", $"Alarm system operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "AlarmSystem");
            }
        }
    }

    /// <summary>
    /// Executes an operation with message bus resilience policy.
    /// </summary>
    public async Task<T> ExecuteMessageBusAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "MessageBus");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "MessageBus"))
        {
            try
            {
                Log.Information("Executing message bus operation: {OperationName}", operationName);
                
                var policy = CreateGenericPolicy(
                    retryCount: 5,
                    circuitBreakerFailures: 8,
                    circuitBreakerDuration: TimeSpan.FromSeconds(60),
                    timeout: TimeSpan.FromSeconds(25));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("Message bus operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Message bus operation failed: {OperationName}", operationName);
                throw new ProtocolException("MessageBus", $"Message bus operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "MessageBus");
            }
        }
    }

    /// <summary>
    /// Executes an operation with external API resilience policy.
    /// </summary>
    public async Task<T> ExecuteExternalApiAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, "ExternalApi");
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", "ExternalApi"))
        {
            try
            {
                Log.Information("Executing external API operation: {OperationName}", operationName);
                
                var policy = CreateGenericPolicy(
                    retryCount: 3,
                    circuitBreakerFailures: 5,
                    circuitBreakerDuration: TimeSpan.FromSeconds(60),
                    timeout: TimeSpan.FromSeconds(30));

                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("External API operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "External API operation failed: {OperationName}", operationName);
                throw new ProtocolException("ExternalAPI", $"External API operation '{operationName}' failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", "ExternalApi");
            }
        }
    }

    /// <summary>
    /// Executes an operation with a custom resilience policy.
    /// </summary>
    public async Task<T> ExecuteWithCustomPolicyAsync<T>(
        Func<Context, CancellationToken, Task<T>> operation,
        IAsyncPolicy policy,
        string operationName,
        string policyType = "Custom",
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(operationName, policyType);
        
        using (LogContext.PushProperty("OperationName", operationName))
        using (LogContext.PushProperty("PolicyType", policyType))
        {
            try
            {
                Log.Information("Executing operation with custom policy: {OperationName}", operationName);
                
                var result = await policy.ExecuteAsync(operation, context, cancellationToken);
                
                Log.Information("Custom policy operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Custom policy operation failed: {OperationName}", operationName);
                throw new ProtocolException("Custom", $"Operation '{operationName}' with custom policy failed: {ex.Message}")
                    .WithContext("OperationName", operationName)
                    .WithContext("PolicyType", policyType);
            }
        }
    }

    /// <summary>
    /// Creates a context for policy execution with correlation tracking.
    /// </summary>
    private Context CreateContext(string operationName, string policyType)
    {
        var context = new Context(operationName);
        
        // Add correlation ID from HTTP context if available
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? 
                           Guid.NewGuid().ToString("N")[..8];
        
        context["CorrelationId"] = correlationId;
        context["PolicyType"] = policyType;
        context["Timestamp"] = DateTimeOffset.UtcNow;
        
        return context;
    }

    /// <summary>
    /// Creates a generic resilience policy for non-HTTP operations.
    /// </summary>
    private IAsyncPolicy CreateGenericPolicy(
        int retryCount,
        int circuitBreakerFailures,
        TimeSpan circuitBreakerDuration,
        TimeSpan timeout)
    {
        // Retry policy
        var retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is ArgumentException || ex is ArgumentNullException))
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryAttempt, context) =>
                {
                    var correlationId = context.GetValueOrDefault("CorrelationId", "unknown");
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    using (LogContext.PushProperty("RetryAttempt", retryAttempt))
                    using (LogContext.PushProperty("DelayMs", timespan.TotalMilliseconds))
                    {
                        Log.Warning("Retry attempt {RetryAttempt} for operation {OperationKey} after {DelayMs}ms due to: {ExceptionMessage}",
                            retryAttempt, context.OperationKey, timespan.TotalMilliseconds, exception.Message);
                    }
                });

        // Circuit breaker policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: circuitBreakerFailures,
                durationOfBreak: circuitBreakerDuration);

        // Timeout policy
        var timeoutPolicy = Policy.TimeoutAsync(
            timeout,
            Polly.Timeout.TimeoutStrategy.Optimistic);

        // Combine policies: Timeout -> Circuit Breaker -> Retry
        return Policy.WrapAsync(timeoutPolicy, (IAsyncPolicy)circuitBreakerPolicy, retryPolicy);
    }
}
