using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// GitHub organization user service.
/// </summary>
public sealed class GitHubOrgUserService : OrgUserService<GitHubOrganization>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubOrgUserService"/> class.
    /// </summary>
    /// <param name="apiClient"></param>
    /// <param name="logger"></param>
    public GitHubOrgUserService(GitHubApiClient apiClient, ILogger<GitHubOrgUserService> logger)
        : base(apiClient, logger)
    {
    }

    /// <summary>
    /// Adds or updates the team members.
    /// </summary>
    /// <param name="teamMembersData"></param>
    /// <returns></returns>
    protected override async Task AddOrUpdateTeamMembersAsync(TeamMembersData teamMembersData)
    {
        if (teamMembersData is null)
            throw new ArgumentNullException(nameof(teamMembersData));

        OrganizationRoot? results = (OrganizationRoot)teamMembersData;
        foreach (var team in results.data.organization?.teams?.nodes)
        {
            await InternalAddOrUpdateTeamMembers<TeamsNode>(
                team.slug,
                () => team.members?.nodes,
                item => (item.login, item.name, item.email));
        }
    }
}
