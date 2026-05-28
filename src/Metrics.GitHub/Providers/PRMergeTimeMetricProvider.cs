using System;
using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Calculates the merge time for a pr.
/// </summary>
public sealed class PRMergeTimeMetricProvider : GitHubMetricProvider
{
    public PRMergeTimeMetricProvider(ILogger<PRMergeTimeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the merge time elapsed between the last pr approval is received and pr is merged.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        //PR Approved and Then Merged: Time to Merge = Time of PR Merge - Time of PR Approval
        var lastReviewApproval = pr.reviews.nodes.LastOrDefault(n => GitHubStates.Approved.Equals(n.state));
        SetTimeElapsed(lastReviewApproval != null && pr.mergedAt.HasValue ? pr.mergedAt.Value.GetWeekdayTimeSpan(lastReviewApproval.updatedAt) : TimeSpan.Zero, span => metrics.DevExMetricItem.MergeTime = span);
    }
}
