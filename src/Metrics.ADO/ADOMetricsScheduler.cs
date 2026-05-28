using Metrics.DataMigrations;
using Metrics.ADO.EF;

namespace Metrics.ADO;

/// <summary>
/// Scheduler for running ADO metrics related tasks.
/// </summary>
sealed class ADOMetricsScheduler : IMetricsScheduler
{
    /// <summary>
    /// Gets the priority of the ADO metrics scheduler.
    /// </summary>
    public int Priority => 2;

    /// <summary>
    /// Runs the scheduled ADO metrics tasks.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // run the migrations.
        var migrationRunner = serviceProvider.GetRequiredService<MigrationRunner<ADOMetricsDbContext>>();
        await migrationRunner.RunMigrationsAsync();

        // run the sync service
        var synchronizer = serviceProvider.GetRequiredService<DataSyncService<ADOMetricsDbContext>>();
        await synchronizer.WriteMetricsAsync();
    }
}
