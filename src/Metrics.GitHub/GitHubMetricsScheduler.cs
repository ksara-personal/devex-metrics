using System;
using Metrics.DataMigrations;
using Metrics.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.GitHub;

public sealed class GitHubMetricsScheduler : IMetricsScheduler
{
    /// <summary>
    /// Gets the priority of the GitHub metrics scheduler.
    /// </summary>
    public int Priority => 1;

    /// <summary>
    /// Runs the GitHub metrics scheduler asynchronously.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // run the migrations.
        var migrationRunner = serviceProvider.GetRequiredService<MigrationRunner<DevExMetricDbContext>>();
        await migrationRunner.RunMigrationsAsync();

        // Resolve the connector from the service provider
        var multiOrgUserService = serviceProvider.GetRequiredService<MultiOrgUserService<GitHubOrganization>>();
        multiOrgUserService.InvalidateOrganizationServiceUserCache();

        var dataSynchronizer = serviceProvider.GetRequiredService<DataSyncService<DevExMetricDbContext>>();
        await dataSynchronizer.WriteMetricsAsync();
    }
}