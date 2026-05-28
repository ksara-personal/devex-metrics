using Metrics.Models;
using Metrics.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// Migration for updating reviewer metrics.
/// </summary>
public sealed class ReviewerMetricsMigration : GitHubDataMigration
{
    const string MigrationId = "ReviewerMetricChanges";
    readonly IMetricsPersistenceService _metricsPersistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewerMetricsMigration"/> class.
    /// </summary>
    /// <param name="dataSynchronizer"></param>
    /// <param name="logger"></param>
    /// <param name="metricsPersistenceService"></param>
    public ReviewerMetricsMigration(GitHubPRMetricsSynchronizer dataSynchronizer,
        ILogger<ReviewerMetricsMigration> logger,
        IMetricsPersistenceService metricsPersistenceService)
        : base(dataSynchronizer, logger)
    {
        _metricsPersistenceService = metricsPersistenceService;
    }

    public override string EFMigrationId => MigrationId;

    /// <summary>
    /// Runs the migration.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public override async Task RunAsync(DevExMetricDbContext dbContext)
    {
        int totalRecords = 0;
        
        _logger.LogInformation("Starting Reviewer Metrics migration...");

        // Use the new migration-specific pagination method
        Func<IQueryable<PRMetrics>> queryFactory = () => dbContext.PRMetrics
            .AsSingleQuery()
            .AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.ReviewerDailyMetrics)
            .Where(p => (p.State == "MERGED" || p.State == "CLOSED") && p.ReviewerDailyMetrics.Count == 0);

        await foreach (var batch in queryFactory.Paginate(20))
        {
            _logger.LogInformation("Processing batch of {BatchSize} PR metrics...", batch.Count());

            var newMetrics = await Task.WhenAll(batch.Select(metric =>
            {
                var (owner, repo) = metric.Repository.GetOwnerAndRepoNames();
                return (_dataSynchronizer as GitHubPRMetricsSynchronizer).GetPRMetricsAsync(owner, repo, metric.PrNumber);
            }));

            totalRecords += newMetrics.Length;
            await _metricsPersistenceService.UpdatePRsAsync(newMetrics);
            
            _logger.LogInformation("Processed batch of {BatchSize} records. Total processed: {TotalRecords}", batch.Count(), totalRecords);
        }
        
        _logger.LogInformation("Reviewer Metrics migration completed successfully. Total records processed: {TotalRecords}", totalRecords);
    }
}
