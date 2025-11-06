// ================================================================
// HMI Marine Automation Platform
// ================================================================
// File: Program.cs
// Project: HMI.Platform.API
// Created: 2025
// Author: HMI Development Team
// 
// Description:
// Main application entry point and service configuration for the
// HMI Marine Automation Platform API. Configures dependency
// injection, middleware pipeline, authentication, logging,
// health checks, and all platform services.
//
// Key Configurations:
// - ASP.NET Core Web API with Swagger documentation
// - JWT authentication with role-based authorization
// - Entity Framework Core with SQLite database
// - Serilog structured logging with multiple sinks
// - Health checks for all dependencies and services
// - SignalR real-time communication hubs
// - Resilience patterns (retry, circuit breaker, timeout)
// - Distributed caching with Redis support
// - Background services for data processing
// - API versioning and rate limiting
//
// Environment Support:
// - Development: Enhanced logging, Swagger UI, detailed errors
// - Production: Optimized performance, security headers, monitoring
//
// Copyright (c) 2025 HMI Marine Automation Platform
// Licensed under MIT License
// ================================================================

using Microsoft.EntityFrameworkCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;
using Serilog;
using Serilog.Context;
using HMI.Platform.API.Hubs;
using HMI.Platform.API.Services;
using HMI.Platform.API.HealthChecks;
using HMI.Platform.API.Middleware;
using HMI.Platform.API.Filters;
using HMI.Platform.API.Authorization;
using HMI.Platform.API.Resilience;
using HMI.Platform.API.Services.Caching;
using HMI.Platform.API.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using HMI.Platform.Core.Interfaces;
using HMI.Platform.Core.Models;
using HMI.AlarmSystem.Services;
using HMI.Platform.Core.Telemetry;
using HMI.Platform.API.Services.Telemetry;
using HMI.Platform.API.Swagger;
using HMI.Platform.API.Services.Background;
using HMI.Platform.API.Services.Scheduled;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using HMI.DataAccess.Data;
using HMI.DataAccess.Interfaces;
using HMI.DataAccess.Services;
using HMI.DataAccess.Repositories;
using HMI.Protocols.Modbus.Services;
using HMI.Protocols.OPC.Services;
using HMI.VesselControl.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace HMI.Platform.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog early to capture startup logs
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting K-Chief Marine Automation Platform");
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Use Serilog for logging
            builder.Host.UseSerilog();

        // Add FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        // Add localization for validation messages
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        // Add services to the container
        builder.Services.AddControllers(options =>
        {
            // Add global filters
            options.Filters.Add<FluentValidationFilter>();
            options.Filters.Add<OperationCancelledExceptionFilter>();
        });
        builder.Services.AddEndpointsApiExplorer();
        // Configure Swagger with enhanced documentation
        builder.Services.AddSwaggerGen(c =>
        {
            SwaggerConfiguration.ConfigureSwaggerGen(c);
        });

        // Add Entity Framework
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Configure Caching
        var cacheOptions = builder.Configuration.GetSection("Caching").Get<CacheOptions>() ?? new CacheOptions();
        builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Caching"));

        // Add in-memory caching
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheOptions.InMemoryCacheSizeLimit;
        });

        // Add distributed caching (Redis if configured, otherwise in-memory)
        if (cacheOptions.UseDistributedCache && !string.IsNullOrWhiteSpace(cacheOptions.RedisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = "kchief:";
            });
            
            // Register Redis connection multiplexer
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString!));
        }
        else
        {
            // Fallback to in-memory distributed cache
            builder.Services.AddDistributedMemoryCache();
        }

        // Register cache services
        builder.Services.AddSingleton<InMemoryCacheService>();
        builder.Services.AddSingleton<ICacheService>(sp =>
        {
            var inMemoryCache = sp.GetRequiredService<InMemoryCacheService>();
            var distributedCache = sp.GetService<IDistributedCache>();
            
            // If Redis is configured, use composite cache (in-memory + Redis)
            if (cacheOptions.UseDistributedCache && distributedCache != null)
            {
                var redisCache = new RedisCacheService(
                    distributedCache,
                    sp.GetRequiredService<IOptions<CacheOptions>>(),
                    sp.GetRequiredService<ILogger<RedisCacheService>>());
                
                return new CompositeCacheService(
                    inMemoryCache,
                    redisCache,
                    sp.GetRequiredService<IOptions<CacheOptions>>(),
                    sp.GetRequiredService<ILogger<CompositeCacheService>>());
            }
            
            // Otherwise, use only in-memory cache
            return inMemoryCache;
        });

        // Register cache invalidation service
        builder.Services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();

        // Register response cache
        builder.Services.AddSingleton<IResponseCache, InMemoryResponseCache>();

        // Add SignalR
        builder.Services.AddSignalR();

        // Add Application Insights
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            options.EnableAdaptiveSampling = true;
            options.EnableDependencyTrackingTelemetryModule = true;
            options.EnableRequestTrackingTelemetryModule = true;
            options.EnableEventCounterCollectionModule = true;
            options.EnablePerformanceCounterCollectionModule = true;
        });

        // Register telemetry services
        builder.Services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();
        builder.Services.AddSingleton<CustomMetricsService>();
        builder.Services.AddSingleton<DistributedTracingService>();
        builder.Services.AddSingleton<PerformanceProfilingService>();

        // Configure background services
        builder.Services.Configure<DataPollingOptions>(
            builder.Configuration.GetSection("BackgroundServices:DataPolling"));
        builder.Services.Configure<PeriodicHealthCheckOptions>(
            builder.Configuration.GetSection("BackgroundServices:PeriodicHealthCheck"));
        builder.Services.Configure<DataSynchronizationOptions>(
            builder.Configuration.GetSection("BackgroundServices:DataSynchronization"));
        builder.Services.Configure<MessageQueueOptions>(
            builder.Configuration.GetSection("BackgroundServices:MessageQueue"));

        // Register background services
        builder.Services.AddHostedService<DataPollingService>();
        builder.Services.AddHostedService<PeriodicHealthCheckService>();
        builder.Services.AddHostedService<DataSynchronizationService>();
        builder.Services.AddSingleton<MessageQueueProcessor>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<MessageQueueProcessor>());

        // Register Quartz.NET for scheduled tasks
        builder.Services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });
        });

        builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Register scheduled jobs
        builder.Services.AddScoped<DataCleanupJob>();
        builder.Services.AddScoped<ReportGenerationJob>();
        builder.Services.AddScoped<HealthCheckJob>();
        builder.Services.AddSingleton<ScheduledTaskService>();
        builder.Services.AddSingleton<IJobFactory, JobFactory>();
        builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

        // Add Performance Monitoring
        builder.Services.AddSingleton<PerformanceMonitoringService>();

            // Add Error Logging Service
            builder.Services.AddScoped<ErrorLoggingService>();

            // Add Resilience Service
            builder.Services.AddScoped<ResilienceService>();

            // Add Authentication Services
            builder.Services.AddScoped<IHMIAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // Configure JWT Authentication
            var jwtSecret = builder.Configuration["Authentication:JWT:Secret"];
            var jwtIssuer = builder.Configuration["Authentication:JWT:Issuer"];
            var jwtAudience = builder.Configuration["Authentication:JWT:Audience"];

            if (!string.IsNullOrEmpty(jwtSecret))
            {
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Authentication:Security:RequireHttpsMetadata", false);
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // Configure SignalR JWT authentication
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationSchemeHandler>(
                    ApiKeyAuthenticationSchemeOptions.DefaultScheme, 
                    options => { });
            }

            // Configure Authorization
            builder.Services.AddAuthorization(options =>
            {
                MaritimeAuthorizationPolicies.ConfigurePolicies(options);
            });

            // Register authorization handlers
            builder.Services.AddScoped<IAuthorizationHandler, VesselAccessHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, VesselOwnershipHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, EmergencyAccessHandler>();

        // Add Health Checks
        builder.Services.AddHealthChecks()
            // Basic health checks
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
            .AddDbContextCheck<ApplicationDbContext>("database")
            
            // Custom health checks for dependencies
            .AddCheck<OpcUaHealthCheck>("opcua")
            .AddCheck<ModbusHealthCheck>("modbus") 
            .AddCheck<MessageBusHealthCheck>("messagebus")
            .AddCheck<VesselControlHealthCheck>("vesselcontrol")
            .AddCheck<AlarmSystemHealthCheck>("alarmsystem")
            .AddCheck<RedisHealthCheck>("redis")
            
            // Memory checks
            .AddCheck("memory", () => 
            {
                var allocatedBytes = GC.GetTotalMemory(false);
                var allocatedMB = allocatedBytes / 1024.0 / 1024.0;
                return allocatedMB < 1024 
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {allocatedMB:F2} MB")
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {allocatedMB:F2} MB");
            });

        // Add Health Checks UI
        builder.Services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(30);
            options.MaximumHistoryEntriesPerEndpoint(50);
            options.AddHealthCheckEndpoint("K-Chief API", "/health");
        }).AddInMemoryStorage();

        // Register repositories and Unit of Work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register base repositories
        builder.Services.AddScoped<VesselRepository>();
        builder.Services.AddScoped<EngineRepository>();
        builder.Services.AddScoped<SensorRepository>();
        builder.Services.AddScoped<AlarmRepository>();
        builder.Services.AddScoped<MessageBusEventRepository>();
        
        // Register cached repositories (wrappers around base repositories)
        builder.Services.AddScoped<IVesselRepository>(sp =>
        {
            var baseRepo = sp.GetRequiredService<VesselRepository>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            var cacheInvalidationService = sp.GetRequiredService<ICacheInvalidationService>();
            var logger = sp.GetRequiredService<ILogger<CachedVesselRepository>>();
            return new CachedVesselRepository(baseRepo, cacheService, cacheInvalidationService, logger);
        });
        
        // For other repositories, use base implementations (can be wrapped later if needed)
        builder.Services.AddScoped<IEngineRepository>(sp => sp.GetRequiredService<EngineRepository>());
        builder.Services.AddScoped<ISensorRepository>(sp => sp.GetRequiredService<SensorRepository>());
        builder.Services.AddScoped<IAlarmRepository>(sp => sp.GetRequiredService<AlarmRepository>());
        builder.Services.AddScoped<IMessageBusEventRepository>(sp => sp.GetRequiredService<MessageBusEventRepository>());

            // Register application services
            builder.Services.AddScoped<VesselControlService>(); // Base service
            builder.Services.AddScoped<IVesselControlService, VesselControlService>(); // For backward compatibility
        // Register alarm services
        builder.Services.AddSingleton<AlarmService>(); // Base service
        builder.Services.AddSingleton<AlarmRuleEngine>();
        builder.Services.AddSingleton<AlarmEscalationService>();
        builder.Services.AddSingleton<AlarmGroupingService>();
        builder.Services.AddSingleton<AlarmHistoryService>();
        
        // Register enhanced alarm service as the main service
        builder.Services.AddSingleton<IAlarmService>(sp =>
        {
            var baseService = sp.GetRequiredService<AlarmService>();
            var ruleEngine = sp.GetRequiredService<AlarmRuleEngine>();
            var escalationService = sp.GetRequiredService<AlarmEscalationService>();
            var groupingService = sp.GetRequiredService<AlarmGroupingService>();
            var historyService = sp.GetRequiredService<AlarmHistoryService>();
            var logger = sp.GetRequiredService<ILogger<EnhancedAlarmService>>();
            
            return new EnhancedAlarmService(
                baseService,
                ruleEngine,
                escalationService,
                groupingService,
                historyService,
                logger);
        });
        builder.Services.AddSingleton<IOPCUaClient, OPCUaClientService>();
        builder.Services.AddSingleton<IModbusClient, ModbusClientService>();
        builder.Services.AddSingleton<IMessageBus, MessageBusService>();
        builder.Services.AddSingleton<RealtimeUpdateService>();

        // Add CORS for development
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                SwaggerConfiguration.ConfigureSwaggerUI(c);
            });
        }

        app.UseHttpsRedirection();
        app.UseCors();

        // Add correlation ID middleware (must be very early in pipeline)
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add telemetry middleware (early in pipeline for comprehensive tracking)
        app.UseMiddleware<TelemetryMiddleware>();

        // Add validation middleware (early validation of JSON structure)
        app.UseMiddleware<ValidationMiddleware>();

        // Add request validation middleware (early in pipeline to reject invalid requests)
        var requestValidationOptions = new RequestValidationOptions
        {
            MaxRequestSize = builder.Configuration.GetValue<long>("Middleware:RequestValidation:MaxRequestSize", 10 * 1024 * 1024),
            MaxPathLength = builder.Configuration.GetValue<int>("Middleware:RequestValidation:MaxPathLength", 2048),
            MaxQueryStringLength = builder.Configuration.GetValue<int>("Middleware:RequestValidation:MaxQueryStringLength", 2048),
            AllowedContentTypes = builder.Configuration.GetSection("Middleware:RequestValidation:AllowedContentTypes")
                .Get<List<string>>() ?? new List<string> { "application/json", "application/xml", "multipart/form-data" },
            RequiredHeaders = builder.Configuration.GetSection("Middleware:RequestValidation:RequiredHeaders")
                .Get<List<string>>() ?? new List<string>(),
            BlockedUserAgents = builder.Configuration.GetSection("Middleware:RequestValidation:BlockedUserAgents")
                .Get<List<string>>() ?? new List<string>()
        };
        app.UseMiddleware<RequestValidationMiddleware>(requestValidationOptions);

        // Add rate limiting middleware (after validation, before processing)
        var rateLimitingOptions = new RateLimitingOptions
        {
            RequestsPerWindow = builder.Configuration.GetValue<int>("Middleware:RateLimiting:RequestsPerWindow", 100),
            WindowSize = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Middleware:RateLimiting:WindowSizeSeconds", 60)),
            Strategy = builder.Configuration.GetValue<string>("Middleware:RateLimiting:Strategy", "FixedWindow") == "SlidingWindow"
                ? RateLimitingStrategy.SlidingWindow
                : RateLimitingStrategy.FixedWindow,
            PerEndpointLimiting = builder.Configuration.GetValue<bool>("Middleware:RateLimiting:PerEndpointLimiting", false),
            ExcludedPaths = builder.Configuration.GetSection("Middleware:RateLimiting:ExcludedPaths")
                .Get<List<string>>() ?? new List<string> { "/health", "/health-ui", "/metrics" }
        };
        app.UseMiddleware<RateLimitingMiddleware>(rateLimitingOptions);

        // Add response caching middleware (before request logging for better performance)
        // Note: IResponseCache is injected via DI, options are optional
        app.UseMiddleware<ResponseCachingMiddleware>();

        // Add response time tracking middleware (for detailed performance metrics)
        var responseTimeOptions = new ResponseTimeTrackingOptions
        {
            SlowRequestThresholdMs = builder.Configuration.GetValue<int>("Middleware:ResponseTimeTracking:SlowRequestThresholdMs", 1000),
            IncludeTimingHeaders = builder.Configuration.GetValue<bool>("Middleware:ResponseTimeTracking:IncludeTimingHeaders", true),
            TrackDetailedTimings = builder.Configuration.GetValue<bool>("Middleware:ResponseTimeTracking:TrackDetailedTimings", true)
        };
        app.UseMiddleware<ResponseTimeTrackingMiddleware>(responseTimeOptions);

        // Add request/response logging middleware
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        // Add global exception handling middleware (must be early in pipeline)
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            // Add resilience middleware (before performance monitoring for accurate metrics)
            app.UseResilience();

            // Add performance monitoring middleware
            app.UseMiddleware<PerformanceMonitoringMiddleware>();

            // Add authentication and authorization
            app.UseAuthentication();
            app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
            app.UseAuthorization();
        app.MapControllers();

        // Map SignalR hub
        app.MapHub<VesselHub>("/hubs/vessel");

        // Map Health Check endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Exclude all checks for liveness - just check if app is running
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Map Health Checks UI
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-api";
        });

        // Add performance metrics endpoint
        app.MapGet("/metrics", (PerformanceMonitoringService performanceService) =>
        {
            return Results.Ok(performanceService.GetPerformanceStats());
        }).WithTags("Monitoring");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
