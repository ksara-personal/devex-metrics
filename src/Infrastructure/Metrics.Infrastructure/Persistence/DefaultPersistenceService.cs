

namespace Metrics.Infrastructure;

/// <summary>
/// Default implementation of the IMetricsPersistenceService interface.
/// This class acts as a no-op (null object) implementation, returning default values for all methods.
/// Useful for testing, fallback, or scenarios where persistence is not required.
/// </summary>
public sealed class DefaultPersistenceService : IDbContextMetricsPersistenceService
{
    /// <summary>
    /// Returns null for all PRs by author. No data is persisted or retrieved.
    /// </summary>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByAuthorAsync(string author, DateTime start, DateTime end) =>
        Task.FromResult<IEnumerable<PRMetrics>>(null);

    /// <summary>
    /// Returns null for all PRs by team. No data is persisted or retrieved.
    /// </summary>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByTeamAsync(string team, DateTime start, DateTime end) =>
        Task.FromResult<IEnumerable<PRMetrics>>(null);

    /// <summary>
    /// Returns null for all PRs by team region. No data is persisted or retrieved.
    /// </summary>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByTeamRegionAsync(string teamRegion, DateTime start, DateTime end) =>
        Task.FromResult<IEnumerable<PRMetrics>>(null);

    /// <summary>
    /// Returns null for all PRs by value stream. No data is persisted or retrieved.
    /// </summary>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByValueStreamAsync(string valueStream, DateTime start, DateTime end) =>
        Task.FromResult<IEnumerable<PRMetrics>>(null);

    /// <summary>
    /// Returns null for the last run timestamp. No data is persisted or retrieved.
    /// </summary>
    public Task<(DateTime?,int?)> GetLastRunAt(string repoWithOwner) => Task.FromResult<(DateTime?,int?)>((null,null));

    /// <summary>
    /// Returns null for all metrics. No data is persisted or retrieved.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllMetricsAsync(string team = null, DateTime? start = null, DateTime? end = null, bool includeReviewerMetrcis = true)
    {
        yield break;
    }

    /// <summary>
    /// Returns the input values as-is. No data is persisted or retrieved.
    /// </summary>
    public Task<IEnumerable<string>> GetPRUrlsNotExists(IEnumerable<string> values) => Task.FromResult<IEnumerable<string>>(values);

    /// <summary>
    /// Returns null for PR with metrics. No data is persisted or retrieved.
    /// </summary>
    public Task<PRMetrics?> GetPRWithMetricsAsync(int prId, string repo) => Task.FromResult<PRMetrics?>(null);

    /// <summary>
    /// Returns 0 as no metrics are inserted. No data is persisted or retrieved.
    /// </summary>
    public Task<int> InsertIfNotExistsRangeAsync(IEnumerable<PRMetrics> metrics) => Task.FromResult(0);

    /// <summary>
    /// No operation for updating last run timestamp. No data is persisted or retrieved.
    /// </summary>
    public Task UpdateLastRunAt(DateTime dateTime, string repoWithOwner, int? prNumber, Exception? exception = null) => Task.CompletedTask;

    /// <summary>
    /// Inserts the run statuses into the persistence store.
    /// </summary>
    /// <param name="runStatuses"></param>
    /// <returns></returns>
    public Task<int> InsertRunStatusAsync(IEnumerable<RunStatus> runStatuses) => Task.FromResult(0);

    /// <summary>
    /// Gets all run statuses from the persistence store.
    /// This method retrieves all run statuses stored in the database.
    /// It is useful for aggregating run statuses across all runs.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<RunStatus>> GetRunStatusesAsync() => Task.FromResult<IEnumerable<RunStatus>>(null);

    /// <summary>
    /// Gets all the PR's in a given state, states could be open, merged or closed.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllPRsByStateAsync(string state = "OPEN", int pageSize = 200)
    {
        yield break;
    }

    /// <summary>
    /// Updates the PRs async.
    /// </summary>
    /// <param name="metricIds"></param>
    /// <returns></returns>
    public Task<int> UpdatePRsAsync(IEnumerable<PRMetrics> metricIds) => Task.FromResult(0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="team"></param>
    /// <returns></returns> <summary>
    public Task<IEnumerable<PRMetrics>> GetCopilotReviewerMetricsByTeamAsync(DateTime start, DateTime end, string? team = null) =>
        Task.FromResult<IEnumerable<PRMetrics>>(null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Task<IEnumerable<CopilotReviewSummary>> GetCopilotReviewerMetricsForAllTeamsAsync(DateTime start, DateTime end) =>
        Task.FromResult<IEnumerable<CopilotReviewSummary>>(null);

    /// <summary>
    /// Queries the database asynchronously.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Task<IEnumerable<T>> QueryAsync<T>(Func<DevExMetricDbContext, IQueryable<T>> query) => Task.FromResult<IEnumerable<T>>(null);

    /// <summary>
    /// Upserts the data asynchronously.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public Task<int> UpsertAsync(Func<DevExMetricDbContext, Task> operation) => Task.FromResult(0);

    /// <summary>
    /// Gets a summary of reviewer metrics for a specified sprint.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public Task<IEnumerable<ReviewerSprintMetrics>> GetReviewerMetricsSummaryBySprintAsync(int year, int sprintNumber, string reviewer = null, string repository = null) => Task.FromResult<IEnumerable<ReviewerSprintMetrics>>(null);

    /// <summary>
    /// Gets a summary of reviewer metrics for a specified month.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public Task<IEnumerable<ReviewerMonthlyMetrics>> GetReviewerMetricsSummaryByMonthAsync(int year, int month, string reviewer = null, string repository = null) => Task.FromResult<IEnumerable<ReviewerMonthlyMetrics>>(null);

    public Task<AuthorMetrics> GetAuthorMetricsBySprintAsync(string author, int year, int sprintNumber) => Task.FromResult<AuthorMetrics>(null);

    public Task<AuthorMetrics> GetAuthorMetricsByMonthAsync(string author, int year, int month) => Task.FromResult<AuthorMetrics>(null);

    public Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamBySprintAsync(string team, int year, int sprintNumber) => Task.FromResult<IEnumerable<AuthorMetrics>>(null);
    public Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamByMonthAsync(string team, int year, int month) => Task.FromResult<IEnumerable<AuthorMetrics>>(null);
}
