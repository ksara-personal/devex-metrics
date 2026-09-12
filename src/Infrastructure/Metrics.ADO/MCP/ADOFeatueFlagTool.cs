using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace Metrics.ADO.MCP;

/// <summary>
/// Provides tools for managing feature flags in ADO.
/// </summary>
[McpServerToolType]
public sealed class ADOFeatureFlagTool
{
    readonly ILogger<ADOFeatureFlagTool> _logger;
    readonly WorkItemClient client;
    readonly DevExMetricDbContext dbContext;
    /// <summary>
    /// Initializes a new instance of the <see cref="ADOFeatureFlagTool"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="client"></param>
    /// <param name="dbContext"></param>
    public ADOFeatureFlagTool(ILogger<ADOFeatureFlagTool> logger, [FromServices] WorkItemClient client,
        [FromServices] DevExMetricDbContext dbContext)
    {
        _logger = logger;
        this.client = client;
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Gets feature flag work items for a specific release version.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="dbContext"></param>
    /// <param name="releaseVersion"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets feature flag work items for a specific release version.")]
    public async Task<string> GetFeatureFlagWorkItemsForReleaseVersionAsync(
        [Description("The release version to filter work items.")] string releaseVersion)
    {
        _logger.LogInformation("Fetching feature flag work items for release version: {ReleaseVersion}", releaseVersion);
        var workItems = await client.GetFeatureFlagWorkItemsForReleaseVersionAsync(dbContext, releaseVersion);
        return System.Text.Json.JsonSerializer.Serialize(workItems,
            WorkItemWithGitHubLinkRootContext.Default.IEnumerableWorkItemWithGitHubLink);
    }
}