using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Metrics.Tests;

public class SprintGenerationTests : TestBase
{
    public SprintGenerationTests() : base()
    {
    }

    /// <summary>
    /// Tests the GetSprintsList method.
    /// </summary>
    [Theory]
    [InlineData("learn")]
    [InlineData("illuminate")]
    public void GetSprintsList_ReturnsCorrectBoundaries(string tenantId)
    {
        SetTenant(tenantId);
        var sprintCalendar = _host.Services.GetRequiredService<SprintCalendar>();
        var sprints = sprintCalendar.GenerateSprints();

        // Assert
        Assert.Equal(new DateTime(2022, 12, 28), sprints.First().StartDate);
        Assert.Equal(new DateTime(2023, 1, 10, 23, 59, 59), sprints.First().EndDate);
        foreach (var sprint in sprints)
        {
            var (calculatedStart, calculatedEnd) = (sprint.StartDate, sprint.EndDate);
            _logger.LogInformation($"Release:{sprint.ReleaseNumber}, Sprint {sprint.SprintNumber} ({sprint.Year}): Start: {sprint.StartDate}, End: {sprint.EndDate}");
            Assert.Equal(sprint.StartDate, calculatedStart);
            Assert.Equal(sprint.EndDate, calculatedEnd);
        }
    }

    /// <summary>
    /// Tests the GetSprintsList method.
    /// </summary>
    /// <param name="releaseNumber"></param>
    [Theory]
    [InlineData("learn", "3900.59.0")]
    [InlineData("learn", "4000.0.0")]
    [InlineData("learn", "4000.2.0")]
    public void GetSprints_By_Release(string tenantId, string releaseNumber)
    {
        SetTenant(tenantId);
        var sprintCalendar = _host.Services.GetRequiredService<SprintCalendar>();
        var sprint = sprintCalendar.GetSprintByRelease(releaseNumber);

        // Assert
        Assert.NotNull(sprint);
        Assert.Equal(releaseNumber, sprint.ReleaseNumber);
    }
}