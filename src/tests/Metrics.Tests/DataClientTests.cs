using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Metrics.GitHub;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Castle.Core.Configuration;
using Metrics.EF;
using Metrics.DataMigrations;
using Metrics.GitHub.ReviewerMetrics;

namespace Metrics.Tests;

/// <summary>
/// Tests for the DataClientTests class.
/// This class tests the functionality of writing metrics to a file and database,
/// </summary>
public sealed class DataClientTests : TestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataClientTests"/> class.
    /// This constructor sets up the host with the necessary services for testing.
    /// It configures the host to use an HTTP client, logging, and the DataClient services.
    /// The DataClient is used to perform various operations such as writing metrics to a file or database,
    /// and querying metrics by ID, team, or author.
    /// </summary>
    public DataClientTests() : base()
    {
    }

    [Theory]
    [InlineData("tenant-1", "2025-01-01", "2025-07-10")]
    [InlineData("tenant-2", "2025-01-01", "2025-07-10")]
    public async Task Query_Copilot_Review_Metrics_Async(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);
        var ret = await dataClient.QueryPRsMetricsByCopilotReviewerAsync(start, end);
    }

    [Theory]
    [InlineData("tenant-1", "2025-01-01", "2025-07-10")]
    [InlineData("tenant-2", "2025-01-01", "2025-07-10")]
    public async Task Query_Copilot_Review_Metrics_Summary_For_Teams_Async(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);
        var ret = await dataClient.QueryCopilotReviewerSummaryForAllTeamsAsync(start, end);
    }

    /// <summary>
    /// Queries metrics by ID asynchronously.This method tests the functionality of querying metrics by ID.
    /// </summary>
    /// <param name="repo"></param>
    /// <param name="prId"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "your-github-org/repo-1", 1)]
    [InlineData("tenant-1", "your-github-org/repo-2", 2)]
    public async Task Query_Metrics_ById_Async(string tenantId, string repo, int prId)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        var metrics = await dataClient.QueryMetricsByIdAsync(repo, prId);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(prId, metrics.PrNumber);
        Assert.NotNull(metrics.Author);
        Assert.NotEmpty(metrics.Contributors);
    }

    /// <summary>
    /// This data is used to test the QueryMetricsByTeamAsync method.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> GetTestDataForQueryByTeam()
    {
        var current = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        yield return new object[] { "tenant-1", DataStoreType.InMemory, "Alpha", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-1", DataStoreType.Postgres, "Beta", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-1", DataStoreType.File, "Alpha", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-2", DataStoreType.File, "Beta", new DateTime(current.Year, 1, 1), current };
    }

    /// <summary>
    /// Queries metrics by team asynchronously.
    /// This method tests the functionality of querying metrics by team.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [MemberData(nameof(GetTestDataForQueryByTeam))]
    public async Task Query_Metrics_ByTeam_Async(string tenantId, DataStoreType storeType, string team, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(storeType);

        var metrics = await dataClient.QueryMetricsByTeamAsync(team, start, end);

        // Assert
        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    /// <summary>
    /// This data is used to test the QueryMetricsByAuthorAsync method.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> GetTestDataForQueryByAuthor()
    {
        // test by email, name and login.
        var current = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        yield return new object[] { "tenant-1", "author@example.com", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-1", "github-user-1", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-1", "github-user-2", new DateTime(current.Year, 1, 1), current };
    }

    /// <summary>
    /// Queries metrics by author asynchronously.
    /// This method tests the functionality of querying metrics by author.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [MemberData(nameof(GetTestDataForQueryByAuthor))]
    public async Task Query_Metrics_ByAuthor_Async(string tenantId, string author, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        var metrics = await dataClient.QueryMetricsByAuthorAsync(author, start, end);

        // Assert
        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    /// <summary>
    /// Queries metrics by author asynchronously with failures.
    /// This method tests the functionality of querying metrics by author when the author does not exist.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "nonexistent_author", "2025-01-01", "2025-12-31")]
    [InlineData("tenant-2", "nonexistent_author", "2025-01-01", "2025-12-31")]
    public async Task Query_Metrics_ByAuthor_Async_With_Failures(string tenantId, string author, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            // This should throw an exception since the author does not exist.
            await dataClient.QueryMetricsByAuthorAsync(author, start, end);
        });
    }

    /// <summary>
    /// Queries metrics by team asynchronously with failures.
    /// This method tests the functionality of querying metrics by team when the team does not exist.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "no_team", "2025-06-01", "2025-06-30")]
    [InlineData("tenant-2", "no_team", "2025-06-01", "2025-06-30")]
    public async Task Query_Metrics_ByTeam_Async_With_Failures(string tenantId, string author, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient();

        await Assert.ThrowsAsync<ArgumentException>(async () => await dataClient.QueryMetricsByTeamAsync(author, start, end));
    }

    [Theory]
    [InlineData("tenant-1", "Alpha", 2025, 20)]
    [InlineData("tenant-2", "Alpha", 2025, 20)]
    public async Task Query_Author_Sprint_Metrics_Summary_For_Team_Async(string tenantId, string team, int year, int sprintNumber)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);
        var ret = await dataClient.QueryAuthorMetricsForTeamBySprintAsync(team, year, sprintNumber);

        Assert.NotNull(ret);
        Assert.Equal(team, ret.First().Team);
    }

    [Theory]
    [InlineData("tenant-1", "Alpha", 2025, 10)]
    [InlineData("tenant-2", "Alpha", 2025, 10)]
    public async Task Query_Author_Monthly_Metrics_Summary_For_Team_Async(string tenantId, string team, int year, int month)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);
        var ret = await dataClient.QueryAuthorMetricsForTeamByMonthAsync(team, year, month);

        Assert.NotNull(ret);
        Assert.Equal(team, ret.First().Team);
    }

    /// <summary>
    /// Queries author metrics by sprint asynchronously.
    /// This method tests the functionality of querying author metrics for a specific sprint.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year of the sprint</param>
    /// <param name="sprintNumber">The sprint number</param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "github-user-1", 2025, 20)]
    [InlineData("tenant-1", "github-user-2", 2025, 20)]
    public async Task Query_Author_Metrics_BySprint_Async(string tenantId, string author, int year, int sprintNumber)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        var metrics = await dataClient.QueryAuthorMetricsBySprintAsync(author, year, sprintNumber);

        // Assert
        Assert.NotNull(metrics);
        Assert.NotNull(metrics.Author);
        Assert.True(metrics.PrsAuthored >= 0);
    }

    /// <summary>
    /// Queries author metrics by month asynchronously.
    /// This method tests the functionality of querying author metrics for a specific month.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "github-user-1", 2025, 10)]
    [InlineData("tenant-1", "github-user-2", 2025, 9)]
    public async Task Query_Author_Metrics_ByMonth_Async(string tenantId, string author, int year, int month)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        var metrics = await dataClient.QueryAuthorMetricsByMonthAsync(author, year, month);

        // Assert
        Assert.NotNull(metrics);
        Assert.NotNull(metrics.Author);
        Assert.True(metrics.PrsAuthored >= 0);
    }

    /// <summary>
    /// Queries author metrics by sprint asynchronously with failures.
    /// This method tests the functionality of querying author metrics when the author does not exist for the sprint.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year</param>
    /// <param name="sprintNumber">The sprint number</param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "nonexistent_author", 2025, 20)]
    [InlineData("tenant-2", "nonexistent_author", 2025, 20)]
    public async Task Query_Author_Metrics_BySprint_Async_With_Failures(string tenantId, string author, int year, int sprintNumber)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            // This should throw an exception since the author does not exist or has no data for the sprint.
            await dataClient.QueryAuthorMetricsBySprintAsync(author, year, sprintNumber);
        });
    }

    /// <summary>
    /// Queries author metrics by month asynchronously with failures.
    /// This method tests the functionality of querying author metrics when the author does not exist for the month.
    /// </summary>
    /// <param name="author">The author's login or email</param>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "nonexistent_author", 2025, 1)]
    [InlineData("tenant-2", "nonexistent_author", 2025, 1)]
    public async Task Query_Author_Metrics_ByMonth_Async_With_Failures(string tenantId, string author, int year, int month)
    {
        SetTenant(tenantId);
        var dataClient = GetDataClient(DataStoreType.Postgres);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            // This should throw an exception since the author does not exist or has no data for the month.
            await dataClient.QueryAuthorMetricsByMonthAsync(author, year, month);
        });
    }

    
}
