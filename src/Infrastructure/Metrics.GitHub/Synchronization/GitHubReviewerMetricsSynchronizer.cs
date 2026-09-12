using Metrics.GitHub.ReviewerMetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// GitHub reviewer metrics synchronizer.
/// </summary>
public sealed class GitHubReviewerMetricsSynchronizer : GitHubSynchronizer
{
    const string RankedPRDailyMetricsSqlTemplate = @"
        SELECT * from(
            SELECT pr_metric_id, 
                DENSE_RANK() OVER(ORDER BY pr_metric_id ASC) as dense_rank,
                id, reviewer, date, reviews_requested, reviews_submitted, 
                average_response_time_hours, comment_count, changes_requested, 
                approved, review_comments
            FROM reviewer_daily_metrics 
            WHERE pr_metric_id > {0}
            ORDER BY pr_metric_id
        )t where t.dense_rank <= 20";

    const string DeleteReviewerDailyMetricsSqlTemplate = @"
        DELETE FROM reviewer_daily_metrics 
        WHERE date < {0}";

    readonly SprintCalendar _sprintCalendar;

    /// <summary>
    /// Reviewer aggregation composes queries directly against the EF model, so it needs the
    /// infrastructure persistence contract rather than the storage-agnostic application port.
    /// </summary>
    readonly IDbContextMetricsPersistenceService _store;

    public GitHubReviewerMetricsSynchronizer(ILogger<GitHubSynchronizer> logger,
        GitHubApiClient apiClient,
        ITenantSettingsProvider tenantConfig,
        IDbContextMetricsPersistenceService persistenceService,
        SprintCalendar sprintCalendar)
        : base(logger, apiClient, tenantConfig, persistenceService)
    {
        _store = persistenceService;
        _sprintCalendar = sprintCalendar ?? throw new ArgumentNullException(nameof(sprintCalendar));
    }

    /// <summary>
    /// Updates the reviewer metrics.
    /// </summary>
    /// <returns></returns>
    public override async Task<int> UpdateMetricsAsync()
    {
        // clear up all daily metrics less than 3 months.
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        return await _store.UpsertAsync(async ctx =>
            await ctx.Database.ExecuteSqlRawAsync(DeleteReviewerDailyMetricsSqlTemplate, cutoffDate));
    }

