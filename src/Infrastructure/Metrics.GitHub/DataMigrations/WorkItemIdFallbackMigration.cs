using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// This migration is a placeholder for the actual migration that adds the SprintCalendar table.
/// It is used to ensure that the EF migration "AddSprintCalendar" is applied before any
/// code that depends on it is executed.
/// </summary>
public sealed class WorkItemIdFallbackMigration : GitHubDataMigration
{
    const string WorkItemMigrationId = "AddSprintCalendar";
    
    /// <summary>
    /// Constructs a new instance of the migration.
    /// </summary>
    /// <param name="dataSynchronizer"></param>
    /// <param name="logger"></param>
    public WorkItemIdFallbackMigration(GitHubPRMetricsSynchronizer dataSynchronizer,
        ILogger<WorkItemIdFallbackMigration> logger) : base(dataSynchronizer, logger)
    {
    }

    /// <summary>
    /// The ID of the EF migration to apply.
    /// </summary>
    public override string EFMigrationId => WorkItemMigrationId;

    /// <summary>
    /// Runs the migration.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public override async Task RunAsync(DevExMetricDbContext dbContext)
    {
        Func<IQueryable<PRMetrics>> query = () => dbContext.PRMetrics.Where(p => p.WorkItemId == null && p.WorkItemId2 == null);

        await UpdateExistingMetrics(dbContext, query, (existing, newMetric) =>
        {
            if (string.IsNullOrEmpty(existing.WorkItemId) && !string.IsNullOrEmpty(newMetric.WorkItemId))
            {
                existing.WorkItemId = newMetric.WorkItemId;
                return true;
            }
            return false;
        });
    }
}
