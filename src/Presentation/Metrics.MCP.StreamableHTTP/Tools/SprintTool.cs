using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace Metrics.MCP;

[McpServerToolType]
public sealed class SprintTool : Metrics.MCP.ToolBase
{
    readonly SprintCalendar _sprintCalendar;

    public SprintTool([FromServices] DataClient client,
        [FromServices]SprintCalendar sprintCalendar)
        : base(client)
    {
        _sprintCalendar = sprintCalendar;
    }
    
    /// <summary>
    /// Gets the sprint for the given release number.
    /// </summary>
    /// <param name="releaseNumber"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the sprint for the given release number")]
    public string GetSprintInfoByRelease([Description("The release number to fetch the sprint")] string releaseNumber)
    {
        if (string.IsNullOrWhiteSpace(releaseNumber))
            throw new ArgumentException("Release number must be provided.", nameof(releaseNumber));

        var sprint = _sprintCalendar.GetSprintByRelease(releaseNumber);
        return JsonSerializer.Serialize(sprint, SprintJsonContext.Default.Sprint);
    }

    /// <summary>
    /// Gets the sprint for the given sprint number and year.
    /// </summary>
    /// <param name="sprintNumber"></param>
    /// <param name="year"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets the sprint for the given sprint number and year")]
    public string GetSprintInfo([Description("The sprint number to fetch")] int sprintNumber,
        [Description("The year of the sprint")] int year)
    {
        if (sprintNumber <= 0)
            throw new ArgumentException("Sprint number must be positive.", nameof(sprintNumber));
        if (year <= 0)
            throw new ArgumentException("Year must be positive.", nameof(year));

        var sprint = _sprintCalendar.GetSprintInfo(year, sprintNumber);
        return JsonSerializer.Serialize(sprint, SprintJsonContext.Default.Sprint);
    }

    /// <summary>
    /// Gets all sprints for the given year.
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    [McpServerTool]
    [Description("Gets all sprints for the given year")]
    public string GetSprintInfoByYear([Description("The year to fetch sprints for")] int year)
    {
        if (year <= 0)
            throw new ArgumentException("Year must be positive.", nameof(year));

        var sprints = _sprintCalendar.GenerateSprints()
            .Where(s => s.Year == year)
            .ToList();

        return JsonSerializer.Serialize(sprints, SprintJsonContext.Default.IEnumerableSprint);
    }
}
