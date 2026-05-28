using Metrics.EF;
using Metrics.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Controller for managing sprint metrics.
/// </summary>
public sealed class SprintsController : MetricsController
{
    readonly SprintCalendar _sprintCalendar;
    /// <summary>
    /// Initializes a new instance of the <see cref="SprintController"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public SprintsController(DevExMetricDbContext context, ILogger<MetricsController> logger,
        SprintCalendar sprintCalendar)
        : base(context, logger)
    {
        _sprintCalendar = sprintCalendar;
    }

    /// <summary>
    /// Gets the sprint metrics.
    /// </summary>
    /// <returns></returns>
    [EnableQuery]
    public IQueryable<Sprint> Get() => _sprintCalendar.GetSprintsUpto().AsQueryable();
}
