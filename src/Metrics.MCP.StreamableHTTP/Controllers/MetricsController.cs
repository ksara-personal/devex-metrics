using Metrics.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Metrics;
using System.Diagnostics;
using Metrics.Models;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Base controller for handling metrics-related requests.
/// </summary>
/// <typeparam name="TContext"></typeparam>
[Authorize]
public abstract class MetricsController : ODataController
{
    readonly DevExMetricDbContext _context;
    protected readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsController{TContext}"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    protected MetricsController(DevExMetricDbContext context, ILogger<MetricsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the database context for the current controller.
    /// </summary>
    protected DevExMetricDbContext DbContext => _context;
}

/// <summary>
/// Action filter to post-process metrics after action execution and manage memory
/// </summary>
public sealed class MetricsActionFilterAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Post-processes the metrics in the action result after the action has executed.  
    /// </summary>
    /// <param name="context"></param>
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var objectContent = context.Result as ObjectResult;
        if (objectContent != null)
        {
            var metrics = (IEnumerable<PRMetrics>?)objectContent.Value;
            objectContent.Value = metrics?.Select(m => m.ToPRMetricsEx());
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
