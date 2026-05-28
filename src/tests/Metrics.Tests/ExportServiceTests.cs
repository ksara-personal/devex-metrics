using System;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Tests;

public sealed class ExportServiceTests : TestBase
{
    public ExportServiceTests() : base()
    {
    }

    /// <summary>
    /// Exports metrics from source to target data store.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("learn", DataStoreType.SQLite, DataStoreType.Postgres)]
    [InlineData("illuminate", DataStoreType.SQLite, DataStoreType.Postgres)]
    public async Task Export_Metrics_From_Src_To_Target_Async(string tenantId, DataStoreType src, DataStoreType target)
    {
        SetTenant(tenantId);
        var exportService = _host.Services.GetRequiredService<MetricsExportService>();
        await exportService.ExportMetricsAsync(src, target);
    }
}
