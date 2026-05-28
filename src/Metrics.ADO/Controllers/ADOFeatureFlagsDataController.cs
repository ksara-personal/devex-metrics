using Metrics.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Metrics.ADO.Controllers;

/// <summary>
/// ADO feature flags Data Controller for OData endpoints
/// </summary>
[Route("{__tenant__}/odata/ado/featureflagworkitems")]
public sealed class ADOFeatureFlagsDataController : ODataController
{
    readonly WorkItemClient _client;
    readonly ILogger _logger;
    readonly DevExMetricDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADOFeatureFlagsDataController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="client">The work item client.</param>
    public ADOFeatureFlagsDataController(ILogger<ADOFeatureFlagsDataController> logger, DevExMetricDbContext dbContext, WorkItemClient client)
    {
        _logger = logger;
        _client = client;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets feature flag work items for a specific release version.
    /// </summary>
    /// <param name="ReleaseVersion">The release version to filter work items.</param>
    /// <returns>A collection of work items with GitHub links.</returns>
    public async Task<IEnumerable<WorkItemWithGitHubLink>> Get([FromQuery] string ReleaseVersion)
    {
        if (string.IsNullOrEmpty(ReleaseVersion))
        {
            throw new ArgumentException("ReleaseVersion query parameter is required", nameof(ReleaseVersion));
        }
        
        return await _client.GetFeatureFlagWorkItemsForReleaseVersionAsync(_dbContext, ReleaseVersion);
    }
}
