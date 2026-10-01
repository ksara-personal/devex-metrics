using Metrics.ADO.EF;
using Metrics.ADO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Metrics.ADO.Controllers;

/// <summary>
/// OData controller for epic metrics.
/// Provides endpoints to query epic metrics by team, release version, or both.
/// </summary>
[Route("{__tenant__}/odata/ado/[controller]")]
public sealed class EpicMetricsController : ODataController
{
    readonly ADOMetricsDbContext _dbContext;
    readonly ILogger<EpicMetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicMetricsController"/> class.
    /// </summary>
    /// <param name="dbContext">The ADO metrics database context.</param>
    /// <param name="logger">The logger instance.</param>
    public EpicMetricsController(ADOMetricsDbContext dbContext, ILogger<EpicMetricsController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all epic metrics with optional OData query parameters.
    /// Supports filtering by team and/or release version via OData query string.
    /// </summary>
    /// <returns>A queryable collection of epic metrics.</returns>
    /// <remarks>
    /// Example queries:
    /// - Get all: GET /odata/ado/epicmetrics
    /// - Filter by team: GET /odata/ado/epicmetrics?$filter=Team eq 'TeamA'
    /// - Filter by release: GET /odata/ado/epicmetrics?$filter=ReleaseVersion eq '1.0.0'
    /// - Filter by both: GET /odata/ado/epicmetrics?$filter=Team eq 'TeamA' and ReleaseVersion eq '1.0.0'
    /// - With ordering: GET /odata/ado/epicmetrics?$filter=Team eq 'TeamA'&amp;$orderby=CreatedDate desc
    /// </remarks>
    [HttpGet]
    [EnableQuery(MaxTop = 1000, AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<WorkItemMetrics> Get() => _dbContext.WorkItemMetrics.AsNoTracking();

    /// <summary>
    /// Gets epic metrics for a specific team.
    /// </summary>
    /// <param name="team">The team name to filter epic metrics.</param>
    /// <returns>A collection of epic metrics for the specified team.</returns>
    /// <remarks>
    /// Example: GET /odata/ado/epicmetrics/byteam?team=TeamA
    /// </remarks>
    [HttpGet("byteam")]
    public async Task<ActionResult<IEnumerable<WorkItemMetrics>>> GetByTeam([FromQuery] string team)
    {
        if (string.IsNullOrWhiteSpace(team))
        {
            return BadRequest("Team parameter is required");
        }

        _logger.LogInformation("Retrieving epic metrics for team: {Team}", team);

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.Team == team)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} epic metrics for team {Team}", metrics.Count, team);

        return Ok(metrics);
    }

    /// <summary>
    /// Gets epic metrics for a specific release version.
    /// </summary>
    /// <param name="releaseVersion">The release version to filter epic metrics.</param>
    /// <returns>A collection of epic metrics for the specified release version.</returns>
    /// <remarks>
    /// Example: GET /odata/ado/epicmetrics/byreleaseversion?releaseVersion=1.0.0
    /// </remarks>
    [HttpGet("byreleaseversion")]
    public async Task<ActionResult<IEnumerable<WorkItemMetrics>>> GetByReleaseVersion([FromQuery] string releaseVersion)
    {
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            return BadRequest("ReleaseVersion parameter is required");
        }

        _logger.LogInformation("Retrieving epic metrics for release version: {ReleaseVersion}", releaseVersion);

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.ReleaseVersion == releaseVersion)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} epic metrics for release version {ReleaseVersion}", 
            metrics.Count, releaseVersion);

        return Ok(metrics);
    }

    /// <summary>
    /// Gets epic metrics for a specific team and release version.
    /// </summary>
    /// <param name="team">The team name to filter epic metrics.</param>
    /// <param name="releaseVersion">The release version to filter epic metrics.</param>
    /// <returns>A collection of epic metrics for the specified team and release version.</returns>
    /// <remarks>
    /// Example: GET /odata/ado/epicmetrics/byteamandreleaseversion?team=TeamA&amp;releaseVersion=1.0.0
    /// </remarks>
    [HttpGet("byteamandreleaseversion")]
    public async Task<ActionResult<IEnumerable<WorkItemMetrics>>> GetByTeamAndReleaseVersion(
        [FromQuery] string team,
        [FromQuery] string releaseVersion)
    {
        if (string.IsNullOrWhiteSpace(team))
        {
            return BadRequest("Team parameter is required");
        }

        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            return BadRequest("ReleaseVersion parameter is required");
        }

        _logger.LogInformation("Retrieving epic metrics for team: {Team} and release version: {ReleaseVersion}", 
            team, releaseVersion);

        var metrics = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .Where(m => m.Team == team && m.ReleaseVersion == releaseVersion)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} epic metrics for team {Team} and release version {ReleaseVersion}", 
            metrics.Count, team, releaseVersion);

        return Ok(metrics);
    }

    /// <summary>
    /// Gets a single epic metric by work item ID.
    /// </summary>
    /// <param name="key">The work item ID.</param>
    /// <returns>The epic metric with the specified ID.</returns>
    /// <remarks>
    /// Example: GET /odata/ado/epicmetrics(12345)
    /// </remarks>
    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<WorkItemMetrics>> Get([FromRoute] int key)
    {
        _logger.LogInformation("Retrieving epic metric for ID: {WorkItemId}", key);

        var metric = await _dbContext.WorkItemMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkItemId == key);

        if (metric == null)
        {
            _logger.LogWarning("Epic metric not found for ID: {WorkItemId}", key);
            return NotFound();
        }

        return Ok(metric);
    }
}
