using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Controller for managing contributors.
/// </summary>
public sealed class ContributorsController : MetricsController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContributorsController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public ContributorsController(DevExMetricDbContext context, ILogger<MetricsController> logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the list of contributors.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<PRContributor> Get() => DbContext.Contributors.AsNoTracking();
}
