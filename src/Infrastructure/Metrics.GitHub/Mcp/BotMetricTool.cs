using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Metrics.GitHub.MCP;

/// <summary>
/// Provides tools for retrieving coding agent metrics from GitHub.
/// </summary>
[McpServerToolType]
public sealed class BotMetricTool
{
    readonly DataClient _dataClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotMetricTool"/> class.
    /// </summary>
    /// <param name="dataClient">The data client used for querying metrics.</param>
    public BotMetricTool(DataClient dataClient)
    {
        _dataClient = dataClient;
    }

    /// <summary>
    /// Gets PR metrics authored by the coding agent within a specified date range.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <param name="end">The end date of the range.</param>
    /// <returns>A JSON string representing the PR metrics.</returns>
    [McpServerTool]
    [Description("Gets PR metrics authored by the coding agent within a specified date range.")]
    public async Task<string> GetMetricsAuthoredByCodingAgentAsync(
        [Description("The start date of the range.")] DateTime start,
        [Description("The end date of the range.")] DateTime end)
    {
        var result = await _dataClient.QueryMetricsByAuthorAsync(GitHubAuthors.CodingAgent, start, end);
        return result is not null ? System.Text.Json.JsonSerializer.Serialize(result, PRMetricsContext.Default.IEnumerablePRMetrics) : string.Empty;
    }

    /// <summary>
    /// Gets PR metrics authored by the dependabot within a specified date range.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <param name="end">The end date of the range.</param>
    /// <returns>A JSON string representing the PR metrics.</returns>
    [McpServerTool]
    [Description("Gets PR metrics authored by the dependabot within a specified date range.")]
    public async Task<string> GetMetricsAuthoredByDependabotAsync(
        [Description("The start date of the range.")] DateTime start,
        [Description("The end date of the range.")] DateTime end)
    {
        var result = await _dataClient.QueryMetricsByAuthorAsync(GitHubAuthors.Dependabot, start, end);
        return result is not null ? System.Text.Json.JsonSerializer.Serialize(result, PRMetricsContext.Default.IEnumerablePRMetrics) : string.Empty;
    }
}
