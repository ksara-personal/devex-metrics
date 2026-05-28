using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Metrics.EF;
using Microsoft.OData.UriParser;
using Metrics.Models;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// OData controller for exposing PR metrics with DevExEffciencyMetrics as individual columns
/// </summary>
public sealed class PRMetricsExController : MetricsController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PRMetricsExController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public PRMetricsExController(DevExMetricDbContext context,
        ILogger<PRMetricsExController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the PR metrics.
    /// </summary>
    /// <returns></returns>
    [EnableQuery()]
    [ServiceFilter(typeof(MetricsActionFilterAttribute))]
    public IQueryable<PRMetrics> Get()
    {
        return DbContext.PRMetrics
            .AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.DevExMetricItem)
            .Include(p => p.CopilotReviewMetrics)
            .Include(p => p.ReviewerMetrics)
            .AsSingleQuery();
    }
}
