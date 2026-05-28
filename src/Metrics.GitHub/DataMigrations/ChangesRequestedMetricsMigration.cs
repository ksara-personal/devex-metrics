using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Metrics.Extensions;

namespace Metrics.GitHub.DataMigrations;

public sealed class ChangesRequestedMetricsMigration : GitHubDataMigration
{
    const string MigrationId = "NullableMetricValues";
    public ChangesRequestedMetricsMigration(GitHubPRMetricsSynchronizer dataSynchronizer, ILogger<ChangesRequestedMetricsMigration> logger) 
        : base(dataSynchronizer, logger)
    {
    }

    public override string EFMigrationId => MigrationId;

    public override async Task RunAsync(DevExMetricDbContext dbContext)
    {
        var lastStatus = GetLastProcessedId(dbContext);
        int? lastId = lastStatus != null ? lastStatus.PRNumber : null;

        Func<IQueryable<PRMetrics>> queryFactory = () => dbContext.PRMetrics
            .AsSingleQuery()
            .Include(p => p.Team)
            .Include(p => p.DevExMetricItem)
            .Where(p => p.Id > lastId.GetValueOrDefault(0)
                && (p.State == "MERGED" || p.State == "CLOSED")
                && p.DevExMetricItem.TotalReviewChangesRequested > 0)
            .OrderBy(p => p.Id);

        _logger.LogInformation("Processing batch of PR metrics after id: {Id}", lastId.GetValueOrDefault(0));
        int totalRecords = 0;
        try
        {
            await foreach (var metrics in queryFactory.Paginate(20))
            {
                _logger.LogInformation("Processing batch of {BatchSize} PR metrics...", metrics.Count());

                var newMetrics = await Task.WhenAll(metrics.Select(metric =>
                {
                    var (owner, repo) = metric.Repository.GetOwnerAndRepoNames();
                    return (_dataSynchronizer as GitHubPRMetricsSynchronizer).GetPRMetricsAsync(owner, repo, metric.PrNumber);
                }));

                foreach (var metric in metrics)
                {
                    var newMetric = newMetrics.FirstOrDefault(m => m.ToString() == metric.ToString());
                    if (newMetric != null)
                    {
                        var oldMetricItem = metric?.DevExMetricItem;
                        var newMetricItem = newMetric?.DevExMetricItem;
                        if (oldMetricItem is not null && newMetricItem is not null && (oldMetricItem.TotalReviewChangesRequested.GetValueOrDefault(0) != newMetricItem.TotalReviewChangesRequested.GetValueOrDefault(0)
                            || oldMetricItem.TeamRequestedChanges.GetValueOrDefault(0) != newMetricItem.TeamRequestedChanges.GetValueOrDefault(0)
                            || oldMetricItem.OthersRequestedChanges.GetValueOrDefault(0) != newMetricItem.OthersRequestedChanges.GetValueOrDefault(0)
                            || oldMetricItem.CodeExcellenceRequestedChanges.GetValueOrDefault(0) != newMetricItem.CodeExcellenceRequestedChanges.GetValueOrDefault(0)))
                        {
                            _logger.LogInformation("Updating PRMetric {Id} on repository {Repository} for ChangesRequested", metric.PrNumber, metric.Repository);
                            metric.DevExMetricItem.TotalReviewChangesRequested = newMetric.DevExMetricItem.TotalReviewChangesRequested;
                            metric.DevExMetricItem.TeamRequestedChanges = newMetric.DevExMetricItem.TeamRequestedChanges;
                            metric.DevExMetricItem.OthersRequestedChanges = newMetric.DevExMetricItem.OthersRequestedChanges;
                            metric.DevExMetricItem.CodeExcellenceRequestedChanges = newMetric.DevExMetricItem.CodeExcellenceRequestedChanges;
                            dbContext.DevExMetricItems.Update(metric.DevExMetricItem);
                        }
                        lastId = metric.Id;
                        UpdateLastProcessedId(dbContext, metric.Id, lastStatus, false);
                    }
                    totalRecords++;
                }
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Processed batch of {BatchSize} records. Total processed: {TotalRecords}", metrics.Count(), totalRecords);
            }

            RemoveLastProcessedId(dbContext);
        }
        finally
        {
            if(lastId.HasValue)
                UpdateLastProcessedId(dbContext, lastId.GetValueOrDefault(0), lastStatus);
        }
    }
}
