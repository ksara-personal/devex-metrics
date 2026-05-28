using System.Runtime.CompilerServices;
using Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Metrics;

/// <summary>
/// Client to query the metrics from the data source (API or database).
/// </summary>
public sealed class DataClient
{
    readonly IMetricsPersistenceService _persistenceService;
    readonly ILogger<DataClient> _logger;
    readonly ConfigService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClient"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configService"></param>
    /// <param name="persistenceService"></param>
    public DataClient(ILogger<DataClient> logger,
        ConfigService configService,
        IMetricsPersistenceService persistenceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
    }

    /// <summary>
    /// Gets the metrics for a given pr id.
    /// </summary>
    /// <param name="repo"></param>
    /// <param name="prId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<PRMetrics> QueryMetricsByIdAsync(string repo, int prId)
    {
        if (string.IsNullOrEmpty(repo))
            throw new ArgumentNullException(nameof(repo));

        if (repo.IndexOf("/") == -1)
            throw new ArgumentException(nameof(repo), "Repository must be a full name");

        var (owner, repoName) = repo.GetOwnerAndRepoNames();
        return _configService.DataStoreType == DataStoreType.API ?
            //await GetPRMetricsAsync(owner, repoName, prId) :
            throw new NotSupportedException("Getting PR metrics by id via API is not supported") :
            await _persistenceService.GetPRWithMetricsAsync(prId, repo);
    }

    /// <summary>
    /// Queries the metrics for the given team and duration.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<IEnumerable<PRMetrics>> QueryMetricsByTeamAsync(string team, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(team))
            throw new ArgumentNullException(nameof(team));

        return await Query<IEnumerable<PRMetrics>>(
            () => throw new NotSupportedException(),
            async () => await _persistenceService.GetAllPRsByTeamAsync(team, start, end));
    }

    /// <summary>
    /// Queries the metrics for the given value stream and duration.
    /// It returns all the PRs that were created in the given value stream and between the given start and end datetimes.
    /// </summary>
    /// <param name="valueStream"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<IEnumerable<PRMetrics>> QueryMetricsByValueStreamAsync(string valueStream, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(valueStream))
            throw new ArgumentNullException(nameof(valueStream));

        return await Query<IEnumerable<PRMetrics>>(
            () => throw new NotSupportedException(),
            async () => await _persistenceService.GetAllPRsByValueStreamAsync(valueStream, start, end));
    }

    /// <summary>
    /// Queries the metrics for the given author and duration.
    /// It returns all the PRs that were created by the given author and between the given start and end datetimes.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<IEnumerable<PRMetrics>> QueryMetricsByAuthorAsync(string author, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(author))
            throw new ArgumentNullException(nameof(author));

        return await Query<IEnumerable<PRMetrics>>(
            () => throw new NotSupportedException(nameof(QueryMetricsByAuthorAsync)),
            async () => await _persistenceService.GetAllPRsByAuthorAsync(author, start, end));
    }

    /// <summary>
    /// Queries the pr metrics where copilot statistics is available.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="team"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<IEnumerable<PRMetrics>> QueryPRsMetricsByCopilotReviewerAsync(DateTime start, DateTime end, string? team = null) =>
        await Query<IEnumerable<PRMetrics>>(
            () => throw new NotSupportedException("Copilot reviewer specific metrics is not supported"),
            async () => await _persistenceService.GetCopilotReviewerMetricsByTeamAsync(start, end, team));

    /// <summary>
    /// Queries the copilot metrics summary for all teams.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CopilotReviewSummary>> QueryCopilotReviewerSummaryForAllTeamsAsync(DateTime start, DateTime end) =>
        await Query<IEnumerable<CopilotReviewSummary>>(
        () => throw new NotSupportedException("Copilot reviewer specific metrics for teams is not supported"),
        async () => await _persistenceService.GetCopilotReviewerMetricsForAllTeamsAsync(start, end));

    /// <summary>
    /// Queries the author metrics by sprint.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <typeparam name="AuthorMetrics"></typeparam>
    /// <returns></returns>
    public async Task<AuthorMetrics> QueryAuthorMetricsBySprintAsync(string author, int year, int sprintNumber) =>
        await Query<AuthorMetrics>(
            () => throw new NotSupportedException(nameof(QueryAuthorMetricsBySprintAsync)),
            async () => await _persistenceService.GetAuthorMetricsBySprintAsync(author, year, sprintNumber));

    /// <summary>
    /// Queries the author metrics by month.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <typeparam name="AuthorMetrics"></typeparam>
    /// <returns></returns>
    public async Task<AuthorMetrics> QueryAuthorMetricsByMonthAsync(string author, int year, int month) =>
        await Query<AuthorMetrics>(
            () => throw new NotSupportedException(nameof(QueryAuthorMetricsByMonthAsync)),
            async () => await _persistenceService.GetAuthorMetricsByMonthAsync(author, year, month));

    /// <summary>
    /// Queries the team metrics by sprint.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public async Task<IEnumerable<AuthorMetrics>> QueryAuthorMetricsForTeamBySprintAsync(string team, int year, int sprintNumber) =>
        await Query<IEnumerable<AuthorMetrics>>(
            () => throw new NotSupportedException(nameof(QueryAuthorMetricsForTeamBySprintAsync)),
            async () => await _persistenceService.GetAuthorMetricsForTeamBySprintAsync(team, year, sprintNumber));

    /// <summary>
    /// Queries the team metrics by month.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public async Task<IEnumerable<AuthorMetrics>> QueryAuthorMetricsForTeamByMonthAsync(string team, int year, int month) =>
        await Query<IEnumerable<AuthorMetrics>>(
            () => throw new NotSupportedException(nameof(QueryAuthorMetricsForTeamByMonthAsync)),
            async () => await _persistenceService.GetAuthorMetricsForTeamByMonthAsync(team, year, month));

    /// <summary>
    /// Queries the reviewer metrics summary by month.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>        
    public async Task<IEnumerable<ReviewerMonthlyMetrics>> QueryReviewerMetricsSummaryByMonthAsync(int year, int month, string reviewer = null, string repository = null) =>
        await Query<IEnumerable<ReviewerMonthlyMetrics>>(
            () => throw new NotSupportedException(nameof(QueryReviewerMetricsSummaryByMonthAsync)),
            async () => await _persistenceService.GetReviewerMetricsSummaryByMonthAsync(year, month, reviewer, repository));
    
    /// <summary>
    /// Queries the reviewer metrics summary by sprint.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ReviewerSprintMetrics>> QueryReviewerMetricsSummaryBySprintAsync(int year, int sprintNumber, string reviewer = null, string repository = null) =>
        await Query<IEnumerable<ReviewerSprintMetrics>>(
            () => throw new NotSupportedException(nameof(QueryReviewerMetricsSummaryBySprintAsync)),
            async () => await _persistenceService.GetReviewerMetricsSummaryBySprintAsync(year, sprintNumber, reviewer, repository));

    /// <summary>
    /// Queries the value based on api vs database mode.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="apiCallback"></param>
    /// <param name="databaseCallback"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    async Task<T> Query<T>(Func<Task<T>> apiCallback, Func<Task<T>> databaseCallback) =>
        _configService.DataStoreType == DataStoreType.API ? await apiCallback?.Invoke() : await databaseCallback?.Invoke();
}