    /// <summary>
    /// Writes the metrics for the specified date range.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public override async Task WriteMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        await WriteAggregatedSprintMetrics();
        await WriteggregatedMonthlyMetrics();
        await WriteAggregatedPRMetrics();
    }

    /// <summary>
    /// Formats the run status key for the specified repository.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <returns></returns>
    protected override string FormatRunStatusKey(string repoWithOwner) => null;

    protected override Task<SearchRoot> GetSearchResults(string repoFilter, DateTime start, DateTime end, string? endCursor) =>
        throw new NotImplementedException();

    protected override Task PersistLastRunAt(DateTime? lastRunAt, string lastRunKey, int? lastPRNumber,
        Exception? exception = null) => Task.CompletedTask;

    protected override Task ProcessSearchResults(string repoWithOwner, SearchRoot results, int? lastPRNumber, bool useLastRun) =>
        throw new NotImplementedException();

    /// <summary>
    /// Writes the aggregated sprint metrics to the persistence service.
    /// </summary>
    /// <returns></returns>
    async Task WriteAggregatedSprintMetrics()
    {
        var latestMetrics = await _store.QueryAsync<ReviewerSprintMetrics>(ctx => ctx.ReviewerSprintMetrics
            .AsNoTracking()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.SprintNumber)
            .Take(1));

        var latest = latestMetrics.Any() ? latestMetrics.FirstOrDefault() : null;
        var current = DateTime.UtcNow;
        
        // find all completed sprints up to today
        var completedSprints = latest != null ?
            _sprintCalendar.GetSprintsAfter(latest.SprintNumber, latest.Year) :
            _sprintCalendar.GetSprintsUpto(current).ToList();
            
        foreach (var sprint in completedSprints)
        {
            // skip future sprints
            if (sprint.StartDate > current || sprint.EndDate > current)
            {
                break;
            }

            _logger.LogInformation("Aggregating reviewer sprint metrics for Sprint {SprintNumber} - {StartDate} to {EndDate}",
                sprint.SprintNumber, sprint.StartDate.ToString("yyyy-MM-dd"), sprint.EndDate.ToString("yyyy-MM-dd"));

            var dailyMetrics = await _store.QueryAsync<ReviewerDailyMetrics>(ctx =>
                ctx.ReviewerDailyMetrics
                    .Include(m => m.PRMetric)
                    .Where(m => m.Date >= DateOnly.FromDateTime(sprint.StartDate)
                    && m.Date <= DateOnly.FromDateTime(sprint.EndDate))
                    .AsNoTracking());

            foreach (var grp in dailyMetrics.GroupBy(m => new GroupingKeys { Reviewer = m.Reviewer, Repository = m.PRMetric.Repository }))
            {
                var sprintMetrics = await _store.QueryAsync<ReviewerSprintMetrics>(ctx =>
                    ctx.ReviewerSprintMetrics.Where(m => m.Reviewer == grp.Key.Reviewer
                        && m.Repository == grp.Key.Repository
                        && m.SprintNumber == sprint.SprintNumber
                        && m.Year == sprint.Year));

                if (!sprintMetrics.Any())
                {
                    var key = grp.Key;
                    var sprintMetric = CreateAggregatedReviewerMetrics<ReviewerSprintMetrics>(key, grp);
                    sprintMetric.SprintNumber = sprint.SprintNumber;
                    sprintMetric.Year = sprint.Year;
                    
                    // persist sprint metric
                    await _store.UpsertAsync(async ctx => await ctx.ReviewerSprintMetrics.AddAsync(sprintMetric));
                }
            }
        }
    }

    /// <summary>
    /// Writes the monthly metrics to the persistence service.
    /// </summary>
    /// <param name="persistenceService"></param>
    /// <returns></returns>
    async Task WriteggregatedMonthlyMetrics()
    {
        var latestMetrics = await _store.QueryAsync<ReviewerMonthlyMetrics>(ctx => ctx.ReviewerMonthlyMetrics
            .AsNoTracking()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .Take(1));

        var latest = latestMetrics.Any() ? latestMetrics.FirstOrDefault() : null;
        for (DateTime startDate = latest != null ? new DateTime(latest.Year, latest.Month, 1).AddMonths(1) : DefaultStartDate, endDate = startDate.AddMonths(1).AddDays(-1); endDate < DateTime.UtcNow; startDate = endDate.AddDays(1), endDate = startDate.AddMonths(1).AddDays(-1))
        {
            _logger.LogInformation("Aggregating reviewer monthly metrics for {StartDate} to {EndDate}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            var dailyMetrics = await _store.QueryAsync<ReviewerDailyMetrics>(ctx =>
                ctx.ReviewerDailyMetrics
                    .Include(m => m.PRMetric)
                    .Where(m => m.Date >= DateOnly.FromDateTime(startDate)
                    && m.Date <= DateOnly.FromDateTime(endDate))
                    .AsNoTracking());

            foreach (var grp in dailyMetrics.GroupBy(m => new MonthlyGroupingKeys { Reviewer = m.Reviewer, Repository = m.PRMetric.Repository, Month = m.Date.Month, Year = m.Date.Year }))
            {
                var key = grp.Key;
                var monthlyMetrics = await _store.QueryAsync<ReviewerMonthlyMetrics>(ctx =>
                    ctx.ReviewerMonthlyMetrics.Where(m => m.Reviewer == key.Reviewer
                        && m.Repository == key.Repository
                        && m.Month == key.Month
                        && m.Year == key.Year).AsNoTracking());

                if (!monthlyMetrics.Any())
                {
                    var monthlyMetric = CreateAggregatedReviewerMetrics<ReviewerMonthlyMetrics>(key, grp);
                    monthlyMetric.Month = key.Month;
                    monthlyMetric.Year = key.Year;

                    // persist monthly metric
                    await _store.UpsertAsync(async ctx => await ctx.ReviewerMonthlyMetrics.AddAsync(monthlyMetric));
                }
            }
        }
    }

    /// <summary>
    /// Writes the monthly metrics to the persistence service.
    /// </summary>
    /// <param name="persistenceService"></param>
    /// <returns></returns>
    async Task WriteAggregatedPRMetrics()
    {
        var latestMetrics = await _store.QueryAsync<PRReviewerMetrics>(ctx => ctx.PRReviewerMetrics
            .AsNoTracking()
            .OrderByDescending(m => m.PRMetricId)
            .Take(1));

        var latest = latestMetrics.Any() ? latestMetrics.FirstOrDefault() : null;
        var startId = latest != null ? latest.PRMetricId : 0;

        _logger.LogInformation("Aggregating PR reviewer metrics after PR Number {StartId}", startId);
       
        do
        {
            // let's use sql to rank and load them in batches of 20 PRs at a time. This is to avoid loading too much data into memory at once.
            var dailyMetrics = await _store.QueryAsync<ReviewerDailyMetrics>(ctx =>
                ctx.ReviewerDailyMetrics
                    .FromSqlRaw(RankedPRDailyMetricsSqlTemplate, startId)
                    .AsNoTracking());

            if (!dailyMetrics.Any())
            {
                break;
            }
            
            int count = 0;
            foreach (var grp in dailyMetrics.GroupBy(m => new PRMetricGroupingKeys { Reviewer = m.Reviewer, PRMetricId = m.PRMetricId }))
            {
                var key = grp.Key;
                var prMetrics = await _store.QueryAsync<PRReviewerMetrics>(ctx =>
                    ctx.PRReviewerMetrics.Where(m => m.Reviewer == key.Reviewer
                        && m.PRMetricId == key.PRMetricId).AsNoTracking());

                if (!prMetrics.Any())
                {
                    var prMetric = CreateReviewerMetrics<PRReviewerMetrics>(key, grp);
                    prMetric.PRMetricId = key.PRMetricId;

                    await _store.UpsertAsync(async ctx => await ctx.PRReviewerMetrics.AddAsync(prMetric));
                    count++;
                }
            }
            startId = dailyMetrics.Max(m => m.PRMetricId);
            _logger.LogInformation("Aggregated {Count} of PR reviewer metrics", count);
        } while (true);
    }

    /// <summary>
    /// Creates reviewer metrics from the grouped daily metrics.
    /// </summary>
    /// <returns></returns>
    T CreateReviewerMetrics<T>(GroupingKeys key, IEnumerable<ReviewerDailyMetrics> groupedMetrics) where T : global::Metrics.Domain.ReviewerMetrics, new()
    {
        var validResponseTimes = groupedMetrics.Where(m => m.AverageResponseTimeHours > 0).ToList();
        return new T
        {
            Reviewer = key.Reviewer,
            ReviewsRequested = groupedMetrics.Sum(m => m.ReviewsRequested.GetValueOrDefault(0)),
            ReviewsSubmitted = groupedMetrics.Sum(m => m.ReviewsSubmitted.GetValueOrDefault(0)),
            CommentCount = groupedMetrics.Sum(m => m.CommentCount.GetValueOrDefault(0)),
            ChangesRequested = groupedMetrics.Sum(m => m.ChangesRequested.GetValueOrDefault(0)),
            Approved = groupedMetrics.Sum(m => m.Approved.GetValueOrDefault(0)),
            ReviewComments = groupedMetrics.Sum(m => m.ReviewComments.GetValueOrDefault(0)),
            AverageResponseTimeHours = validResponseTimes.Any() ? validResponseTimes.Average(m => m.AverageResponseTimeHours) : 0
        };
    }

    /// <summary>
    /// Creates aggregated reviewer metrics from the grouped daily metrics.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="groupedMetrics"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T CreateAggregatedReviewerMetrics<T>(GroupingKeys key, IEnumerable<ReviewerDailyMetrics> groupedMetrics)
        where T : global::Metrics.Domain.AggregatedReviewerMetrics, new()
    {
        var prsForApprovalRate = groupedMetrics.Where(p => p.ReviewsSubmitted.GetValueOrDefault(0) > 0 || p.ReviewsRequested.GetValueOrDefault(0) > 0).Select(p => p.PRMetricId).Distinct().Count();
        var prsReviewed = groupedMetrics.Where(p => p.ReviewsSubmitted.GetValueOrDefault(0) > 0).Select(p => p.PRMetricId).Distinct().Count();

        var metrics = CreateReviewerMetrics<T>(key, groupedMetrics);
        metrics.Repository = key.Repository;
        metrics.TotalReviewDays = groupedMetrics.Select(m => m.Date).Distinct().Count();
        metrics.PrsRequested = groupedMetrics.Where(p => p.ReviewsRequested.GetValueOrDefault(0) > 0).Select(p => p.PRMetricId).Distinct().Count();
        metrics.PrsReviewed = prsReviewed;
        metrics.PrsForApprovalRate = prsForApprovalRate;
        metrics.PrsCommented = groupedMetrics.Where(p => p.CommentCount.GetValueOrDefault(0) > 0).Select(p => p.PRMetricId).Distinct().Count();
        metrics.PrsReviewedWithComments = groupedMetrics.Where(p => p.ReviewsSubmitted.GetValueOrDefault(0) > 0 && p.CommentCount.GetValueOrDefault(0) > 0).Select(p => p.PRMetricId).Distinct().Count();
        metrics.CommentCountWhenReviewed = groupedMetrics.Where(p => p.ReviewsSubmitted.GetValueOrDefault(0) > 0).Sum(p => p.CommentCount) ?? 0;
        metrics.ApprovalRatePercentage = prsForApprovalRate > 0 ? (int)Math.Round(prsReviewed * 100.0 / prsForApprovalRate, 0) : 0;
        metrics.AverageCommentCount = metrics.PrsReviewedWithComments > 0 ? (float)Math.Round(metrics.CommentCountWhenReviewed * 1.0f / metrics.PrsReviewedWithComments, 2) : 0.0f;
        return metrics;
    }
}

record GroupingKeys
{
    public string Reviewer { get; set; }
    public string Repository { get; set; }
}

sealed record PRMetricGroupingKeys : GroupingKeys
{
    public int PRMetricId { get; set; }
}

sealed record MonthlyGroupingKeys : GroupingKeys
{
    public int Month { get; set; }
    public int Year { get; set; }
}
