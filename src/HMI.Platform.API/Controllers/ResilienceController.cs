using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;
using HMI.Platform.API.Services;
using HMI.Platform.Core.Exceptions;

namespace HMI.Platform.API.Controllers;

/// <summary>
/// Controller demonstrating resilience patterns in action.
/// Provides endpoints to test retry, circuit breaker, timeout, and fallback mechanisms.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireObserver")]
[Produces("application/json")]
public class ResilienceController : ControllerBase
{
    private readonly ResilienceService _resilienceService;
    private readonly ErrorLoggingService _errorLoggingService;
    private readonly ILogger<ResilienceController> _logger;

    // Static counters for demonstration purposes
    private static int _failureCounter = 0;
    private static DateTime _lastReset = DateTime.UtcNow;

    public ResilienceController(
        ResilienceService resilienceService,
        ErrorLoggingService errorLoggingService,
        ILogger<ResilienceController> logger)
    {
        _resilienceService = resilienceService;
        _errorLoggingService = errorLoggingService;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates retry policy with simulated transient failures.
    /// </summary>
    [HttpGet("retry-demo")]
    [ProducesResponseType(typeof(ResilienceTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RetryDemo([FromQuery] int failureRate = 50)
    {
        using (LogContext.PushProperty("Operation", "RetryDemo"))
        using (LogContext.PushProperty("FailureRate", failureRate))
        {
            try
            {
                var result = await _resilienceService.ExecuteExternalApiAsync(
                    (context, cancellationToken) =>
                    {
                        // Simulate transient failures
                        var random = new Random();
                        if (random.Next(100) < failureRate)
                        {
                            Log.Warning("Simulated transient failure in retry demo");
                            throw new HttpRequestException("Simulated transient failure");
                        }

                        Log.Information("Retry demo operation succeeded");
                        return Task.FromResult(new ResilienceTestResult
                        {
                            Success = true,
                            Message = "Operation completed successfully after potential retries",
                            Timestamp = DateTime.UtcNow,
                            Pattern = "Retry",
                            AttemptCount = context.GetValueOrDefault("RetryCount", 1)
                        });
                    },
                    "RetryDemo");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _errorLoggingService.LogException(ex, HttpContext);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Retry Demo Failed",
                    Detail = "All retry attempts exhausted",
                    Status = 500
                });
            }
        }
    }

    /// <summary>
    /// Demonstrates circuit breaker pattern with controlled failures.
    /// </summary>
    [HttpGet("circuit-breaker-demo")]
    [ProducesResponseType(typeof(ResilienceTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CircuitBreakerDemo([FromQuery] bool forceFailure = false)
    {
        using (LogContext.PushProperty("Operation", "CircuitBreakerDemo"))
        using (LogContext.PushProperty("ForceFailure", forceFailure))
        {
            try
            {
                var result = await _resilienceService.ExecuteVesselControlAsync(
                    async (context, cancellationToken) =>
                    {
                        // Reset counter every 2 minutes for demo purposes
                        if (DateTime.UtcNow - _lastReset > TimeSpan.FromMinutes(2))
                        {
                            _failureCounter = 0;
                            _lastReset = DateTime.UtcNow;
                            Log.Information("Circuit breaker demo counter reset");
                        }

                        if (forceFailure || _failureCounter < 3)
                        {
                            _failureCounter++;
                            Log.Warning("Simulated failure #{FailureCount} in circuit breaker demo", _failureCounter);
                            throw new VesselOperationException("test-vessel", "simulate", $"Simulated failure #{_failureCounter}");
                        }

                        Log.Information("Circuit breaker demo operation succeeded");
                        return new ResilienceTestResult
                        {
                            Success = true,
                            Message = "Circuit breaker is closed - operation successful",
                            Timestamp = DateTime.UtcNow,
                            Pattern = "CircuitBreaker",
                            AttemptCount = _failureCounter
                        };
                    },
                    "CircuitBreakerDemo");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _errorLoggingService.LogException(ex, HttpContext);
                return StatusCode(503, new ProblemDetails
                {
                    Title = "Circuit Breaker Open",
                    Detail = "Service is temporarily unavailable due to repeated failures",
                    Status = 503
                });
            }
        }
    }

    /// <summary>
    /// Demonstrates timeout policy with configurable delays.
    /// </summary>
    [HttpGet("timeout-demo")]
    [ProducesResponseType(typeof(ResilienceTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> TimeoutDemo([FromQuery] int delaySeconds = 5)
    {
        using (LogContext.PushProperty("Operation", "TimeoutDemo"))
        using (LogContext.PushProperty("DelaySeconds", delaySeconds))
        {
            try
            {
                var result = await _resilienceService.ExecuteOpcUaAsync(
                    async (context, cancellationToken) =>
                    {
                        Log.Information("Starting timeout demo with {DelaySeconds}s delay", delaySeconds);
                        
                        // Simulate long-running operation
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                        Log.Information("Timeout demo operation completed within time limit");
                        return new ResilienceTestResult
                        {
                            Success = true,
                            Message = $"Operation completed within timeout after {delaySeconds}s",
                            Timestamp = DateTime.UtcNow,
                            Pattern = "Timeout",
                            AttemptCount = 1
                        };
                    },
                    "TimeoutDemo");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Timeout demo operation was cancelled due to timeout");
                return StatusCode(408, new ProblemDetails
                {
                    Title = "Operation Timeout",
                    Detail = $"Operation exceeded timeout limit with {delaySeconds}s delay",
                    Status = 408
                });
            }
            catch (Exception ex)
            {
                _errorLoggingService.LogException(ex, HttpContext);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Timeout Demo Failed",
                    Detail = "Operation failed due to unexpected error",
                    Status = 500
                });
            }
        }
    }

    /// <summary>
    /// Demonstrates bulkhead isolation by simulating resource contention.
    /// </summary>
    [HttpGet("bulkhead-demo")]
    [ProducesResponseType(typeof(ResilienceTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> BulkheadDemo([FromQuery] int concurrentRequests = 1)
    {
        using (LogContext.PushProperty("Operation", "BulkheadDemo"))
        using (LogContext.PushProperty("ConcurrentRequests", concurrentRequests))
        {
            try
            {
                var tasks = new List<Task<ResilienceTestResult>>();

                for (int i = 0; i < concurrentRequests; i++)
                {
                    var requestId = i;
                    var task = _resilienceService.ExecuteMessageBusAsync(
                        async (context, cancellationToken) =>
                        {
                            Log.Information("Processing bulkhead demo request #{RequestId}", requestId);
                            
                            // Simulate processing time
                            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                            Log.Information("Completed bulkhead demo request #{RequestId}", requestId);
                            return new ResilienceTestResult
                            {
                                Success = true,
                                Message = $"Request #{requestId} processed successfully",
                                Timestamp = DateTime.UtcNow,
                                Pattern = "Bulkhead",
                                AttemptCount = requestId + 1
                            };
                        },
                        $"BulkheadDemo-{requestId}");

                    tasks.Add(task);
                }

                var results = await Task.WhenAll(tasks);
                
                return Ok(new
                {
                    Success = true,
                    Message = $"Processed {results.Length} concurrent requests successfully",
                    Results = results,
                    Pattern = "Bulkhead"
                });
            }
            catch (Exception ex)
            {
                _errorLoggingService.LogException(ex, HttpContext);
                return StatusCode(429, new ProblemDetails
                {
                    Title = "Bulkhead Rejection",
                    Detail = "Request rejected due to resource capacity limits",
                    Status = 429
                });
            }
        }
    }

    /// <summary>
    /// Demonstrates fallback mechanism with graceful degradation.
    /// </summary>
    [HttpGet("fallback-demo")]
    [ProducesResponseType(typeof(ResilienceTestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> FallbackDemo([FromQuery] bool simulateFailure = true)
    {
        using (LogContext.PushProperty("Operation", "FallbackDemo"))
        using (LogContext.PushProperty("SimulateFailure", simulateFailure))
        {
            try
            {
                var result = await _resilienceService.ExecuteAlarmSystemAsync(
                    async (context, cancellationToken) =>
                    {
                        if (simulateFailure)
                        {
                            Log.Warning("Simulated primary service failure in fallback demo");
                            throw new ProtocolException("Simulation", "Simulated primary service failure");
                        }

                        Log.Information("Primary service succeeded in fallback demo");
                        return new ResilienceTestResult
                        {
                            Success = true,
                            Message = "Primary service response",
                            Timestamp = DateTime.UtcNow,
                            Pattern = "Fallback-Primary",
                            AttemptCount = 1
                        };
                    },
                    "FallbackDemo");

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Fallback response
                Log.Information(ex, "Executing fallback response due to primary service failure");
                
                var fallbackResult = new ResilienceTestResult
                {
                    Success = true,
                    Message = "Fallback service response - graceful degradation",
                    Timestamp = DateTime.UtcNow,
                    Pattern = "Fallback-Secondary",
                    AttemptCount = 1,
                    IsFallback = true
                };

                return Ok(fallbackResult);
            }
        }
    }

    /// <summary>
    /// Gets the current status of resilience patterns.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetResilienceStatus()
    {
        using (LogContext.PushProperty("Operation", "GetResilienceStatus"))
        {
            var status = new
            {
                Timestamp = DateTime.UtcNow,
                Patterns = new
                {
                    Retry = new { Status = "Active", Description = "Exponential backoff with jitter" },
                    CircuitBreaker = new { Status = "Active", Description = "Fail-fast protection" },
                    Timeout = new { Status = "Active", Description = "Operation time limits" },
                    Bulkhead = new { Status = "Active", Description = "Resource isolation" },
                    Fallback = new { Status = "Active", Description = "Graceful degradation" }
                },
                Configuration = new
                {
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    FailureCounter = _failureCounter,
                    LastReset = _lastReset
                },
                Endpoints = new
                {
                    RetryDemo = "/api/resilience/retry-demo?failureRate=50",
                    CircuitBreakerDemo = "/api/resilience/circuit-breaker-demo?forceFailure=false",
                    TimeoutDemo = "/api/resilience/timeout-demo?delaySeconds=5",
                    BulkheadDemo = "/api/resilience/bulkhead-demo?concurrentRequests=3",
                    FallbackDemo = "/api/resilience/fallback-demo?simulateFailure=true"
                }
            };

            Log.Information("Resilience status requested");
            return Ok(status);
        }
    }

    /// <summary>
    /// Resets the demo counters and state.
    /// </summary>
    [HttpPost("reset")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ResetDemo()
    {
        using (LogContext.PushProperty("Operation", "ResetDemo"))
        {
            _failureCounter = 0;
            _lastReset = DateTime.UtcNow;

            Log.Information("Resilience demo state reset by user {UserId}", User.Identity?.Name);
            
            return Ok(new
            {
                Message = "Demo state reset successfully",
                Timestamp = _lastReset,
                ResetBy = User.Identity?.Name
            });
        }
    }
}

/// <summary>
/// Result object for resilience pattern demonstrations.
/// </summary>
public class ResilienceTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public object AttemptCount { get; set; } = 1;
    public bool IsFallback { get; set; } = false;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
