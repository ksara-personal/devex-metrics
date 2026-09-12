using System;
using Metrics.GitHub.ReviewerMetrics;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

public sealed class PRReviewerMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRReviewerMetricProvider(ILogger<PRReviewerMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the reviewer metrics.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        if (pr.state == GitHubStates.Open)
        {
            return;
        }
        
        using var metricsTracker = new PRReviewMetricsTracker(pr);
        var reviewerMetrics = metricsTracker.AggregateMetrics();
        if (reviewerMetrics.Count == 0)
        {
            metrics.ReviewerDailyMetrics.Add(new ReviewerDailyMetrics
            {
                Reviewer = GitHubAuthors.Ghost,
                Date = DateOnly.MinValue
            });
        }
        else
        {
            foreach (var kv in reviewerMetrics)
            {
                var dailyMetrics = kv.Value;
                foreach (var authorKvp in dailyMetrics.ReviewerMetrics)
                {
                    metrics.ReviewerDailyMetrics.Add(authorKvp.Value);
                }
            }
        }
    }
}

