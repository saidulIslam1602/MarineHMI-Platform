using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

namespace KChief.Platform.API.Swagger;

/// <summary>
/// Configuration for Swagger/OpenAPI documentation.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger generation options.
    /// </summary>
    public static void ConfigureSwaggerGen(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "K-Chief Marine Automation Platform API",
            Version = "v1",
            Description = @"
                Comprehensive API for managing marine vessel automation systems.
                
                ## Features
                - Vessel management and monitoring
                - Engine control and diagnostics
                - Sensor data collection
                - Alarm system with rules engine
                - Real-time updates via SignalR
                - Advanced telemetry and metrics
                
                ## Authentication
                The API supports two authentication methods:
                1. **JWT Bearer Token**: Use the `/api/auth/login` endpoint to obtain a token
                2. **API Key**: Include `X-API-Key` header in requests
                
                ## Rate Limiting
                API requests are rate-limited to prevent abuse. Default limits:
                - 100 requests per minute per IP address
                - Some endpoints may have specific limits
                
                ## Error Responses
                The API uses standard HTTP status codes:
                - `200 OK`: Successful request
                - `201 Created`: Resource created successfully
                - `400 Bad Request`: Invalid request data
                - `401 Unauthorized`: Authentication required
                - `403 Forbidden`: Insufficient permissions
                - `404 Not Found`: Resource not found
                - `429 Too Many Requests`: Rate limit exceeded
                - `500 Internal Server Error`: Server error
                
                All error responses include a `ProblemDetails` object with detailed information.
            ",
            Contact = new OpenApiContact
            {
                Name = "K-Chief Support",
                Email = "support@kchief.com",
                Url = new Uri("https://kchief.com/support")
            },
            License = new OpenApiLicense
            {
                Name = "Proprietary",
                Url = new Uri("https://kchief.com/license")
            },
            TermsOfService = new Uri("https://kchief.com/terms")
        });

        // Include XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // Add JWT Bearer authentication
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"
                JWT Authorization header using the Bearer scheme.
                
                Enter 'Bearer' [space] and then your token in the text input below.
                
                Example: 'Bearer 12345abcdef'
                
                Get your token from the /api/auth/login endpoint.
            ",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        // Add API Key authentication
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = @"
                API Key authentication.
                
                Enter your API key in the text input below.
                
                Example: 'your-api-key-here'
                
                Contact support to obtain an API key.
            ",
            Name = "X-API-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });

        // Apply security to all endpoints
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            },
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Enable annotations
        options.EnableAnnotations();

        // Custom operation filters
        options.OperationFilter<ResponseExamplesOperationFilter>();
        options.OperationFilter<ErrorResponseOperationFilter>();
        options.SchemaFilter<ExampleSchemaFilter>();

        // Group endpoints by tags
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });
        options.DocInclusionPredicate((name, api) => true);
    }

    /// <summary>
    /// Configures Swagger UI options.
    /// </summary>
    public static void ConfigureSwaggerUI(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "K-Chief API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "K-Chief Marine Automation Platform API";
        options.DefaultModelsExpandDepth(-1);
        options.DefaultModelExpandDepth(2);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableDeepLinking();
        options.EnableFilter();
        options.EnableValidator();
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        
        // Add custom CSS
        options.InjectStylesheet("/swagger-ui/custom.css");
        
        // Add custom JavaScript
        options.InjectJavascript("/swagger-ui/custom.js");
    }
}

