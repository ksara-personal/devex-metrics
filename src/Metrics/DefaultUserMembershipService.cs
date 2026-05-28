using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Metrics;

/// <summary>
/// Default implementation of user membership service that provides user-to-team mapping functionality
/// for development metrics collection. This service resolves users to their teams, regions, and value streams
/// based on source control settings and organizational configurations.
/// </summary>
/// <typeparam name="T">The organization type that extends <see cref="Organization"/></typeparam>
public sealed class DefaultUserMembershipService<T> : UserMembershipService<T> where T : Organization
{
    readonly Dictionary<string, global::Metrics.Models.Team> _teamInfoDict;

    internal static readonly global::Metrics.Models.Team _ghostTeam = new global::Metrics.Models.Team { Name = "ghost", Region = "ghost", ValueStream = "ghost" };

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserMembershipService{T}"/> class.
    /// Builds the team information dictionary from the tenant-specific source control settings.
    /// </summary>
    /// <param name="tenantConfig">Tenant configuration service for resolving tenant-specific settings</param>
    /// <param name="logger">Logger instance for capturing resolution activities and troubleshooting</param>
    public DefaultUserMembershipService(TenantConfigurationService tenantConfig, ILogger<DefaultUserMembershipService<T>> logger)
        : base(ResolveSettings(tenantConfig), logger)
    {
        _teamInfoDict = (from t in _settings.Teams
                         from pt in t.Teams
                         select (pt.RemoteName, pt.Name, pt.ValueStream, t.Region))
                        .ToDictionary(k => string.IsNullOrEmpty(k.RemoteName) ? k.Name : k.RemoteName, v => new global::Metrics.Models.Team { Name = v.Name, RemoteName = v.RemoteName, Region = v.Region, ValueStream = v.ValueStream }, StringComparer.OrdinalIgnoreCase);
    }

    static SourceControlSettings<T> ResolveSettings(TenantConfigurationService tenantConfig)
    {
        var sectionName = typeof(T).Name.Replace("Organization", "");
        return tenantConfig.GetSettings<SourceControlSettings<T>>(sectionName)
            ?? throw new InvalidOperationException($"Source control settings not configured for {typeof(T).Name}");
    }

    /// <summary>
    /// Resolves a user to their team information including team name, region, and value stream.
    /// This method performs a comprehensive search across all configured organizations to find
    /// the user's team membership, with fallback mechanisms for unresolvable users.
    /// </summary>
    /// <param name="currentOrg">The current organization context for fallback resolution when user is not found in any organization</param>
    /// <param name="user">The username/login to resolve to team information</param>
    /// <param name="organizationServices">Dictionary of organization services for cross-organizational user lookup</param>
    /// <returns>
    /// A <see cref="Team"/> object containing the user's team information, or default team information if not found.
    /// Never returns null - always provides fallback team information.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="user"/> or <paramref name="currentOrg"/> is null or empty
    /// </exception>
    public override global::Metrics.Models.Team ResolveUserToTeam(string currentOrg, string user, Dictionary<string, OrgUserService<T>> organizationServices)
    {
        if (string.IsNullOrEmpty(user))
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrEmpty(currentOrg))
            throw new ArgumentNullException(nameof(currentOrg));

        bool found = false;
        global::Metrics.Models.Team? teamInfo = null;
        var userteamMappings = _settings.UserTeamMappings;
        if (userteamMappings is not null && userteamMappings.TryGetValue(user, out var teamName) && !string.IsNullOrEmpty(teamName))
        {
            _teamInfoDict.TryGetValue(teamName, out teamInfo);
        }

        if (teamInfo is null)
        {
            foreach (var org in organizationServices.Values)
            {
                if (found = TryResolveUserTeam(user, org, out teamInfo))
                {
                    break;
                }
            }

            if (!found)
            {
                if (organizationServices.TryGetValue(currentOrg, out var orgUserService) && orgUserService is not null)
                {
                    if (!TryResolveUserTeam(user, orgUserService, out teamInfo, true))
                    {
                        // Final fallback: use organization defaults
                        var org = orgUserService.Organization;
                        teamInfo = new global::Metrics.Models.Team
                        {
                            Name = org.DefaultTeam,
                            Region = org.DefaultRegion,
                            ValueStream = org.DefaultTeam
                        };
                    }
                }
            }
        }
        return teamInfo ?? _ghostTeam;
    }

    /// <summary>
    /// Attempts to resolve a user to a specific team within an organization using the configured team mappings.
    /// This method looks up the user's team name through the organization service and then maps it to
    /// detailed team information from the configuration.
    /// </summary>
    /// <param name="userToFind">The username/login to search for within the organization</param>
    /// <param name="orgUserService">The organization-specific user service to query for team membership</param>
    /// <param name="team">
    /// When this method returns, contains the team information if the user was found, 
    /// or a new empty team instance if not found. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the user was successfully resolved to a team; otherwise, <c>false</c>.
    /// When <c>true</c>, the <paramref name="team"/> parameter contains valid team information.
    /// When <c>false</c>, the <paramref name="team"/> parameter contains an empty team instance.
    /// </returns>
    protected override bool TryResolveUserTeam(string userToFind, OrgUserService<T> orgUserService, out global::Metrics.Models.Team? team, bool useTeamNameAsFallback = false)
    {
        var teams = orgUserService.ResolveUser2Team(userToFind);
        bool found = false;
        team = new();

        if (teams is not null && teams.Count > 0)
        {
            var org = orgUserService.Organization;
            foreach (var teamName in teams)
            {
                if (_teamInfoDict.TryGetValue(teamName, out team))
                {
                    team.Name ??= teamName ?? org.DefaultTeam;
                    team.Region ??= org.DefaultRegion;
                    team.ValueStream ??= org.DefaultTeam;
                    found = true;
                }
                if (found)
                    break;
            }
            if (useTeamNameAsFallback && team is null)
            {
                team = new global::Metrics.Models.Team
                {
                    Name = teams.Last() ?? org.DefaultTeam,
                    Region = org.DefaultRegion,
                    ValueStream = org.DefaultTeam
                };
                found = true;
            }
        }
        return found;
    }
}
