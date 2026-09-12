
namespace Metrics.Application;

/// <summary>
/// Interface for persistence service that handles storage and retrieval of pull request metrics and related data.
/// Provides methods for inserting, querying, and updating PR metrics, reviewers, and run status information.
/// </summary>
public interface IMetricsPersistenceService
{
    /// <summary>
    /// Inserts a range of PR metrics if they do not already exist in the database.
    /// </summary>
    /// <param name="metrics">The collection of PR metrics to insert.</param>
    /// <returns>The number of records inserted.</returns>
    public Task<int> InsertIfNotExistsRangeAsync(IEnumerable<PRMetrics> metrics);

    /// <summary>
    /// Returns PR URLs from the input that do not exist in the database.
    /// </summary>
    /// <param name="values">The collection of PR URLs to check.</param>
    /// <returns>The collection of PR URLs not found in the database.</returns>
    public Task<IEnumerable<string>> GetPRUrlsNotExists(IEnumerable<string> values);

    /// <summary>
    /// Gets the timestamp of the last successful run for a specific repository.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <returns></returns>
    public Task<(DateTime?,int?)> GetLastRunAt(string repoWithOwner);

    /// <summary>
    /// Updates the timestamp of the last run along with optional error information.
    /// This method is used to log the completion time of a run and any associated error details
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="repoWithOwner"></param>
    /// <param name="prNumber"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public Task UpdateLastRunAt(DateTime dateTime, string repoWithOwner, int? prNumber, Exception? exception = null);

    /// <summary>
    /// Inserts a run status record into the database.
    /// This method is used to log the status of a run, including whether it was successful or failed, and any associated error message.
    /// </summary>
    /// <param name="runStatuses"></param>
    /// <returns></returns>
    public Task<int> InsertRunStatusAsync(IEnumerable<RunStatus> runStatuses);

    /// <summary>
    /// Gets all reviewers from the database.
    /// This method retrieves all unique reviewers stored.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<RunStatus>> GetRunStatusesAsync();

    /// <summary>
    /// Gets all PR metrics asynchronously.
    /// This method retrieves all pull request metrics from the database, optionally filtered by team and date range.
    /// It returns an asynchronous enumerable of PR metrics, allowing for efficient streaming of results.  
    /// </summary>
    /// <param name="team"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="includeReviewerMetrcis"></param>
    /// <returns></returns>
    public IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllMetricsAsync(string team = null, DateTime? start = null, DateTime? end = null, bool includeReviewerMetrcis = true);

    /// <summary>
    /// Gets all PR metrics for a given value stream and date range.
    /// </summary>
    /// <param name="valueStream">The value stream name.</param>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The collection of PR metrics.</returns>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByValueStreamAsync(string valueStream, DateTime start, DateTime end);

    /// <summary>
    /// Gets all PR metrics for a given team and date range.
    /// </summary>
    /// <param name="team">The team name.</param>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The collection of PR metrics.</returns>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByTeamAsync(string team, DateTime start, DateTime end);

    /// <summary>
    /// Gets all PR metrics for a given author and date range.
    /// </summary>
    /// <param name="author">The author login.</param>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The collection of PR metrics.</returns>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByAuthorAsync(string author, DateTime start, DateTime end);

    /// <summary>
    /// Gets all PR metrics for a given team region and date range.
    /// </summary>
    /// <param name="teamRegion">The team region name.</param>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The collection of PR metrics.</returns>
    public Task<IEnumerable<PRMetrics>> GetAllPRsByTeamRegionAsync(string teamRegion, DateTime start, DateTime end);

    /// <summary>
    /// Gets a PR with its metrics by PR ID and repository name.
    /// </summary>
    /// <param name="prId">The pull request ID.</param>
    /// <param name="repo">The repository name.</param>
    /// <returns>The PR metrics object, or null if not found.</returns>
    public Task<PRMetrics?> GetPRWithMetricsAsync(int prId, string repo);
    /// <summary>
    /// Gets a summary of reviewer metrics for a specified sprint.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <param name="reviewer"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    public Task<IEnumerable<ReviewerSprintMetrics>> GetReviewerMetricsSummaryBySprintAsync(int year, int sprintNumber, string reviewer = null, string repository = null);
    /// <summary>
    /// Gets a summary of reviewer metrics for a specified month.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <param name="reviewer"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    public Task<IEnumerable<ReviewerMonthlyMetrics>> GetReviewerMetricsSummaryByMonthAsync(int year, int month, string reviewer = null, string repository = null);

    /// <summary>
    /// Gets the author metrics for a specified author and sprint.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public Task<AuthorMetrics> GetAuthorMetricsBySprintAsync(string author, int year, int sprintNumber);
    /// <summary>
    /// Gets the author metrics for a specified author and month.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public Task<AuthorMetrics> GetAuthorMetricsByMonthAsync(string author, int year, int month);

    /// <summary>
    /// Gets the author metrics for a specified team and sprint.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamBySprintAsync(string team, int year, int sprintNumber);
    /// <summary>
    /// Gets the author metrics for a specified team and month.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamByMonthAsync(string team, int year, int month);

    /// <summary>
    /// Gets all the PR's in a given state, states could be open, merged or closed.
    /// </summary>
    /// <returns></returns>
    public IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllPRsByStateAsync(string state = "OPEN", int pageSize = 200);

    /// <summary>
    /// Updates the open prs with state changes.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    public Task<int> UpdatePRsAsync(IEnumerable<PRMetrics> metrics);

    /// <summary>
    /// Gets the raw copilot metrics for the given team.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="team"></param>
    /// <returns></returns>
    public Task<IEnumerable<PRMetrics>> GetCopilotReviewerMetricsByTeamAsync(DateTime start, DateTime end, string? team = null);

    /// <summary>
    /// Gets the summary of copilot metrics for all teams.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Task<IEnumerable<CopilotReviewSummary>> GetCopilotReviewerMetricsForAllTeamsAsync(DateTime start, DateTime end);
}
