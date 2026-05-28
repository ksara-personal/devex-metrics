using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace Metrics.MCP;

/// <summary>
/// MetricsTool class provides various methods to retrieve metrics related to GitHub pull requests.
/// It includes methods to get metrics by team, author, and other criteria, as well as
/// methods to retrieve team names, code excellence reviewers, and product teams.
/// </summary>
[McpServerToolType]
public sealed class MetricsTool : Metrics.MCP.ToolBase
{
    readonly UserMembershipService<GitHubOrganization> _userMembershipService;
    
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="connector"></param>
    public MetricsTool([FromServices] DataClient client,
        [FromServices] UserMembershipService<GitHubOrganization> userMembershipService)
        : base(client)
    {
        _userMembershipService = userMembershipService;
    }

    /// <summary>
    /// Gets the work items details.
    /// </summary>
    /// <param name="workItemId"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics for the given team name within the specified date range")]
    public async Task<string> GetMetricsByTeamAsync(
        [Description("The name of the team to fetch metrics for")] string team,
        [Description("The start date for the metrics query in ISO 8601 format")] DateTime start,
        [Description("The end date for the metrics query in ISO 8601 format")] DateTime end) =>
        await GetMetricsAsync(start, end, (s, e) => _client.QueryMetricsByTeamAsync(team, s, e), PRMetricsContext.Default.IEnumerablePRMetrics);

    /// <summary>
    /// Gets the author specific metrics.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics for the given author login or email within the specified date range")]
    public async Task<string> GetMetricsByAuthorLoginOrEmailAsync(
        [Description("The login or email of the author to fetch metrics for")] string authorLoginOrEmail,
        [Description("The start date for the metrics query in ISO 8601 format")] DateTime start,
        [Description("The end date for the metrics query in ISO 8601 format")] DateTime end) =>
        await GetMetricsAsync(start, end, (s, e) => _client.QueryMetricsByAuthorAsync(authorLoginOrEmail, s, e), PRMetricsContext.Default.IEnumerablePRMetrics);

    /// <summary>
    /// Gets the metrics by author name for the given duration.
    /// </summary>
    /// <param name="authorName"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics for the given author name within the specified date range")]
    public async Task<string> GetMetricsByAuthorNameAsync(
        [Description("The name of the author to fetch metrics for")] string authorName,
        [Description("The start date for the metrics query in ISO 8601 format")] DateTime start,
        [Description("The end date for the metrics query in ISO 8601 format")] DateTime end) =>
        await GetMetricsAsync(start, end, (s, e) => _client.QueryMetricsByAuthorAsync(authorName, s, e), PRMetricsContext.Default.IEnumerablePRMetrics);

    /// <summary>
    /// Gets the pr metrics for the given pr number or number using the repo specified
    /// </summary>
    /// <param name="prNumber"></param>
    /// <param name="repo"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics for the given pull request number within the specified repository")]
    public async Task<string> GetMetricsByIdAsync(
        [Description("The number of the pull request to fetch metrics for")] int prNumber,
        [Description("The repository to fetch metrics from")] string repo) =>
        JsonSerializer.Serialize(await _client.QueryMetricsByIdAsync(repo, prNumber), PRMetricsContext.Default.PRMetrics);

    /// <summary>
    /// Gets the team names by the given value stream.
    /// </summary>
    /// <param name="valueStream"></param>
    /// <returns></returns>
    [McpServerTool, Description("Gets the team names for the given value stream")]
    public string GetTeamsByValueStream(string valueStream) => JsonSerializer.Serialize(_userMembershipService.GetTeamsByVS( valueStream ), EnumerableJsonContext.Default.IEnumerableString);

    /// <summary>
    /// Gets all PR's that are rewviewed by the copilot reviewer and have statistics available
    /// This method retrieves all pull requests that have been reviewed by the copilot reviewer and have
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the copilot review metrics for the given team within the specified date range")]
    public async Task<string> GetCopilotReviewMetricsByTeamAsync(
        [Description("The start date for the metrics query in ISO 8601 format")] DateTime start,
        [Description("The end date for the metrics query in ISO 8601 format")] DateTime end,
        [Description("The name of the team to fetch metrics for")] string team) =>
        await GetMetricsAsync(start, end, (s, e) => _client.QueryPRsMetricsByCopilotReviewerAsync(s, e, team), PRMetricsContext.Default.IEnumerablePRMetrics);

    /// <summary>
    /// Gets the copilot review summary for the teams.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the copilot review metrics summary for all teams within the specified date range")]
    public async Task<string> GetCopilotReviewMetricsForAllTeamsAsync(
        [Description("The start date for the metrics query in ISO 8601 format")] DateTime start,
        [Description("The end date for the metrics query in ISO 8601 format")] DateTime end) =>
        await GetMetricsAsync(start, end, (s, e) => _client.QueryCopilotReviewerSummaryForAllTeamsAsync(s, e), CopilotReviewSummaryJsonContext.Default.IEnumerableCopilotReviewSummary);
}
