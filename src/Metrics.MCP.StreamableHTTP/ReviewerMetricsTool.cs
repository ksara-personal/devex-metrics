using System.ComponentModel;
using Metrics.Models;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace Metrics.MCP;

/// <summary>
/// ReviewerMetricsTool is a tool that provides methods to query reviewer metrics data and export it to CSV files. It uses the DataClient to query the data and returns the results in a format that can be easily consumed by other tools or exported as needed.
/// </summary>
[McpServerToolType]
public sealed class ReviewerMetricsTool : Metrics.MCP.ToolBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    public ReviewerMetricsTool([FromServices] DataClient client)
        : base(client)
    {
    }

    /// <summary>
    /// Gets the reviewer metrics summary for the given year and sprint number
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the reviewer metrics summary for the given year and sprint number")]
    public Task<IEnumerable<ReviewerSprintMetrics>> GetReviewerMetricsSummaryBySprintAsync([Description("The year for which to get the metrics")] int year,
        [Description("The sprint number for which to get the metrics")] int sprintNumber,
        [Description("The repository for which to get the metrics")] string repository = null) =>
        _client.QueryReviewerMetricsSummaryBySprintAsync(year, sprintNumber, repository: repository);

    /// <summary>
    /// Gets the reviewer metrics summary for the given year and month
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the reviewer metrics summary for the given year and month")]
    public Task<IEnumerable<ReviewerMonthlyMetrics>> GetReviewerMetricsSummaryByMonthAsync([Description("The year for which to get the metrics")] int year,
        [Description("The month for which to get the metrics")] int month,
        [Description("The repository for which to get the metrics")] string repository = null) =>
        _client.QueryReviewerMetricsSummaryByMonthAsync(year, month, repository: repository);

    /// <summary>
    /// Gets the reviewer metrics summary for the given year and month for the reviewer
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the reviewer metrics summary for the given year and month for the reviewer")]
    public Task<IEnumerable<ReviewerMonthlyMetrics>> GetReviewerMetricsByReviewerForMonthAsync([Description("The year for which to get the metrics")] int year,
        [Description("The month for which to get the metrics")] int month,
        [Description("The reviewer for which to get the metrics")] string reviewer = null) =>
        _client.QueryReviewerMetricsSummaryByMonthAsync(year, month, reviewer);

    /// <summary>
    /// Gets the reviewer metrics summary for the given year and sprint number for the reviewer
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the reviewer metrics summary for the given year and sprint number for the reviewer")]
    public Task<IEnumerable<ReviewerSprintMetrics>> GetReviewerMetricsByReviewerForSprintAsync([Description("The year for which to get the metrics")] int year,
        [Description("The sprint number for which to get the metrics")] int sprintNumber,
        [Description("The reviewer for which to get the metrics")] string reviewer = null) =>
        _client.QueryReviewerMetricsSummaryBySprintAsync(year, sprintNumber, reviewer);

    /// <summary>
    /// Exports the reviewer metrics summary for the given year and month for the reviewer
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Exports the reviewer metrics summary for the given year and month and for the optional repository")]
    public async Task<CallToolResult> ExportReviewerMetricsSummaryByMonthToCsvAsync(
        [Description("The year for which to get the metrics")] int year,
        [Description("The month for which to get the metrics")] int month,
        [Description("The repository for which to get the metrics")] string repository = null)
    {
        var jsonData = await _client.QueryReviewerMetricsSummaryByMonthAsync(year, month, repository: repository);
        return await BuildEmbeddedResourceResult(jsonData, $"reviewer-metrics-{year}-{month}.csv");
    }

    /// <summary>
    /// Exports the reviewer metrics summary for the given year and sprint for the reviewer
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Exports the reviewer metrics summary for the given year and sprint and for the optional repository")]
    public async Task<CallToolResult> ExportReviewerMetricsSummaryBySprintToCsvAsync(
        [Description("The year for which to get the metrics")] int year,
        [Description("The sprint number for which to get the metrics")] int sprintNumber,
        [Description("The repository for which to get the metrics")] string repository = null)
    {
        var jsonData = await _client.QueryReviewerMetricsSummaryBySprintAsync(year, sprintNumber, repository: repository);
        return await BuildEmbeddedResourceResult(jsonData, $"reviewer-metrics-{year}-{sprintNumber}.csv");
    }
}
