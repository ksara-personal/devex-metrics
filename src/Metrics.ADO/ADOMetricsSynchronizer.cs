using ChoETL;
using Metrics.ADO.EF;
using Metrics.ADO.Models;
using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Metrics.Extensions;

namespace Metrics.ADO;

/// <summary>
/// ADO Metrics Synchronizer
/// </summary>
public sealed class ADOMetricsSynchronizer : DataSynchronizer
{
    readonly WorkItemClient _workItemClient;
    readonly DevExMetricDbContext _devMetricsContext;
    readonly ADOMetricsDbContext _adoDbContext;
    const int MaxWorkItemsPerBatch = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADOMetricsSynchronizer"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="persistenceService"></param>
    public ADOMetricsSynchronizer(ILogger<ADOMetricsSynchronizer> logger,
        ADOMetricsDbContext adoDbContext,
        DevExMetricDbContext dbContext,
        WorkItemClient workItemClient)
        : base(logger, null!)
    {
        _workItemClient = workItemClient;
        _adoDbContext = adoDbContext;
        _devMetricsContext = dbContext;
    }

    /// <summary>
    /// Updates the ADO metrics.
    /// </summary>
    /// <returns></returns>
    public override async Task<int> UpdateMetricsAsync()
    {
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// Writes the ADO metrics.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public override async Task WriteMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        await _adoDbContext.Database.EnsureCreatedAsync();
        // find the last run date from the existing data.
        var lastRunStatus = await _adoDbContext.WorkItemSyncStatus.SingleOrDefaultAsync();
        var start = lastRunStatus?.LastSyncDate.GetValueOrDefault(startDate ?? DataSynchronizer.DefaultStartDate) ?? DataSynchronizer.DefaultStartDate;
        DateTime? currentRunDate = null;
        int lastWorkItemId = 0;

        try
        {
            var wiql = string.Format(WorkItemQueries.EpicsClosedAfterDateWiql, start.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            int total = 0;
            await foreach (var results in _workItemClient.QueryWorkItemsByHierarchyWiql(wiql))
            {
                total += results.Count();
                _logger.LogInformation("Processing Work items count: {Count}, Total: {Total}", results.Count(), total);

                var workItemIds = results.Select(r => r.WorkItemId).ToList();

                var existingWorkItemIds = await _adoDbContext.WorkItemMetrics
                    .AsNoTracking()
                    .Where(m => workItemIds.Contains(m.WorkItemId))
                    .Select(m => m.WorkItemId)
                    .ToListAsync();

                var newResults = results.Where(r => !existingWorkItemIds.Contains(r.WorkItemId)).ToList();

                foreach (var result in newResults)
                {
                    await ProcessWorkItemAsync(result);
                    lastWorkItemId = result.WorkItemId;
                    currentRunDate = result.ClosedDate.UtcDateTime;
                }

                await UpdateLastWorkItemSyncStatusAsync(lastWorkItemId, currentRunDate.Value, false);
                await _adoDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while syncing ADO work items.");
            //throw;
        }
        finally
        {
            if (currentRunDate.HasValue)
            {
                await UpdateLastWorkItemSyncStatusAsync(lastWorkItemId, currentRunDate.Value);
            }
        }
    }
    
    /// <summary>
    /// Updates the last work item sync status asynchronously.
    /// </summary>
    /// <param name="workItemId"></param>
    /// <param name="syncDate"></param>
    /// <param name="save"></param>
    /// <returns></returns> <summary>
    async Task UpdateLastWorkItemSyncStatusAsync(int workItemId, DateTime syncDate, bool save = true)
    {
        var lastRunStatus = await _adoDbContext.WorkItemSyncStatus.SingleOrDefaultAsync();
        if (lastRunStatus == null)
        {
            lastRunStatus = new WorkItemSyncStatus
            {
                LastSyncDate = syncDate,
                WorkItemId = workItemId
            };
            _adoDbContext.WorkItemSyncStatus.Add(lastRunStatus);
        }
        else
        {
            lastRunStatus.LastSyncDate = syncDate;
            lastRunStatus.WorkItemId = workItemId;
            _adoDbContext.WorkItemSyncStatus.Update(lastRunStatus);
        }
        if(save)
            await _adoDbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Processes a work item result and updates metrics accordingly.
    /// </summary>
    /// <param name="workItemResult"></param>
    /// <returns></returns>
    async Task ProcessWorkItemAsync(WorkItemResult workItemResult)
    {
        Dictionary<int, int> children = new();
        var workItemsResult = await _workItemClient.GetRawWorkItemsByWiql(string.Format(WorkItemQueries.EpicHierarchyWiql, workItemResult.WorkItemId));
        if (workItemsResult?.WorkItemRelations == null)
        {
            return;
        }
        
        foreach (var relation in workItemsResult.WorkItemRelations)
        {
            if (relation.Source == null)
            {
                // it is an epic.
                children.TryAdd(relation.Target.Id, 0);
            }
            else
            {
                children.TryAdd(relation.Target.Id, 0);
                children.TryAdd(relation.Source.Id, 0);
            }
        }

        _logger.LogInformation("Processing Work Item ID: {WorkItemId}", workItemResult.WorkItemId);
        await AddOrUpdateMetricsAsync(workItemResult, children.Keys.ToList());
    }
    
    /// <summary>
    /// Fills the metrics asynchronously.
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="children"></param>
    /// <returns></returns> <summary>
    async Task AddOrUpdateMetricsAsync(WorkItemResult workItemResult, List<int> children)
    {
        var workItemId = workItemResult.WorkItemId;
        var allIds = children.Select(id => id.ToString()).ToList();
        allIds.Add(workItemId.ToString());

        var prMetrics = await _devMetricsContext.PRMetrics
                        .Include(pr => pr.DevExMetricItem)
                        .Include(pr => pr.Contributors)
                        .AsNoTracking()
                        .Where(pr => (pr.WorkItemId != null && allIds.Contains(pr.WorkItemId)) || 
                                     (pr.WorkItemId2 != null && allIds.Contains(pr.WorkItemId2)))
                        .ToListAsync();

        if (prMetrics.Any())
        {
            var relVersion = workItemResult.Fields.GetValue<string>("Custom.ReportingVersion");
            if (!string.IsNullOrWhiteSpace(relVersion))
            {
                var match = WorkItemClientExtensions._releaseVersionRegex.Match(relVersion);
                relVersion = match.Success ? match.Value : relVersion;
            }
            var metrics = new WorkItemMetrics
            {
                WorkItemId = workItemId,
                Team = workItemResult.Fields.GetValue<string>("System.AreaLevel3"),
                CreatedDate = workItemResult.CreatedDate,
                ClosedDate = workItemResult.ClosedDate,
                ShirtSize = workItemResult.Fields.GetValue<string>("Custom.ShirtSize"),
                TotalPrs = prMetrics.Count,
                LOC = prMetrics.Sum(pr => pr.TotalLines),
                Commits = prMetrics.Sum(pr => pr.TotalCommits),
                Contributors = prMetrics.SelectMany(pr => pr.Contributors.Select(c => c.Contributor)).Distinct().Count(),
                ReviewComments = prMetrics.Sum(pr => pr.DevExMetricItem.TotalReviewComments ?? 0),
                ChangesRequested = prMetrics.Sum(pr => (pr.DevExMetricItem.TeamRequestedChanges ?? 0) + (pr.DevExMetricItem.CodeExcellenceRequestedChanges ?? 0) + (pr.DevExMetricItem.OthersRequestedChanges ?? 0)),
                PRCycleTime = TimeSpan.FromSeconds(prMetrics.Sum(pr => (pr.DevExMetricItem.CycleTime ?? TimeSpan.Zero).TotalSeconds)),
                MaturityPercentage = prMetrics.Average(pr => pr.DevExMetricItem.MaturityPercentage ?? 0),
                ReleaseVersion = relVersion
            };
            _adoDbContext.WorkItemMetrics.Add(metrics);
        }
    }
}
