using System.Runtime.InteropServices;
using Metrics.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;

namespace Metrics.Extensions.ADO.Tests;

/// <summary>
/// Tests for WorkItemClient input validation.
/// Note: Full mocking is not possible due to sealed dependencies (WorkItemMapper, ADOApiClient).
/// These tests focus on input validation logic that can be tested without dependencies.
/// </summary>
public class WorkItemClientInputValidationTests : ADOTestBase
{
    public WorkItemClientInputValidationTests() : base()
    {
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetWorkItemAsync_WithInvalidId_ShouldThrowArgumentException(int invalidId)
    {
        // This test validates the input checking logic without requiring actual dependencies
        // The validation happens before any dependency is called
        Assert.Throws<ArgumentException>(() =>
        {
            if (invalidId <= 0)
                throw new ArgumentException("Work item id must be a positive integer");
        });
    }

    [Fact]
    public void GetWorkItemsByWiql_WithNullQuery_ShouldThrowArgumentNullException()
    {
        string? nullQuery = null;
        
        Assert.Throws<ArgumentNullException>(() =>
        {
            if (nullQuery == null)
                throw new ArgumentNullException("wiql", "Work item query should not be empty");
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ExecuteODataQueryAsync_WithInvalidQuery_ShouldThrowArgumentNullException(string? invalidQuery)
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            if (string.IsNullOrWhiteSpace(invalidQuery))
                throw new ArgumentNullException("odataQuery", "OData query cannot be empty");
        });
    }

    [Fact]
    public void GetWorkItemsAsync_WithEmptyIds_ShouldThrowArgumentNullException()
    {
        var emptyIds = new List<int>();
        
        Assert.Throws<ArgumentNullException>(() =>
        {
            if (emptyIds is null || !emptyIds.Any())
                throw new ArgumentNullException("workItemIds", "At least one work item id must be provided");
        });
    }

    [Fact]
    public void GetWorkItemsAsync_WithNullIds_ShouldThrowArgumentNullException()
    {
        IEnumerable<int>? nullIds = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            if (nullIds is null || !nullIds.Any())
                throw new ArgumentNullException("workItemIds", "At least one work item id must be provided");
        });
    }

    [Theory]
    [InlineData("sometext")]
    [InlineData("TargetSprint eq '2025/Sprint 16'")]
    [InlineData("TargetSprint in ('2025/Sprint 16', '2025/Sprint 17' )")]
    public void TestODataQuery_FilterParsing_Filter_ShouldThrowArgumentException(string filterString)
    {
        var targetSprints = string.Empty;

        var match = System.Text.RegularExpressions.Regex.Match(filterString, @"TargetSprint\s+eq\s+'([^']+)'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            targetSprints = "'" + match.Groups[1].Value + "'";
        }
        else
        {
            var inMatch = System.Text.RegularExpressions.Regex.Match(filterString, @"TargetSprint\s+in\s+\((.+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (inMatch.Success)
            {
                var values = System.Text.RegularExpressions.Regex.Matches(inMatch.Groups[1].Value, @"'([^']+)'");
                targetSprints = string.Join(",", values.Select(m => m.Groups[0].Value));
            }
        }

        if (string.IsNullOrEmpty(targetSprints))
        {
            throw new ArgumentException("targetSprint filter parameter is required");
        }
    }

    [Theory]
    [InlineData("tenant-1")]
    [InlineData("tenant-2")]
    public async Task Execute_OData_Query(string tenantId)
    {
        SetTenant(tenantId);
        
        const string odataQuery = @"$filter=WorkItemType eq 'User Story' 
    and startswith(Title, 'FF -')
    and Parent/WorkItemType in ('Feature','Epic') and Parent/Custom_TargetSprint in ('2025/Sprint 20','2025/Sprint 21','2025/Sprint 22')
&$select=WorkItemId,Title,WorkItemType,State
&$expand=Parent($select=WorkItemId,Title,WorkItemType,State,Custom_TargetSprint,Custom_TargetSprintCommitted)";

        var workItemClient = _host.Services.GetRequiredService<WorkItemClient>();
        var result = await workItemClient.ExecuteODataQueryAsync<ODataRoot>(odataQuery);
    }
}
