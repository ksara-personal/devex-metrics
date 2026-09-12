using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Controller for managing reviewer metrics.
/// </summary>
public sealed class ReviewerSprintMetricsController : MetricsController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewerSprintMetricsController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public ReviewerSprintMetricsController(DevExMetricDbContext context, ILogger<MetricsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the reviewer sprint metrics.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<ReviewerSprintMetrics> Get()
    {
        return DbContext.ReviewerSprintMetrics
            .AsNoTracking()
            .AsSingleQuery();
    }
}

/// <summary>
/// Controller for managing reviewer monthly metrics.
/// </summary>
public sealed class ReviewerMonthlyMetricsController : MetricsController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewerMonthlyMetricsController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public ReviewerMonthlyMetricsController(DevExMetricDbContext context, ILogger<MetricsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the reviewer monthly metrics.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<ReviewerMonthlyMetrics> Get()
    {
        return DbContext.ReviewerMonthlyMetrics
            .AsNoTracking()
            .AsSingleQuery();
    }
}

public sealed class PRReviewerMetricsController : MetricsController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PRReviewerMetricsController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public PRReviewerMetricsController(DevExMetricDbContext context, ILogger<MetricsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets the PR reviewer metrics.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<PRReviewerMetrics> Get()
    {
        return DbContext.PRReviewerMetrics
            .AsNoTracking()
            .AsSingleQuery()
            .Include(pr => pr.PRMetric);
    }
}