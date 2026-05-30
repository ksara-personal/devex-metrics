using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace Metrics.MCP;

/// <summary>
/// Tool for fetching author metrics.
/// </summary>
[McpServerToolType]
public sealed class AuthorMetricsTool : ToolBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="userMembershipService"></param>
    /// <returns></returns>
    public AuthorMetricsTool([FromServices] DataClient client)
        : base(client)
    {
    }
    
    /// <summary>
    /// Gets the author metrics for the given author within the specified sprint.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the author metrics for the given author within the specified sprint")]
    public async Task<string> GetAuthorMetricsSummaryBySprintAsync([Description("The author to fetch metrics for")] string author,
        [Description("The year of the metrics")] int year,
        [Description("The sprint number of the metrics")] int sprintNumber) =>
        JsonSerializer.Serialize(await _client.QueryAuthorMetricsBySprintAsync(author, year, sprintNumber), AuthorMetricsJsonContext.Default.AuthorMetrics);

    /// <summary>
    /// Gets the author metrics for the given author within the specified month.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the author metrics for the given author within the specified month")]
    public async Task<string> GetAuthorMetricsSummaryByMonthAsync([Description("The author to fetch metrics for")] string author,
        [Description("The year of the metrics")] int year,
        [Description("The month of the metrics")] int month) =>
        JsonSerializer.Serialize(await _client.QueryAuthorMetricsByMonthAsync(author, year, month), AuthorMetricsJsonContext.Default.AuthorMetrics);

    /// <summary>
    /// Gets the team metrics by month.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics by month for all authors in the given team")]
    public async Task<string> GetAuthorMetricsSummaryForTeamByMonthAsync([Description("The team to fetch metrics for")] string team,
        [Description("The year of the metrics")] int year,
        [Description("The month of the metrics")] int month) =>
        JsonSerializer.Serialize(await _client.QueryAuthorMetricsForTeamByMonthAsync(team, year, month), AuthorMetricsJsonContext.Default.IEnumerableAuthorMetrics);

    /// <summary>
    /// Gets the team metrics by sprint.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the metrics by sprint for the authors in the team")]
    public async Task<string> GetAuthorMetricsSummaryForTeamBySprintAsync([Description("The team to fetch metrics for")] string team,
        [Description("The year of the metrics")] int year,
        [Description("The sprint number of the metrics")] int sprintNumber) =>
        JsonSerializer.Serialize(await _client.QueryAuthorMetricsForTeamBySprintAsync(team, year, sprintNumber), AuthorMetricsJsonContext.Default.IEnumerableAuthorMetrics);
        
}
