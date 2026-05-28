using Metrics.EF;
using Metrics.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
public sealed class TeamsController : MetricsController
{
    public TeamsController(DevExMetricDbContext context, ILogger<MetricsController> logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the list of teams.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<Team> Get() => DbContext.Teams.AsNoTracking();
}
