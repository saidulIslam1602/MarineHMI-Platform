using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using Serilog.Context;

namespace KChief.Platform.API.Filters;

/// <summary>
/// Action filter that integrates FluentValidation with ASP.NET Core model validation.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<FluentValidationFilter> _logger;

    public FluentValidationFilter(ILogger<FluentValidationFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString() ?? 
                           Guid.NewGuid().ToString("N")[..8];

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Validate action arguments using FluentValidation
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
                        Log.Warning(
                            "FluentValidation failed for {ArgumentType}: {ErrorCount} errors",
                            argumentType.Name, validationResult.Errors.Count);

                        // Add FluentValidation errors to ModelState
                        foreach (var error in validationResult.Errors)
                        {
                            var propertyName = error.PropertyName;
                            var errorMessage = error.ErrorMessage;

                            // Handle nested properties (e.g., "User.Email")
                            if (propertyName.Contains('.'))
                            {
                                context.ModelState.AddModelError(propertyName, errorMessage);
                            }
                            else
                            {
                                context.ModelState.AddModelError(propertyName, errorMessage);
                            }
                        }

                        // Return validation error response
                        var problemDetails = CreateValidationProblemDetails(context, correlationId);
                        context.Result = new BadRequestObjectResult(problemDetails);
                        return;
                    }
                }
            }

            // Continue to next filter/action
            await next();
        }
    }

    private ValidationProblemDetails CreateValidationProblemDetails(ActionExecutingContext context, string correlationId)
    {
        var validationErrors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        var problemDetails = new ValidationProblemDetails(validationErrors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["method"] = context.HttpContext.Request.Method;
        problemDetails.Extensions["path"] = context.HttpContext.Request.Path.Value;

        // Add correlation ID to response headers
        context.HttpContext.Response.Headers.Append("X-Correlation-ID", correlationId);

        return problemDetails;
    }
}

