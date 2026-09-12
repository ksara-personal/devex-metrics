using Cronos;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Infrastructure;

/// <summary>
/// Hosted background service that periodically triggers metric collection from GitHub using a cron schedule.
/// </summary>
public sealed class MetricsSchedulerService : BackgroundService
{
    /// <summary>
    /// Logger for service lifecycle and error events.
    /// </summary>
    readonly ILogger _logger;

    /// <summary>
    /// Cron expression for scheduling (every 30 minutes).
    /// </summary>
    readonly CronExpression _cronEvery30Minutes = CronExpression.Parse("0/5 * * * *");

    /// <summary>
    /// Time zone for scheduling (UTC).
    /// </summary>
    readonly TimeZoneInfo _timeZone = TimeZoneInfo.Utc;
    readonly TimeZoneInfo _timezoneLocal = TimeZoneInfo.Local;

    /// <summary>
    /// Data connector for writing GitHub metrics.
    /// </summary>
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructs the hosted service with required dependencies.
    /// </summary>
    /// <param name="logger">Logger for service events.</param>
    /// <param name="connector">Data connector for GitHub metrics.</param>
    public MetricsSchedulerService(ILogger<MetricsSchedulerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Main execution loop. Waits until the next scheduled time, then triggers metric collection.
    /// Handles cancellation and logs errors.
    /// </summary>
    /// <param name="stoppingToken">Token to signal service stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsHostedService is starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            var next = _cronEvery30Minutes.GetNextOccurrence(now, _timeZone);
            var nextLocal = _cronEvery30Minutes.GetNextOccurrence(now, _timezoneLocal);

            var delay = next - now;

            _logger.LogInformation("Next run scheduled at UTC {Next}, Local {nextLocal} (in {Delay})", next, nextLocal, delay);
            await Task.Delay(delay.Value, stoppingToken);
            using (var scope = _serviceProvider.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var tenantStore = sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                var tenants = await tenantStore.GetAllAsync();

                foreach (var tenant in tenants)
                {
                    // Create a logging scope with tenant information for all logs within this tenant's execution
                    using (_logger.BeginScope("TenantID: {TenantId}, TenantName: {TenantName};", tenant.Identifier, tenant.Name))
                    {
                        try
                        {
                            _logger.LogInformation("Running scheduled sync for tenant: {TenantId} ({TenantName})", tenant.Identifier, tenant.Name);

                            // Create a new scope per tenant so that scoped services get the correct tenant context
                            using var tenantScope = _serviceProvider.CreateScope();
                            var tenantSp = tenantScope.ServiceProvider;

                            // Set the tenant context for this scope
                            
                            var contextSetter = tenantSp.GetRequiredService<IMultiTenantContextSetter>();
                            var multiTenantContext = new Finbuckle.MultiTenant.Abstractions.MultiTenantContext<AppTenantInfo>( tenant );
                            contextSetter.MultiTenantContext = multiTenantContext;

                            var schedulers = tenantSp.GetServices<IMetricsScheduler>().OrderBy(s => s.Priority).ToList();
                            foreach (var scheduler in schedulers)
                            {
                                var schedulerName = scheduler.GetType().Name;
                                _logger.LogInformation("Starting scheduled fetch for {Scheduler}", schedulerName);
                                await scheduler.RunAsync(tenantSp, stoppingToken);
                                _logger.LogInformation("Completed scheduled fetch for {Scheduler}", schedulerName);
                            }
                        }
                        catch (TaskCanceledException te)
                        {
                            _logger.LogInformation("Shutdown requested, stopping delay");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during scheduled fetch");
                        }
                    }
                }
            }
        }
        _logger.LogInformation("MetricsHostedService is stopped");
    }
}
