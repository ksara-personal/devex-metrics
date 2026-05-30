using System;
using Metrics.DataMigrations;
using Metrics.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Tests;

public sealed class MigrationTests : TestBase
{
    public MigrationTests() : base()
    {
    }

    /// <summary>
    /// Applies the specified migration.
    /// </summary>
    /// <param name="migrationId"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("tenant-1", "AddUpdatedAtCol")]
    [InlineData("tenant-1", "AddDevExMetricItem")]
    [InlineData("tenant-1", "RemoveMetricItem")]
    [InlineData("tenant-1", "ReviewerMetricChanges")]
    [InlineData("tenant-2", "AddUpdatedAtCol")]
    [InlineData("tenant-2", "AddDevExMetricItem")]
    public async Task Apply_Migrations_Async(string tenantId, string migrationId)
    {
        SetTenant(tenantId);
        using var scope = _host.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner<DevExMetricDbContext>>();
        await migrationRunner.ApplyMigration(migrationId);
    }
}
