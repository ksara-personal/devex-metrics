using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// GitHub metrics synchronizer.
/// </summary>
public class GitHubPRMetricsSynchronizer : GitHubSynchronizer
{
    readonly PRAnalyzer<GitHubPRRoot> _analyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubPRMetricsSynchronizer"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="apiClient"></param>
    /// <param name="tenantConfig"></param>
    /// <param name="analyzer"></param>
    /// <param name="persistenceService"></param>
    /// <returns></returns>
    public GitHubPRMetricsSynchronizer(ILogger<GitHubPRMetricsSynchronizer> logger,
        GitHubApiClient apiClient,
        ITenantSettingsProvider tenantConfig,
        PRAnalyzer<GitHubPRRoot> analyzer,
        IMetricsPersistenceService persistenceService) : base(logger, apiClient, tenantConfig, persistenceService)
    {
        _analyzer = analyzer;
    }

    /// <summary>
    /// Updates the metrics for open PR's whose states have changed.
    /// </summary>
    /// <returns></returns>
    public override async Task<int> UpdateMetricsAsync()
    {
        int recordsAffected = 0;
        await foreach (var metrics in _persistenceService.GetAllPRsByStateAsync(pageSize: 100))
        {
            // check if the states have changed.
            foreach (var grp in metrics.GroupBy(m => m.Repository))
            {
                _logger.LogInformation("Checking PR's in repo {Repo} for state changes", grp.Key);
                List<PRMetrics> prsChanged = new();
                var (owner, repo) = grp.Key.GetOwnerAndRepoNames();
                var prDict = grp.ToDictionary(k => k.PrNumber, v => v);
                var postBody = QueryBuilder.BuildSearchQuery(owner, repo, prDict.Keys);
                var searchResults = await _apiClient.ExecutePostAsync(string.Empty,
                    postBody,
                    PostQueryContext.Default.PostWithVariablesPostVariables,
                    SearchStateRootContext.Default.SearchStateRoot,
                    root => root?.data is null);
                
                foreach (var result in searchResults.data)
                {
                    if (result.Value?.pullRequest == null)
                    {
                        _logger.LogWarning("pullRequest is null for a result in repo {Repo}", repo);
                        continue;
                    }
                    var pr = result.Value.pullRequest;
                    if (prDict.TryGetValue(pr.number, out var item)
                        && (!string.Equals(item.State, pr.state, StringComparison.OrdinalIgnoreCase)
                        || !item.UpdatedAt.HasValue || (pr.updatedAt.HasValue && pr.updatedAt.Value > item.UpdatedAt.Value)
                        ))
                    {
                        prsChanged.Add(item);
                    }
                }

                if (prsChanged.Count > 0)
                {
                    foreach (var items in prsChanged.Chunk(20))
                    {
                        _logger.LogInformation("Updating {Items} records as they have their states changed", items.Length);
                        var newMetrics = await Task.WhenAll(items.Select(pr => GetPRMetricsAsync(owner, repo, pr.PrNumber)));
                        recordsAffected += await _persistenceService.UpdatePRsAsync(newMetrics);
                    }
                }
            }
        }
        return recordsAffected;
    }

    /// <summary>
    /// Formats the run status key for the specified repository.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <returns></returns>
    protected override string FormatRunStatusKey(string repoWithOwner) => repoWithOwner;

    /// <summary>
    /// Writes the metrics for the specified date range.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public override async Task WriteMetricsAsync(DateTime? startDate = null, DateTime? endDate = null) =>
        await EnumerateOrgsAndWriteMetricsAsync(startDate, endDate.HasValue ? endDate.Value : DateTime.UtcNow);

    /// <summary>
    /// Processes the search results for the specified repository.
    /// </summary>
    /// <param name="repoWithOwner"></param>
    /// <param name="results"></param>
    /// <param name="lastPRNumber"></param>
    /// <returns></returns>
    protected override async Task ProcessSearchResults(string repoWithOwner, SearchRoot results, int? lastPRNumber, bool useLastRun)
    {
        var search = results.data.search;
        IEnumerable<Edge> filteredEdges = search.edges.Where(e => !ExcludeBranch(e.node.baseRefName)).ToList();
        if (useLastRun)
        {
            var newUrls = await _persistenceService.GetPRUrlsNotExists(filteredEdges.Select(e => e.node.repository.owner.login + "/" + e.node.repository.name + "|" + e.node.number));
            filteredEdges = filteredEdges.Where(e => newUrls.Contains(e.node.repository.owner.login + "/" + e.node.repository.name + "|" + e.node.number));
            filteredEdges = lastPRNumber.HasValue ? filteredEdges.Where(e => e.node.number > lastPRNumber.Value) : filteredEdges;
        }

        if (filteredEdges.Any())
        {
            _logger.LogInformation("Found total {IssueCount} PR's, paginating it, current count: {EdgeCount}", search.issueCount, search.edges.Count);
            var asyncResults = filteredEdges.RunParallelTasks(edge => GetPRMetricsAsync(edge.node));

            await ProcessPRMetrics(asyncResults);
        }
    }
    
    /// <summary>
    /// Processes the PR metrics.
    /// </summary>
    /// <param name="asyncResults"></param>
    /// <returns></returns>
    protected virtual async Task ProcessPRMetrics(IAsyncEnumerable<IEnumerable<PRMetrics>> asyncResults)
    {
        await foreach (var result in asyncResults)
        {
            var recordsAffected = await _persistenceService.InsertIfNotExistsRangeAsync(result);
            _logger.LogInformation("Total records affected:{recordsAffected}", recordsAffected);
        }
    }

    /// <summary>
    /// Gets the search results for the specified repository.
    /// </summary>
    /// <param name="repoFilter"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="endCursor"></param>
    /// <returns></returns>
    protected override async Task<SearchRoot> GetSearchResults(string repoFilter, DateTime start, DateTime end, string? endCursor)
        => (SearchRoot)await _apiClient.GetSearchResultsAsync(repoFilter, start, end, endCursor);

    /// <summary>
    /// Determines whether to exclude the branch.
    /// </summary>
    /// <param name="branchName"></param>
    /// <returns></returns> <summary>
    bool ExcludeBranch(string branchName)
    {
        var branches = _settings.ExcludeBranchPrefixes;
        if (branches != null && branches.Count > 0)
        {
            return branches.Any(pattern => branchName.StartsWith(pattern));
        }
        return false;
    }

    /// <summary>
    /// Gets the PR metrics instance.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    protected async Task<PRMetrics> GetPRMetricsAsync(SearchNode node)
    {
        var repo = node.repository;
        return await GetPRMetricsAsync(repo.owner.login, repo.name, node.number);
    }

    /// <summary>
    /// Gets the PR metrics
    /// </summary>
    /// <param name="ownerOrOrg"></param>
    /// <param name="repo"></param>
    /// <param name="prNumber"></param>
    /// <returns></returns>
    public async Task<PRMetrics> GetPRMetricsAsync(string ownerOrOrg, string repo, int id) => _analyzer.CalcPRMetrics(await GetPRDetailsAsync(ownerOrOrg, repo, id));
}
