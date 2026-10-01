namespace Metrics.Domain;

/// <summary>
/// Represents a region-specific team configuration for development metrics collection.
/// Contains regional settings and team information for a specific geographic area.
/// </summary>
/// <param name="Region">The geographic region name (e.g., "North America", "Europe")</param>
/// <param name="TimeZone">The time zone identifier for the region (e.g., "UTC", "PST")</param>
/// <param name="Teams">A read-only list of product teams operating within this region</param>
public record ProductTeams(string Region, string TimeZone, IReadOnlyList<ProductTeam> Teams);

/// <summary>
/// Represents an individual product team within an organization.
/// Contains team identification and organizational structure information for metrics tracking.
/// </summary>
/// <param name="Name">The display name of the team (e.g., "Frontend Team", "API Team")</param>
/// <param name="RemoteName">The team identifier used in remote systems like GitHub or Azure DevOps</param>
/// <param name="ValueStream">The value stream this team belongs to (e.g., "Platform", "Customer Experience")</param>
public record ProductTeam(string Name, string RemoteName, string ValueStream);

/// <summary>
/// Abstract base settings class for source control providers (GitHub, Azure DevOps, etc.).
/// Provides common configuration properties shared across different source control systems.
/// </summary>
/// <typeparam name="TOrg">The organization type that extends <see cref="Organization"/></typeparam>
public sealed record SourceControlSettings<TOrg> where TOrg : Organization
{
    /// <summary>
    /// Gets or sets the list of product teams organized by region.
    /// Contains team configuration and regional information for metrics collection.
    /// </summary>
    public IReadOnlyList<ProductTeams>? Teams { get; set; }

    /// <summary>
    /// Gets or sets the Personal Access Token (PAT) for authenticating with the source control provider.
    /// This token must have appropriate permissions for reading repository and organization data.
    /// </summary>
    public required string PAT { get; set; }

    /// <summary>
    /// Gets or sets the list of organizations to monitor for metrics collection.
    /// Each organization contains repositories and teams to track.
    /// </summary>
    public IReadOnlyList<TOrg>? Organizations { get; set; }

    /// <summary>
    /// User team mapping
    /// </summary>
    /// <value></value>
    public Dictionary<string, string>? UserTeamMappings { get; set; }

    /// <summary>
    /// Gets or sets the list of branch prefixes to exclude from metrics collection.
    /// Branches with these prefixes will be ignored in processing.
    /// </summary>
    /// <value></value>
    public IReadOnlyList<string> ExcludeBranchPrefixes { get; set; }    
}

/// <summary>
/// Represents a source control organization (GitHub or Azure DevOps) configuration.
/// Contains the fundamental settings needed to connect to and collect metrics from an organization.
/// This is the base class for platform-specific organization settings.
/// </summary>
public abstract record Organization
{
    /// <summary>
    /// Gets or sets the organization owner name.
    /// For GitHub, this is the organization name in the URL (e.g., "microsoft" in github.com/microsoft).
    /// For Azure DevOps, this is the organization name in the URL.
    /// </summary>
    public required string Owner { get; set; }

    /// <summary>
    /// Gets or sets the list of repository names to monitor within this organization.
    /// Only repositories in this list will be included in metrics collection.
    /// </summary>
    public IList<string> Repositories { get; set; }

    /// <summary>
    /// Gets or sets the list of team names that are designated as "Code Excellence" teams.
    /// These teams receive special treatment in metrics calculations and reporting.
    /// </summary>
    public IReadOnlyList<string> CodeExcellenceTeams { get; set; }

    /// <summary>
    /// Gets or sets the list of regex patterns used to identify teams to include in metrics.
    /// Teams whose names match any of these patterns will be included in processing.
    /// </summary>
    public IReadOnlyList<string> IncludeTeamsWithNamePattern { get; set; }

    /// <summary>
    /// Gets or sets the default team name to assign when a user cannot be mapped to a specific team.
    /// This ensures all metrics have a team assignment for proper categorization.
    /// </summary>
    public required string DefaultTeam { get; set; }

    /// <summary>
    /// Gets or sets the default region to assign when a user cannot be mapped to a specific region.
    /// This ensures all metrics have a regional assignment for proper categorization.
    /// </summary>
    public required string DefaultRegion { get; set; }
}

/// <summary>
/// Represents a GitHub organization configuration with GitHub-specific settings.
/// Extends the base <see cref="Organization"/> class with GitHub-specific filtering options.
/// </summary>
public sealed record GitHubOrganization : Organization
{
    /// <summary>
    /// Gets or sets the bot name whose reviews should be ignored in metrics calculations.
    /// This helps filter out automated reviews that might skew developer productivity metrics.
    /// </summary>
    public string? IgnoreReviewsFromBotName { get; set; }

    /// <summary>
    /// Gets or sets the bot name whose comments should be ignored in metrics calculations.
    /// This helps filter out automated comments that might skew communication metrics.
    /// </summary>
    public string? IgnoreCommentsFromBotName { get; set; }
}

/// <summary>
/// Represents the settings for the sprint calendar.
/// </summary>
/// <value></value>
public sealed record SprintCalendarSettings
{
    public required DateTime EarliestKnownSprintStartDate { get; set; }
    public required int EarliestKnownSprintNumber { get; set; }
    public required string EarliestKnownReleaseName { get; set; }
    public DateTime? GenerateSprintsUptoDate { get; set; }
    public IReadOnlyList<KnownSprintReleaseName>? KnownSprintReleaseNames { get; set; }
}

/// <summary>
/// Represents a known sprint release name mapping.
/// Contains the sprint number, year, and corresponding release name for reference.
/// </summary>
public sealed record KnownSprintReleaseName
{
    public required int SprintNumber { get; set; }
    public required int Year { get; set; }
    public required string ReleaseName { get; set; }
}