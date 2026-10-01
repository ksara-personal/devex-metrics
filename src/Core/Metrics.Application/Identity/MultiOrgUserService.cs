using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Application;

/// <summary>
/// Abstract service class that provides team and user resolution functionality across multiple organizations for development metrics.
/// This service handles mapping between users, teams, regions, and value streams across GitHub organizations,
/// as well as determining reviewer roles and permissions.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class MultiOrgUserService<T> where T : Organization
{
    readonly SourceControlSettings<T> _settings;
    readonly IServiceProvider _serviceProvider;
    readonly UserMembershipService<T> _userMembershipService;
    readonly Dictionary<string, OrgUserService<T>> _organizationServices = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiOrgUserService{T}"/> class.
    /// </summary>
    /// <param name="tenantConfig">Tenant configuration service for resolving tenant-specific settings</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="userMembershipService">Service for resolving users to teams</param>
    public MultiOrgUserService(ITenantSettingsProvider tenantConfig, IServiceProvider serviceProvider, UserMembershipService<T> userMembershipService)
    {
        ArgumentNullException.ThrowIfNull(tenantConfig);
        var sectionName = typeof(T).Name.Replace("Organization", "");
        _settings = tenantConfig.GetSettings<SourceControlSettings<T>>(sectionName)
            ?? throw new InvalidOperationException($"Source control settings not configured for {typeof(T).Name}");
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userMembershipService = userMembershipService ?? throw new ArgumentNullException(nameof(userMembershipService));

        foreach (var org in _settings.Organizations)
        {
            ValidateOrganization(org);
            var orgUserService = _serviceProvider.GetRequiredService<OrgUserService<T>>();
            orgUserService.Initialize(org);
            _organizationServices[org.Owner] = orgUserService;
        }
    }

    /// <summary>
    /// Validates the organization configuration to ensure it contains all required fields.
    /// This method checks for the presence of the organization owner, default team, and default region
    /// in the provided organization settings. If any of these fields are missing or empty,
    /// it throws an ArgumentOutOfRangeException with a descriptive message.
    /// </summary>
    /// <param name="org"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <summary>
    void ValidateOrganization(T org)
    {
        if (org is null)
            throw new ArgumentOutOfRangeException(nameof(org), "Organization is not configured");

        if (string.IsNullOrWhiteSpace(org.Owner))
            throw new ArgumentOutOfRangeException(nameof(org.Owner), "Organization owner is not specified");

        if (string.IsNullOrWhiteSpace(org.DefaultTeam))
            throw new ArgumentOutOfRangeException(nameof(org.DefaultTeam), "Default team is not specified");

        if (string.IsNullOrWhiteSpace(org.DefaultRegion))
            throw new ArgumentOutOfRangeException(nameof(org.DefaultRegion), "Default region is not specified");
    }

    /// <summary>
    /// Invalidates the organization service user cache for all configured organizations.
    /// This method forces a re-initialization of the user cache for each organization service,
    /// ensuring that any changes in user memberships or team configurations are reflected
    /// in subsequent queries. This is useful when user data may have changed outside of the service
    /// </summary>
    internal void InvalidateOrganizationServiceUserCache()
    {
        foreach (var orgUserService in _organizationServices.Values)
        {
            orgUserService.InvalidateUserCache();
        }
    }

    /// <summary>
    /// Gets the organization-specific user service for the specified GitHub organization.
    /// This method provides access to organization-specific functionality and data.
    /// </summary>
    /// <param name="organization">The GitHub organization name to get the service for</param>
    /// <returns>The organization-specific user service instance</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified organization is not found in the configured GitHub settings
    /// </exception>
    public OrgUserService<T> GetOrgUserService(string organization)
    {
        if (string.IsNullOrEmpty(organization))
            throw new ArgumentNullException(nameof(organization));

        if (_organizationServices.TryGetValue(organization, out var service))
        {
            return service;
        }
        throw new ArgumentException($"Organization '{organization}' not found in GitHub settings.");
    }

    /// <summary>
    /// Determines whether a specified reviewer belongs to a specific team within a GitHub organization.
    /// </summary>
    /// <param name="organization">The GitHub organization name (e.g., "your-github-org")</param>
    /// <param name="team">The team name to check membership for</param>
    /// <param name="reviewer">The reviewer's GitHub username</param>
    /// <returns>
    /// <c>true</c> if the reviewer is a member of the specified team; otherwise, <c>false</c>
    /// </returns>
    public bool IsTeamReviewer(string organization, global::Metrics.Domain.Team team, string reviewer) =>
        GetOrgUserService(organization).IsTeamReviewer(team, reviewer);

    /// <summary>
    /// Determines whether a specified reviewer has Code Excellence (CE) reviewer privileges 
    /// within a GitHub organization.
    /// </summary>
    /// <param name="organization">The GitHub organization name (e.g., "your-github-org")</param>
    /// <param name="reviewer">The reviewer's GitHub username</param>
    /// <returns>
    /// <c>true</c> if the reviewer has Code Excellence reviewer privileges; otherwise, <c>false</c>
    /// </returns>
    public bool IsCEReviewer(string organization, string reviewer) =>
        GetOrgUserService(organization).IsCEReviewer(reviewer);

    /// <summary>
    /// Resolves a user's display name or email address to their GitHub login username 
    /// within a specific organization.
    /// </summary>
    /// <param name="organization">The GitHub organization name (e.g., "your-github-org")</param>
    /// <param name="nameOrEmail">The user's display name, full name, or email address</param>
    /// <returns>
    /// The user's GitHub login username if found; otherwise, the original input or <c>null</c> if not resolvable
    /// </returns>
    public string Resolve2UserLogin(string organization, string nameOrEmail) =>
        GetOrgUserService(organization).Resolve2UserLogin(nameOrEmail);

    /// <summary>
    /// Gets the team members for the user.
    /// </summary>
    /// <param name="currentOrg">The current organization context</param>
    /// <param name="user">The user to resolve to a team</param>
    /// <returns>Team information including name, region, and value stream</returns>
    /// <exception cref="ArgumentNullException">Thrown when user parameter is null</exception>
    public Team ResolveUser2TeamAndVS(string currentOrg, string user) =>
        _userMembershipService.ResolveUserToTeam(currentOrg, user, _organizationServices);
}
