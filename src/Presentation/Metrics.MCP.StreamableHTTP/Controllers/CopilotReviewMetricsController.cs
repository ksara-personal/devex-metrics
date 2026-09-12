using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// OData controller for Copilot Review Metrics
/// </summary>
public sealed class CopilotReviewMetricsController : MetricsController
{
    readonly IMetricsPersistenceService _persistenceService;

    public CopilotReviewMetricsController(DevExMetricDbContext context,
        ILogger<CopilotReviewMetricsController> logger,
        IMetricsPersistenceService persistenceService)
        : base(context, logger)
    {
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Get Copilot Review Metrics with OData query support
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<CopilotReviewMetrics> Get()
    {
        var context = DbContext;
        return context.CopilotReviewMetrics
            .AsNoTracking()
            .AsSingleQuery();
    }

    /// <summary>
    /// Get Copilot Reviewer Metrics for all teams within a specific date range
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [HttpGet("CopilotReviewerMetricsForAllTeams")]
    public async Task<IActionResult> GetCopilotReviewerMetricsForAllTeams([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var metrics = await _persistenceService.GetCopilotReviewerMetricsForAllTeamsAsync(start, end);
        return Ok(metrics);
    }
}
