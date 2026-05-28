using System.ComponentModel;
using System.Text.Json;
using Metrics.ADO.EF;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace Metrics.ADO.MCP;

/// <summary>
/// MCP server tool for Azure DevOps work item metrics operations.
/// Provides methods to get work item metrics by team or release version.
/// </summary>
[McpServerToolType]
public sealed class ADOWorkItemMetricsTool
{
    readonly ADOMetricsDbContext _dbContext;
    readonly ILogger<ADOWorkItemMetricsTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADOWorkItemMetricsTool"/> class.
    /// </summary>
    /// <param name="dbContext">The ADO metrics database context.</param>
    /// <param name="logger">The logger instance.</param>
    public ADOWorkItemMetricsTool(ADOMetricsDbContext dbContext, ILogger<ADOWorkItemMetricsTool> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets work item metrics for a specific team.
    /// </summary>
    /// <param name="team">The team name to filter work item metrics.</param>
    /// <returns>Serialized list of work item metrics as JSON.</returns>
    [McpServerTool, Description("Gets epic metrics for a specific team")]
    public async Task<string> GetEpicMetricsByTeam([Description("The team name to filter work item metrics")] string team)
    {
        if (string.IsNullOrWhiteSpace(team))
            throw new ArgumentException("Team name cannot be null or empty", nameof(team));

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.Team == team)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} work item metrics for team {Team}", metrics.Count, team);

        return JsonSerializer.Serialize(metrics, WorkItemMetricsJsonContext.Default.IEnumerableWorkItemMetrics);
    }

    /// <summary>
    /// Gets work item metrics for a specific release version.
    /// </summary>
    /// <param name="releaseVersion">The release version to filter work item metrics.</param>
    /// <returns>Serialized list of work item metrics as JSON.</returns>
    [McpServerTool, Description("Gets epic metrics for a specific release version")]
    public async Task<string> GetEpicMetricsByReleaseVersion(
        [Description("The release version to filter work item metrics")] string releaseVersion)
    {
        if (string.IsNullOrWhiteSpace(releaseVersion))
            throw new ArgumentException("Release version cannot be null or empty", nameof(releaseVersion));

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.ReleaseVersion == releaseVersion)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} work item metrics for release version {ReleaseVersion}", 
            metrics.Count, releaseVersion);

        return JsonSerializer.Serialize(metrics, WorkItemMetricsJsonContext.Default.IEnumerableWorkItemMetrics);
    }

    /// <summary>
    /// Gets work item metrics for a specific team and release version.
    /// </summary>
    /// <param name="team">The team name to filter work item metrics.</param>
    /// <param name="releaseVersion">The release version to filter work item metrics.</param>
    /// <returns>Serialized list of work item metrics as JSON.</returns>
    [McpServerTool, Description("Gets epic metrics for a specific team and release version")]
    public async Task<string> GetEpicMetricsByTeamAndReleaseVersion(
        [Description("The team name to filter work item metrics")] string team,
        [Description("The release version to filter work item metrics")] string releaseVersion)
    {
        if (string.IsNullOrWhiteSpace(team))
            throw new ArgumentException("Team name cannot be null or empty", nameof(team));
        
        if (string.IsNullOrWhiteSpace(releaseVersion))
            throw new ArgumentException("Release version cannot be null or empty", nameof(releaseVersion));

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.Team == team && m.ReleaseVersion == releaseVersion)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} work item metrics for team {Team} and release version {ReleaseVersion}", 
            metrics.Count, team, releaseVersion);

        return JsonSerializer.Serialize(metrics, WorkItemMetricsJsonContext.Default.IEnumerableWorkItemMetrics);
    }
}
