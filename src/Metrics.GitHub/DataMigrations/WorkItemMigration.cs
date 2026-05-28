using Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// Migration for work item data.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public sealed class WorkItemMigration : GitHubDataMigration
{
    const string WorkItemMigrationId = "AddWorkItemId";
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItemMigration"/> class.
    /// </summary>
    /// <param name="logger"></param>
    public WorkItemMigration(GitHubPRMetricsSynchronizer dataSynchronizer,
    ILogger<WorkItemMigration> logger) : base(dataSynchronizer, logger)
    {
    }

    /// <summary>
    /// current EF migration identifier.
    /// </summary>
    public override string EFMigrationId => WorkItemMigrationId;

    /// <summary>
    /// Runs the data migration.
    /// </summary>
    /// <returns></returns>
    public override async Task RunAsync(DevExMetricDbContext context)
    {
        var dbContext = (DevExMetricDbContext)context;
        // find the PR metrics that don't have changedfiles set.
        Func<IQueryable<PRMetrics>> query = () => dbContext.PRMetrics.Where(p => p.ChangedFiles == null || p.ChangedFiles == 0);
        await UpdateExistingMetrics(dbContext, query,(existing,newMetric) =>
        {
            existing.WorkItemId = newMetric.WorkItemId;
            existing.WorkItemId2 = newMetric.WorkItemId2;
            existing.ChangedFiles = newMetric.ChangedFiles;
            return true;
        });
    }
}
