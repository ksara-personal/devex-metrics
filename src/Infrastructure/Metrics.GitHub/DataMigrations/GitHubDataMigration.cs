using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// Base class for GitHub data migrations.
/// </summary>
public abstract class GitHubDataMigration : DataMigration<DevExMetricDbContext>
{
    protected GitHubDataMigration(DataSynchronizer dataSynchronizer, ILogger<GitHubDataMigration> logger)
        : base(dataSynchronizer, logger)
    {
    }

    /// <summary>
    /// Gets the last processed run status from the runs table.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected RunStatus? GetLastProcessedId(DevExMetricDbContext context)
    {
        var typeName = this.GetType().Name;
        var lastIdStatus = context.Runs
            .Where(r => r.LastError == typeName && r.LastRunAt == UpdatedAtDefault)
            .SingleOrDefault();

        return lastIdStatus;
    }
    
    /// <summary>
    /// Removes the last processed ID from the runs table.
    /// </summary>
    /// <param name="context"></param>
    protected void RemoveLastProcessedId(DevExMetricDbContext context)
    {
        var typeName = this.GetType().Name;
        var lastIdStatus = context.Runs
            .Where(r => r.LastError == typeName && r.LastRunAt == UpdatedAtDefault)
            .SingleOrDefault();

        if (lastIdStatus != null)
        {
            context.Runs.Remove(lastIdStatus);
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Updates the last processed ID in the runs table asynchronously.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="lastId"></param>
    /// <param name="lastIdStatus"></param>
    protected void UpdateLastProcessedId(DevExMetricDbContext context, int lastId, RunStatus? lastIdStatus, bool autoSave = true)
    {
        var typeName = this.GetType().Name;
        lastIdStatus = context.Runs
            .Where(r => r.LastError == typeName && r.LastRunAt == UpdatedAtDefault)
            .SingleOrDefault();

        // update the last processed id in the runs table.
        if (lastIdStatus == null)
        {
            lastIdStatus = new()
            {
                LastRunAt = UpdatedAtDefault,
                PRNumber = lastId,
                LastError = typeName
            };
            context.Runs.Add(lastIdStatus);
        }
        else
        {
            lastIdStatus.PRNumber = lastId;
            context.Runs.Update(lastIdStatus);
        }
        if (autoSave)
            context.SaveChanges();
    }
    
    /// <summary>
    /// Updates the existing metrics in the database.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="query"></param>
    /// <param name="updateAction"></param>
    /// <returns></returns>
    protected async Task UpdateExistingMetrics(DevExMetricDbContext context,
        Func<IQueryable<PRMetrics>> queryFactory,
        Func<PRMetrics, PRMetrics, bool> updateAction,
        Func<int, IQueryable<PRMetrics>, IQueryable<PRMetrics>>? modifyQuery = null)
    {
        if (queryFactory is null)
            throw new ArgumentNullException("Query is mandatory");
        if (updateAction is null)
            throw new ArgumentNullException("Update action is mandatory");

        int recordsProcessed = 0;
        await foreach (var metrics in queryFactory.Paginate(20))
        {
            _logger.LogInformation("Processing batch of {Count} PR metrics...", metrics.Count());
            var newMetrics = await Task.WhenAll(metrics.Select(metric =>
            {
                var (owner, repo) = metric.Repository.GetOwnerAndRepoNames();
                return (_dataSynchronizer as GitHubPRMetricsSynchronizer).GetPRMetricsAsync(owner, repo, metric.PrNumber);
            }));

            foreach (var metric in metrics)
            {
                var newMetric = newMetrics.FirstOrDefault(m => m.ToString() == metric.ToString());
                if (newMetric != null && updateAction(metric, newMetric))
                {
                    context.PRMetrics.Update(metric);
                }
                recordsProcessed++;
            }

            await context.SaveChangesAsync();

        }
        _logger.LogInformation("Processed total of {RecordsProcessed} PR metrics...", recordsProcessed);
    }
}
