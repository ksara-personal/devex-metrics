using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Controller for managing author metrics.
/// Provides OData endpoints to query individual author metrics and team-level author metrics
/// aggregated by sprint or month.
/// </summary>
[Authorize]
public sealed class AuthorMetricsController : ODataController
{
    private readonly DataClient _dataClient;
    private readonly ILogger<AuthorMetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorMetricsController"/> class.
    /// </summary>
    /// <param name="dataClient">The data client for querying metrics</param>
    /// <param name="logger">The logger instance</param>
    public AuthorMetricsController(DataClient dataClient, ILogger<AuthorMetricsController> logger)
    {
        _dataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets author metrics for a specific author by sprint.
    /// OData function to query author metrics filtered by author, year, and sprint number.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year of the sprint</param>
    /// <param name="sprintNumber">The sprint number</param>
    /// <returns>Author metrics for the specified sprint</returns>
    [HttpGet("odata/AuthorMetrics/Sprint")]
    public async Task<IActionResult> GetByAuthorSprint(
        [FromQuery] string author,
        [FromQuery] int year,
        [FromQuery] int sprintNumber)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            return BadRequest("Author parameter is required");
        }

        try
        {
            var metrics = await _dataClient.QueryAuthorMetricsBySprintAsync(author, year, sprintNumber);
            
            if (metrics == null)
            {
                return NotFound($"No metrics found for author '{author}' in sprint {sprintNumber} of year {year}");
            }

            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for author metrics query");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving author metrics by sprint");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving metrics");
        }
    }

    /// <summary>
    /// Gets author metrics for a specific author by month.
    /// OData function to query author metrics filtered by author, year, and month.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <returns>Author metrics for the specified month</returns>
    [HttpGet("odata/AuthorMetrics/Month")]
    public async Task<IActionResult> GetByAuthorMonth(
        [FromQuery] string author,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            return BadRequest("Author parameter is required");
        }

        if (month < 1 || month > 12)
        {
            return BadRequest("Month must be between 1 and 12");
        }

        try
        {
            var metrics = await _dataClient.QueryAuthorMetricsByMonthAsync(author, year, month);
            
            if (metrics == null)
            {
                return NotFound($"No metrics found for author '{author}' in month {month} of year {year}");
            }

            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for author metrics query");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving author metrics by month");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving metrics");
        }
    }

    /// <summary>
    /// Gets author metrics for a team by sprint.
    /// OData function that returns metrics for all authors in the specified team during the sprint.
    /// Supports OData query options like $filter, $orderby, $top, $skip.
    /// </summary>
    /// <param name="team">The team name</param>
    /// <param name="year">The year of the sprint</param>
    /// <param name="sprintNumber">The sprint number</param>
    /// <returns>Collection of author metrics for the team</returns>
    [HttpGet("odata/TeamAuthorMetrics/Sprint")]
    [EnableQuery]
    public async Task<IActionResult> GetByTeamSprint(
        [FromQuery] string team,
        [FromQuery] int year,
        [FromQuery] int sprintNumber)
    {
        if (string.IsNullOrWhiteSpace(team))
        {
            return BadRequest("Team parameter is required");
        }

        try
        {
            var metrics = await _dataClient.QueryAuthorMetricsForTeamBySprintAsync(team, year, sprintNumber);
            
            if (metrics == null || !metrics.Any())
            {
                return NotFound($"No metrics found for team '{team}' in sprint {sprintNumber} of year {year}");
            }

            // Convert to IQueryable to support OData query options
            return Ok(metrics.AsQueryable());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for team author metrics query");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team author metrics by sprint");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving metrics");
        }
    }

    /// <summary>
    /// Gets author metrics for a team by month.
    /// OData function that returns metrics for all authors in the specified team during the month.
    /// Supports OData query options like $filter, $orderby, $top, $skip.
    /// </summary>
    /// <param name="team">The team name</param>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <returns>Collection of author metrics for the team</returns>
    [HttpGet("odata/TeamAuthorMetrics/Month")]
    [EnableQuery]
    public async Task<IActionResult> GetByTeamMonth(
        [FromQuery] string team,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (string.IsNullOrWhiteSpace(team))
        {
            return BadRequest("Team parameter is required");
        }

        if (month < 1 || month > 12)
        {
            return BadRequest("Month must be between 1 and 12");
        }

        try
        {
            var metrics = await _dataClient.QueryAuthorMetricsForTeamByMonthAsync(team, year, month);
            
            if (metrics == null || !metrics.Any())
            {
                return NotFound($"No metrics found for team '{team}' in month {month} of year {year}");
            }

            // Convert to IQueryable to support OData query options
            return Ok(metrics.AsQueryable());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for team author metrics query");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team author metrics by month");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving metrics");
        }
    }
}
