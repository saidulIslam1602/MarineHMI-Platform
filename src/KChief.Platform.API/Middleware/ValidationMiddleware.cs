using FluentValidation;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System.Text;
using System.Text.Json;

namespace KChief.Platform.API.Middleware;

/// <summary>
/// Middleware for early validation of request models before they reach controllers.
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ValidationMiddleware(
        RequestDelegate next,
        ILogger<ValidationMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST, PUT, PATCH requests with JSON body
        if (context.Request.Method is "POST" or "PUT" or "PATCH" &&
            context.Request.ContentType?.Contains("application/json") == true)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? "unknown";

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                try
                {
                    // Read request body
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    var bodyContent = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(bodyContent))
                    {
                        // Try to parse as JSON and validate structure
                        var jsonDocument = JsonDocument.Parse(bodyContent);
                        
                        // Basic JSON structure validation
                        if (!IsValidJsonStructure(jsonDocument))
                        {
                            await ReturnValidationError(context, correlationId, "Invalid JSON structure");
                            return;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    var correlationId2 = context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? "unknown";
                    Log.Warning(ex, "Invalid JSON in request body");
                    await ReturnValidationError(context, correlationId2, $"Invalid JSON format: {ex.Message}");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsValidJsonStructure(JsonDocument document)
    {
        // Basic validation - can be extended
        return document.RootElement.ValueKind == JsonValueKind.Object || 
               document.RootElement.ValueKind == JsonValueKind.Array;
    }

    private async Task ReturnValidationError(HttpContext context, string correlationId, string message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = StatusCodes.Status400BadRequest,
            detail = message,
            instance = context.Request.Path,
            correlationId = correlationId,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json, Encoding.UTF8);
    }
}

