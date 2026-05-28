using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// GitHub specific api client.
/// </summary>
public class GitHubApiClient : ApiClient
{
    static string[] PR_Fragments = new[]
    {
        ResourceCache.Fragment_Comments,
        ResourceCache.Fragment_Commits,
        ResourceCache.Fragment_Reviews,
        ResourceCache.Fragment_TimelineItems
    };

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="clientFactory"></param>
    public GitHubApiClient(ILogger<GitHubApiClient> logger, HttpClient client)
        : base(logger, client)
    {
    }

    /// <summary>
    /// Executes a POST request with retries.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="typeInfo"></param>
    /// <param name="errorCallback"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    async Task<TResult> ExecutePostWithRetriesAsync<TResult>(string query, JsonTypeInfo<TResult> typeInfo, Func<TResult, bool> errorCallback = null) =>
        await ExecutePostWithRetriesAsync<TResult>(string.Empty, query, typeInfo, errorCallback);

    /// <summary>
    /// Gets the teams by pattern.
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="organization"></param>
    /// <returns></returns>
    public override async Task<TeamMembersData?> GetTeamsByPatternAsync(string organization, string pattern)
    {
        return await ExecuteAsync($"Get teams by pattern {pattern}", async () =>
        {
            var query = ResourceCache.GetValue(ResourceCache.TeamMembersQuery);
            string endCursor = string.Empty; bool hasNextPage = false;
            OrganizationRoot root = null;
            var owner = organization;
            do
            {
                var postBody = QueryBuilder.BuildTeamMembersQuery(owner, pattern, endCursor);
                var results = await ExecutePostAsync(string.Empty, postBody,
                    PostQueryContext.Default.PostWithVariablesPostTeamQueryVariables,
                    OrganizationRootContext.Default.OrganizationRoot,
                    orgRoot => orgRoot is null || orgRoot.data is null || orgRoot.data.organization is null);

                if (results is not null && results.data is not null)
                {
                    var teams = results.data.organization.teams;
                    if (root is null)
                        root = results;
                    else
                        root.data.organization.teams.nodes.AddRange(teams.nodes);

                    hasNextPage = teams.pageInfo.hasNextPage;
                    endCursor = teams.pageInfo.endCursor;
                }
            } while (hasNextPage);
            return root;
        });
    }

    /// <summary>
    /// Gets the paginated PR details.
    /// </summary>
    /// <param name="ownerOrOrg"></param>
    /// <param name="repo"></param>
    /// <param name="prId"></param>
    /// <param name="reviewsEndCursor"></param>
    /// <param name="commitsEndCursor"></param>
    /// <returns></returns>
    public async Task<GitHubPRRoot?> GetPaginatedPRDetailsAsync(string ownerOrOrg, string repo, int prId,
        string? reviewsEndCursor = null,
        string? commitsEndCursor = null,
        string? timelinesEndCursor = null,
        string? commentsEndCursor = null,
        int reviewsCount = 100,
        int commitsCount = 100,
        int timelinesCount = 100,
        int commentsCount = 100)
    {
        return await ExecuteAsync($"Get Paginated PRDetails for {prId}", async () =>
        {
            bool isFirst = string.IsNullOrEmpty(reviewsEndCursor) && string.IsNullOrEmpty(commitsEndCursor)
                && string.IsNullOrEmpty(timelinesEndCursor) && string.IsNullOrEmpty(commentsEndCursor);

            var postBody = QueryBuilder.BuildQuery(isFirst ? ResourceCache.PRQuery : ResourceCache.PRQuery_Reviews,
            new PostPRQueryPaginationVariables(ownerOrOrg, repo, prId, reviewsEndCursor, commitsEndCursor,
                timelinesEndCursor, commentsEndCursor, reviewsCount, commitsCount, timelinesCount, commentsCount),
                PR_Fragments);

            return await ExecutePostAsync(string.Empty, postBody,
                PostQueryContext.Default.PostWithVariablesPostPRQueryPaginationVariables,
                GitHubPRRootContext.Default.GitHubPRRoot,
                results => results is null || results.data is null || results.data.repository is null || results.data.repository.pullRequest is null);
        });
    }

    /// <summary>
    /// Gets the search results.
    /// </summary>
    /// <param name="repoFilter"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="endCursor"></param>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public override async Task<PRSearchData> GetSearchResultsAsync(string repoFilter, DateTime start, DateTime end, string endCursor)
    {
        return await ExecuteAsync($"Get search results for {repoFilter}", async () =>
        {
            var query = string.Format(ResourceCache.GetValue(ResourceCache.SearchPRQuery), repoFilter, start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"), end.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                        endCursor == null ? "null" : $"\"{endCursor}\"");

            return await ExecutePostWithRetriesAsync<SearchRoot>(query,
                    SearchRootContext.Default.SearchRoot,
                    results => results is null || results.data is null || results.data.search is null);
        });
    }
}
