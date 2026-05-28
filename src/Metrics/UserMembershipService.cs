using Microsoft.Extensions.Logging;

namespace Metrics;

/// <summary>
/// Defines a service for managing user membership in teams, regions, and value streams.
/// This interface provides an extensible mechanism for mapping users to organizational structure
/// based on configuration settings and organizational data.
/// </summary>
public abstract class UserMembershipService<T> where T : Organization
{
    protected readonly SourceControlSettings<T> _settings;
    protected readonly ILogger _logger;
    readonly Dictionary<string, IReadOnlyList<string>> _vs2Teams;

    protected UserMembershipService(SourceControlSettings<T> settings, ILogger<UserMembershipService<T>> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _vs2Teams = _settings.Teams.SelectMany(t => t.Teams)
            .GroupBy(t => t.ValueStream)
            .Select(g => new { Name = g.Key, Teams = g.Select(k => k.Name) })
            .ToDictionary(k => k.Name, v => (IReadOnlyList<string>)v.Teams.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the list of teams associated with a specific value stream.
    /// This method retrieves teams that are mapped to the specified value stream from the configuration settings.
    /// If no teams are found for the given value stream, it returns an empty enumerable.
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="valueStream"/> is null or empty.
    /// </summary>
    public IEnumerable<string> GetTeamsByVS(string valueStream)
    {
        if (string.IsNullOrEmpty(valueStream))
            throw new ArgumentNullException(nameof(valueStream));

        return _vs2Teams.TryGetValue(valueStream, out var teams) ? teams : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets the list of product teams.
    /// </summary>
    /// <value></value>
    public IEnumerable<ProductTeams> ProductTeams => _settings.Teams;

    /// <summary>
    /// Resolves a user to their team information including team name, region, and value stream.
    /// </summary>
    /// <param name="currentOrg">The current organization context for resolution</param>
    /// <param name="user">The username to resolve</param>
    /// <param name="organizationServices">Dictionary of organization services for cross-org lookup</param>
    /// <returns>Team information for the user, or default team if not found</returns>
    public abstract Metrics.Models.Team ResolveUserToTeam(string currentOrg, string user, Dictionary<string, OrgUserService<T>> organizationServices);

    /// <summary>
    /// Tries to resolve a user to a specific team within an organization.
    /// </summary>
    /// <param name="userToFind">The username to find</param>
    /// <param name="orgUserService">The organization service to search within</param>
    /// <returns>Team information if found, null otherwise</returns>
    protected abstract bool TryResolveUserTeam(string userToFind, OrgUserService<T> orgUserService, out Metrics.Models.Team? team, bool useTeamNameAsFallback = false);
}