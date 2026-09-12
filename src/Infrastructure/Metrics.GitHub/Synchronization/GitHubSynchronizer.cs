using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// Base class for GitHub data synchronizers.
/// </summary>
public abstract class GitHubSynchronizer : DataSynchronizer
{
    protected readonly GitHubApiClient _apiClient;
    protected readonly SourceControlSettings<GitHubOrganization> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubSynchronizer"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="apiClient"></param>
    /// <param name="tenantConfig"></param>
    /// <param name="persistenceService"></param>
    protected GitHubSynchronizer(ILogger<GitHubSynchronizer> logger,
        GitHubApiClient apiClient,
        ITenantSettingsProvider tenantConfig,
        IMetricsPersistenceService persistenceService)
        : base(logger, persistenceService)
    {
        _apiClient = apiClient;
        _settings = tenantConfig.GetSettings<SourceControlSettings<GitHubOrganization>>(GitHubServiceConfigurator.GitHubSectionName)
            ?? throw new InvalidOperationException("GitHub settings not configured for tenant");
    }

    /// <summary>
    /// Processes the paginated search results.
    /// </summary>
    /// <param name="repoFilter"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="endCursor"></param>
    /// <returns></returns>
    protected abstract Task<SearchRoot> GetSearchResults(string repoFilter, DateTime start, DateTime end, string? endCursor);

    /// <summary>
    /// Processes the paginated search results.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <param name="results"></param>
    /// <param name="lastPRNumber"></param>
    /// <returns></returns>
    protected abstract Task ProcessSearchResults(string repoWithOwner, SearchRoot results, int? lastPRNumber, bool useLastRun = true);

    /// <summary>
    /// Formats the run status key for the specified repository.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <returns></returns>
    protected abstract string FormatRunStatusKey(string repoWithOwner);

    /// <summary>
    /// Enumerates all repositories for the specified organization and invokes the callback for each repository.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    protected internal virtual async Task EnumerateOrgsAndWriteMetricsAsync(DateTime? startDate, DateTime endDate)
    {
        foreach (var org in _settings.Organizations)
        {
            var owner = org.Owner;
            if (string.IsNullOrEmpty(owner))
                throw new ArgumentOutOfRangeException(nameof(owner), "Organization owner is not configured in appsettings.json");

            var repos = org.Repositories;
            if (repos is null || repos.Count == 0)
            {
                _logger.LogWarning("No repositories found for organization {owner}", owner);
                continue;
            }

            foreach (var repo in org.Repositories)
            {
                _logger.LogInformation("Synchronizing metrics for organization {Owner} and repo {Repo}", owner, repo);
                await SyncPullRequests(string.Concat(owner, "/", repo), startDate, endDate);
            }
        }
    }

    /// <summary>
    /// Search PR's by repository within the specified date range.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="searchFunc"></param>
    /// <param name="useLastRun"></param>
    /// <returns></returns>
    protected internal virtual async Task SyncPullRequests(string repoWithOwner,
        DateTime? startDate,
        DateTime end,
        bool useLastRun = true)
    {
        _logger.LogInformation("Searching PR's for repository {Repo}", repoWithOwner);
        var lastRunKey = FormatRunStatusKey(repoWithOwner);
        var (lastRunAt, lastPRNumber) = useLastRun ? await _persistenceService.GetLastRunAt(lastRunKey) : (null, null);

        DateTime start = lastRunAt is not null ? lastRunAt.Value : startDate.HasValue ? startDate.Value : DefaultStartDate;
        DateTime newEnd = end;
        Exception? exception = null;

        var repoFilter = string.Concat("repo:", repoWithOwner);
        try
        {
            do
            {
                newEnd = start.AddDays(30);
                newEnd = newEnd < end ? newEnd : end;

                _logger.LogInformation("Adjusting start and end dates. start date:{start}, current end date:{newEnd}", start, newEnd);

                await PaginateGitHubQuery(async endCursor =>
                {
                    var results = await GetSearchResults(repoFilter, start, newEnd, endCursor);
                    var search = results.data.search;
                    if (search.issueCount > GitHubConstants.GRAPHQL_QUERY_SEARCH_PAGINATION_LIMIT)
                        throw new ApplicationException($"Search results exceeded the limit of {GitHubConstants.GRAPHQL_QUERY_SEARCH_PAGINATION_LIMIT} records");

                    await ProcessSearchResults(repoWithOwner, results, lastPRNumber, useLastRun);
                    var lastNode = search.edges.LastOrDefault();
                    if (lastNode is not null)
                    {
                        lastPRNumber = lastNode.node.number;
                        lastRunAt = lastNode.node.createdAt;
                    }
                    if (useLastRun)
                        await PersistLastRunAt(lastRunAt, lastRunKey, lastPRNumber);
                        
                    return search.pageInfo;
                });

                _logger.LogInformation("Completed fetching PR's for the period: {Start} to {NewEnd}", start, newEnd);

                lastRunAt = newEnd;
                start = newEnd.AddSeconds(-1);
            } while (newEnd < end);
        }
        catch (Exception ex)
        {
            exception = ex;
            _logger.LogError(ex, "Error searching PR's for repository {Repo}", repoWithOwner);
            throw;
        }
        finally
        {
            // there is a risk of first time errors not being saved, but that's ignorable.
            await PersistLastRunAt(lastRunAt, lastRunKey, lastPRNumber, exception);
        }
    }

