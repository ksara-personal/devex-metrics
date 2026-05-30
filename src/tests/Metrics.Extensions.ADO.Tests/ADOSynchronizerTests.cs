using System;
using Metrics.ADO;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Extensions.ADO.Tests;

public class ADOSynchronizerTests : ADOTestBase
{
    public ADOSynchronizerTests() : base()
    {
    }

    [Theory]
    [InlineData("tenant-1", @"SELECT
    [System.Id],
    [System.Title],
    [System.AssignedTo],
    [System.State]
FROM workitems WHERE
    [Microsoft.VSTS.Common.ClosedDate] > '2025-09-28T00:00:00.0000000'
    AND [System.WorkItemType] = 'Epic'
    AND [System.State] = 'Closed'")]
    [InlineData("tenant-2", @"SELECT
    [System.Id],
    [System.Title],
    [System.AssignedTo],
    [System.State]
FROM workitems WHERE
    [Microsoft.VSTS.Common.ClosedDate] > '2025-09-28T00:00:00.0000000'
    AND [System.WorkItemType] = 'Epic'
    AND [System.State] = 'Closed'")]
    public async Task Test_HierarchyQuery_Wiql(string tenantId, string wiql)
    {
        SetTenant(tenantId);
        
        var workItemClient = _host.Services.GetRequiredService<WorkItemClient>();
        var results = workItemClient.QueryWorkItemsByHierarchyWiql(wiql);
        await foreach (var batch in results)
        {
            foreach (var item in batch)
            {
                Assert.NotNull(item.WorkItemId);
            }
        }
    }

    [Theory]
    [InlineData("tenant-1")]
    [InlineData("tenant-2")]
    public async Task Test_Synchronizer_Write_Async(string tenantId)
    {
        SetTenant(tenantId);
        
        var synchronizer = _host.Services.GetRequiredService<ADOMetricsSynchronizer>();
        await synchronizer.WriteMetricsAsync();
    }
}
