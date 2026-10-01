namespace Metrics.Application;

/// <summary>
/// Port for the source-control API calls that organisation and team resolution depends on.
/// </summary>
/// <remarks>
/// Implemented by the HTTP clients in the infrastructure layer (see <c>ApiClient</c> and its
/// provider-specific subclasses). Application code depends on this contract so that team
/// resolution stays free of any HTTP or provider concern.
/// </remarks>
public interface IOrganizationApiClient
{
    /// <summary>
    /// Gets the teams of an organisation whose names match the supplied pattern.
    /// </summary>
    Task<TeamMembersData> GetTeamsByPatternAsync(string organization, string pattern);

    /// <summary>
    /// Searches pull requests changed within the supplied window.
    /// </summary>
    Task<PRSearchData> GetSearchResultsAsync(string repoFilter, DateTime start, DateTime end, string endCursor);
}
