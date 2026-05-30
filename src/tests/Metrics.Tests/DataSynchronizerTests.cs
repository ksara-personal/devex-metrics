using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Metrics.Tests;

public sealed class DataSynchronizerTests : TestBase
{
    public DataSynchronizerTests() : base()
    {
    }

    [Theory]
    [InlineData("tenant-1", "your-github-org/repo-1", 1)]
    [InlineData("tenant-1", "your-github-org/repo-2", 2)]
    public async Task Get_Metrics_ById_Using_API_Async(string tenantId, string repo, int prId)
    {
        SetTenant(tenantId);
        var dataClient = GetMetricsSynchronizer(DataStoreType.API);

        var (owner, repository) = repo.GetOwnerAndRepoNames();
        var metrics = await dataClient.GetPRMetricsAsync(owner, repository, prId);
        var dailyMetrics = metrics.ReviewerDailyMetrics;
        foreach (var grp in dailyMetrics.GroupBy(g => g.Reviewer))
        {
            var reviewer = grp.Key;
            var validResponseTimes = grp.Where(m => m.AverageResponseTimeHours > 0).ToList();
            var avgResponseTime = validResponseTimes.Any() ? validResponseTimes.Average(m => m.AverageResponseTimeHours) : 0;
            var commentsCount = grp.Sum(s => s.CommentCount);
            var approvedCount = grp.Sum(s => s.Approved);
            var changesRequestedCount = grp.Sum(s => s.ChangesRequested);
            var reviewsRequestedCount = grp.Sum(s => s.ReviewsRequested);
            var reviewsSubmitted = grp.Sum(s => s.ReviewsSubmitted);

            _logger.LogInformation("Reviewer: {Reviewer}, Avg Response Time (hrs): {AvgResponseTime}, Comments Count: {CommentsCount}, Approved Count: {ApprovedCount}, Changes Requested Count: {ChangesRequestedCount}, Reviews Requested Count: {ReviewsRequestedCount}, Reviews Submitted: {ReviewsSubmitted}", reviewer, avgResponseTime, commentsCount, approvedCount, changesRequestedCount, reviewsRequestedCount, reviewsSubmitted);
        }
        
        var json = JsonSerializer.Serialize(metrics, PRMetricsContext.Default.PRMetrics);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(prId, metrics.PrNumber);
        Assert.NotNull(metrics.Author);
        Assert.NotEmpty(metrics.Contributors);
    }
    
    [Theory]
    [InlineData("tenant-1", "2025-09-24", "2025-10-07T23:59")]
    [InlineData("tenant-2", "2025-09-24", "2025-10-07T23:59")]
    public async Task Write_Reviewer_Metrics_To_Database_Async(string tenantId, DateTime start, DateTime end)
    {
        SetTenant(tenantId);
        var dataClient = GetReviewerMetricsSynchronizer(DataStoreType.Postgres);

        await dataClient.WriteMetricsAsync(start, end);
    }
}
