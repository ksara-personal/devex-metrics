using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// Represents a migration for adding DevEx metric items.
/// </summary>
public sealed class DevExMetricsMigration : GitHubDataMigration
{
    const string MigrationId = "AddDevExMetricItem";
    public DevExMetricsMigration(GitHubPRMetricsSynchronizer dataSynchronizer,
        ILogger<DevExMetricsMigration> logger) : base(dataSynchronizer, logger)
    {
    }

    /// <summary>
    /// Gets the Entity Framework migration ID.
    /// </summary>
    public override string EFMigrationId => MigrationId;

    /// <summary>
    /// Runs the migration.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public override async Task RunAsync(DevExMetricDbContext dbContext)
    {
        // find the PR metrics that don't have changedfiles set.
        Func<IQueryable<PRMetrics>> query = () => dbContext.PRMetrics
            .AsSingleQuery()
            .Include(p => p.DevExMetricItem)
            .Where(p => p.DevExMetricItem == null);

        int count = 0;

        await UpdateExistingMetrics(dbContext, query, (existing, newMetric) =>
        {
            _logger.LogInformation("Existing DevExMetricItem for PRMetric {Id} is:{Old}, New :{New}", existing.Id, existing.DevExMetricItem, newMetric.DevExMetricItem);
            if (existing.DevExMetricItem is null && newMetric.DevExMetricItem is not null)
            {
                existing.DevExMetricItem = newMetric.DevExMetricItem;
                existing.DevExMetricItem.PRMetric = existing;
                existing.DevExMetricItem.PRMetricId = existing.Id;
                dbContext.DevExMetricItems.Add(existing.DevExMetricItem);
                count++;
            }
            newMetric.CopyTo(existing, false, false);
            _logger.LogWarning("PRMetric {Id} already has a DevExMetricItem. Skipping update.", existing.Id);
            return true;
        });
    }
}