    /// <summary>
    /// Persists the last run at.
    /// </summary>
    /// <param name="lastRunAt"></param>
    /// <param name="lastRunKey"></param>
    /// <param name="lastPRNumber"></param>
    /// <param name="exception"></param>
    protected virtual async Task PersistLastRunAt(DateTime? lastRunAt, string lastRunKey, int? lastPRNumber, Exception? exception = null)
    {
        if (lastRunAt.HasValue)
        {
            await _persistenceService.UpdateLastRunAt(lastRunAt.Value, lastRunKey, lastPRNumber, exception);
        }
    }

    /// <summary>
    /// Gets the PR details.
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="repo"></param>
    /// <param name="prId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    protected async Task<GitHubPRRoot> GetPRDetailsAsync(string organization, string repo, int prId)
    {
        organization = organization ?? throw new ArgumentNullException(nameof(organization));
        repo = repo ?? throw new ArgumentNullException(nameof(repo));
        prId = int.IsNegative(prId) ? throw new ArgumentException($"PR id {prId} is invalid") : prId;

        var results = (GitHubPRRoot)await _apiClient.GetPaginatedPRDetailsAsync(organization, repo, prId);

        // check if we have more reviews, then query them again.
        if (results is not null)
        {
            var pr = results.data.repository.pullRequest;
            if (pr is null)
            {
                throw new ApplicationException($"PR Id {prId} not found on repo {repo}");
            }

            var reviews = pr.reviews;
            var commits = pr.commits;
            var timelines = pr.timelineItems;
            var comments = pr.comments;

            var (reviewHasNextPage, reviewEndCursor) = (reviews.pageInfo.hasNextPage, reviews.pageInfo.endCursor);
            var (commitHasNextPage, commitEndCursor) = (commits.pageInfo.hasNextPage, commits.pageInfo.endCursor);
            var (timelinesHasNextPage, timelinesEndCursor) = (timelines.pageInfo.hasNextPage, timelines.pageInfo.endCursor);
            var (commentsHasNextPage, commentsEndCursor) = (pr.comments.pageInfo.hasNextPage, pr.comments.pageInfo.endCursor);

            while (reviewHasNextPage || commitHasNextPage || timelinesHasNextPage || commentsHasNextPage)
            {
                var prPaginated = await _apiClient.GetPaginatedPRDetailsAsync(organization, repo, prId,
                    reviewEndCursor, commitEndCursor, timelinesEndCursor, commentsEndCursor,
                    reviewHasNextPage ? 100 : 0,
                    commitHasNextPage ? 100 : 0,
                    timelinesHasNextPage ? 100 : 0,
                    commentsHasNextPage ? 100 : 0);
                
                var paginatedPullRequest = prPaginated.data.repository.pullRequest;
                if (reviewHasNextPage)
                {
                    var paginatedReviews = paginatedPullRequest.reviews;
                    reviews.nodes.AddRange(paginatedReviews.nodes);
                    (reviewHasNextPage, reviewEndCursor) = (paginatedReviews.pageInfo.hasNextPage, paginatedReviews.pageInfo.endCursor);
                }
                if (commitHasNextPage)
                {
                    var paginatedCommits = paginatedPullRequest.commits;
                    commits.nodes.AddRange(paginatedCommits.nodes);
                    (commitHasNextPage, commitEndCursor) = (paginatedCommits.pageInfo.hasNextPage, paginatedCommits.pageInfo?.endCursor);
                }
                if (timelinesHasNextPage)
                {
                    var paginatedTimelines = paginatedPullRequest.timelineItems;
                    timelines.nodes.AddRange(paginatedTimelines.nodes);
                    (timelinesHasNextPage, timelinesEndCursor) = (paginatedTimelines.pageInfo.hasNextPage, paginatedTimelines.pageInfo?.endCursor);
                }
                if (commentsHasNextPage)
                {
                    var paginatedComments = paginatedPullRequest.comments;
                    comments.nodes.AddRange(paginatedComments.nodes);
                    (commentsHasNextPage, commentsEndCursor) = (paginatedComments.pageInfo.hasNextPage, paginatedComments.pageInfo?.endCursor);
                }
            }

            // sort reviews, commits and comments.
            reviews.nodes.Sort((x, y) => x.submittedAt?.CompareTo(y.submittedAt) ?? 0);
            commits.nodes.Sort((x, y) => x.commit.committedDate.CompareTo(y.commit.committedDate));
            timelines.nodes.Sort((x, y) => (x.createdAt ?? x.submittedAt).Value.CompareTo(y.createdAt ?? y.submittedAt));
            comments.nodes.Sort((x, y) => (x.createdAt).Value.CompareTo(y.createdAt));
        }
        return results;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    protected async Task PaginateGitHubQuery(Func<string, Task<PageInfo>> callback)
    {
        (bool hasNextPage, string endCursor) = (false, null);
        do
        {
            var pageInfo = await callback(endCursor);
            (hasNextPage, endCursor) = (pageInfo.hasNextPage, pageInfo.endCursor);
        } while (hasNextPage);
    }
}
