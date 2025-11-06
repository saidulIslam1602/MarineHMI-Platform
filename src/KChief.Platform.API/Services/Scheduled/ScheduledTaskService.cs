using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace KChief.Platform.API.Services.Scheduled;

/// <summary>
/// Service for managing scheduled tasks using Quartz.NET.
/// </summary>
public class ScheduledTaskService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private readonly ILogger<ScheduledTaskService> _logger;
    private IScheduler? _scheduler;

    public ScheduledTaskService(
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory,
        ILogger<ScheduledTaskService> logger)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = _jobFactory;

        // Schedule tasks
        await ScheduleDataCleanupJob(cancellationToken);
        await ScheduleReportGenerationJob(cancellationToken);
        await ScheduleHealthCheckJob(cancellationToken);

        await _scheduler.Start(cancellationToken);
        _logger.LogInformation("Scheduled task service started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            _logger.LogInformation("Scheduled task service stopped");
        }
    }

    private async Task ScheduleDataCleanupJob(CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<DataCleanupJob>()
            .WithIdentity("data-cleanup-job", "maintenance")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("data-cleanup-trigger", "maintenance")
            .StartNow()
            .WithCronSchedule("0 0 2 * * ?") // Daily at 2 AM
            .Build();

        await _scheduler!.ScheduleJob(job, trigger, cancellationToken);
        _logger.LogInformation("Data cleanup job scheduled");
    }

    private async Task ScheduleReportGenerationJob(CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<ReportGenerationJob>()
            .WithIdentity("report-generation-job", "reports")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("report-generation-trigger", "reports")
            .StartNow()
            .WithCronSchedule("0 0 0 * * ?") // Daily at midnight
            .Build();

        await _scheduler!.ScheduleJob(job, trigger, cancellationToken);
        _logger.LogInformation("Report generation job scheduled");
    }

    private async Task ScheduleHealthCheckJob(CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<HealthCheckJob>()
            .WithIdentity("health-check-job", "monitoring")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("health-check-trigger", "monitoring")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(5)
                .RepeatForever())
            .Build();

        await _scheduler!.ScheduleJob(job, trigger, cancellationToken);
        _logger.LogInformation("Health check job scheduled");
    }
}

/// <summary>
/// Job factory for creating jobs with dependency injection.
/// </summary>
public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob
            ?? throw new InvalidOperationException($"Unable to create job of type {bundle.JobDetail.JobType}");
    }

    public void ReturnJob(IJob job)
    {
        // Jobs are managed by DI container
    }
}

/// <summary>
/// Base class for scheduled jobs.
/// </summary>
public abstract class ScheduledJobBase : IJob
{
    protected readonly ILogger Logger;

    protected ScheduledJobBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            Logger.LogInformation("Executing scheduled job: {JobName}", context.JobDetail.Key.Name);
            await ExecuteJobAsync(context);
            Logger.LogInformation("Scheduled job completed: {JobName}", context.JobDetail.Key.Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing scheduled job: {JobName}", context.JobDetail.Key.Name);
            throw new JobExecutionException(ex);
        }
    }

    protected abstract Task ExecuteJobAsync(IJobExecutionContext context);
}

/// <summary>
/// Job for cleaning up old data.
/// </summary>
[DisallowConcurrentExecution]
public class DataCleanupJob : ScheduledJobBase
{
    public DataCleanupJob(ILogger<DataCleanupJob> logger) : base(logger) { }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        Logger.LogInformation("Starting data cleanup");
        
        // Cleanup old alarm history
        // Cleanup old sensor readings
        // Cleanup old logs
        
        await Task.CompletedTask;
        Logger.LogInformation("Data cleanup completed");
    }
}

/// <summary>
/// Job for generating reports.
/// </summary>
[DisallowConcurrentExecution]
public class ReportGenerationJob : ScheduledJobBase
{
    public ReportGenerationJob(ILogger<ReportGenerationJob> logger) : base(logger) { }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        Logger.LogInformation("Starting report generation");
        
        // Generate daily reports
        // Generate weekly summaries
        // Generate monthly analytics
        
        await Task.CompletedTask;
        Logger.LogInformation("Report generation completed");
    }
}

/// <summary>
/// Job for periodic health checks.
/// </summary>
public class HealthCheckJob : ScheduledJobBase
{
    private readonly Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService _healthCheckService;

    public HealthCheckJob(
        ILogger<HealthCheckJob> logger,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService)
        : base(logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        Logger.LogDebug("Running scheduled health check");
        
        var result = await _healthCheckService.CheckHealthAsync();
        
        if (result.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
        {
            Logger.LogWarning("Health check failed: {Status}", result.Status);
        }
    }
}

