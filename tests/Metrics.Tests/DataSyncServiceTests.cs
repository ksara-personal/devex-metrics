using System;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Tests;

public sealed class DataSyncServiceTests : TestBase
{
    public DataSyncServiceTests() : base()
    {
    }

    [Theory]
    [InlineData("tenant-1", null, null)]
    [InlineData("tenant-2", null, null)]
    public async Task Write_Metrics_To_SqliteDatabase_Async(string tenantId, DateTime? start, DateTime? end)
    {
        SetTenant(tenantId);
        await WriteToDatabaseAndValidate(DataStoreType.SQLite, start, end);
    }

    [Theory]
    [InlineData("tenant-1", "2025-01-01", "2025-05-01")]
    [InlineData("tenant-2", "2025-01-01", "2025-05-01")]
    public async Task Write_Metrics_To_PostgresDatabase_Async(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        await WriteToDatabaseAndValidate(DataStoreType.Postgres, start, end);
    }

    /// <summary>
    /// Writes to db and validates the results.
    /// </summary>
    /// <param name="storeType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    async Task WriteToDatabaseAndValidate(DataStoreType storeType, DateTime? start, DateTime? end)
    {
        var dataClient = GetDataSynchronizer(storeType);

        await dataClient.WriteMetricsAsync(start, end);

        // now query to get the data.
        var results = await GetDataClient(storeType).QueryMetricsByTeamAsync("Celeste", start.GetValueOrDefault(DateTime.Now), end.GetValueOrDefault(DateTime.Now));

        var copilotReviews = results.Where(r => r.CopilotReviewMetrics != null).ToList();
        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.NotEmpty(copilotReviews);
    }

    [Theory]
    [InlineData("tenant-1")]
    [InlineData("tenant-2")]
    public async Task Update_Open_Prs_Async(string tenantId)
    {
        SetTenant(tenantId);
        var dataClient = GetDataSynchronizer(DataStoreType.SQLite);
        await dataClient.UpdateMetricsAsync();
    }

    /// <summary>
    /// Writes metrics to a database asynchronously.
    /// This method tests the functionality of writing metrics to a database.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "2025-10-01", "2025-10-31")]
    [InlineData("tenant-2", "2025-10-01", "2025-10-31")]
    public async Task Write_Metrics_To_InMemoryDatabase_Async(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataSynchronizer(DataStoreType.InMemory);

        await dataClient.WriteMetricsAsync(start, end);
        // now query to get the data.
        var results = await GetDataClient(DataStoreType.InMemory).QueryMetricsByTeamAsync("Celeste", start, end);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Writes metrics to a file asynchronously.This method tests the functionality of writing metrics to a file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [Theory]
    [MemberData(nameof(GetTestDataForWriteToFile))]
    public async Task Write_Metrics_ToFileAsync(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetDataSynchronizer(DataStoreType.File);
        await dataClient.WriteMetricsAsync(start, end);

        var configuration = _host.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var filePath = configuration.GetConnectionString("File");

        // Assert
        Assert.True(File.Exists(filePath));
        var json = await File.ReadAllTextAsync(filePath);

        var results = JsonSerializer.Deserialize<IEnumerable<PRMetrics>>(json, PRMetricsContext.Default.IEnumerablePRMetrics);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }
    
    /// <summary>
    /// Test data for writing metrics to a file.
    /// This data is used to test the WriteMetricsToFileAsync method.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> GetTestDataForWriteToFile()
    {
        var current = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        yield return new object[] { "tenant-1", new DateTime(current.Year, 1, 1), current };
        yield return new object[] { "tenant-2", new DateTime(current.Year, 1, 1), current };
    }
}
