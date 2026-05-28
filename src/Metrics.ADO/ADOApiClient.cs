using System;

namespace Metrics.ADO;

/// <summary>
/// ADO API Client
/// </summary>
public sealed class ADOApiClient : ApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ADOApiClient"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="client">The HTTP client instance.
    /// </param>
    /// <returns></returns>
    public ADOApiClient(ILogger<ApiClient> logger, HttpClient client) : base(logger, client)
    {
    }

    /// <summary>
    /// Get search results
    /// </summary>
    /// <param name="repoFilter"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="endCursor"></param>
    /// <returns></returns> <summary>
    public override Task<PRSearchData> GetSearchResultsAsync(string repoFilter, DateTime start, DateTime end, string endCursor)
        => Task.FromResult(null as PRSearchData);

    /// <summary>
    /// Get teams by pattern
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public override Task<TeamMembersData> GetTeamsByPatternAsync(string organization, string pattern)
        => Task.FromResult(null as TeamMembersData);
}
