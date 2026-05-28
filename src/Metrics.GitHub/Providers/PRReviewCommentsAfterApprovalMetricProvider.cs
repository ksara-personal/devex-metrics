using System;
using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Helps to calculate the review comments after final approval.
/// </summary>
public sealed class PRReviewCommentsAfterApprovalMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRReviewCommentsAfterApprovalMetricProvider(ILogger<PRReviewCommentsAfterApprovalMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the total review comments received after final approval for PR is done.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        var totalComments = 0;
        if (pr.commits.nodes.Count > 0)
        {
            var nodes = pr.reviews.nodes;
            var finalApprovedReview = nodes.LastOrDefault(n => GitHubStates.Approved.Equals(n.state));
            if (finalApprovedReview != null)
            {
                var finalApprovalIndex = nodes.IndexOf(finalApprovedReview);
                totalComments = nodes.Skip(finalApprovalIndex).Count(n => GitHubStates.Commented.Equals(n.state));
            }
        }
        var metricItem = metrics.DevExMetricItem;
        metricItem.TotalReviewCommentsAfterFinalApproval = totalComments;
    }
}
