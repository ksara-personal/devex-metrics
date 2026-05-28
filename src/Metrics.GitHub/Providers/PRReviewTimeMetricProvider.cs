using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Calculates the cycle time between the reviews. This class cannot be inherited,
/// </summary>
public sealed class PRReviewTimeMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRReviewTimeMetricProvider(ILogger<PRReviewTimeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the cycle time metrics. It is the elapsed between first and last reviews.
    /// </summary>
    /// <param name="pr"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        // Time between first review/comment and the PR being merged.
        var reviewNodes = pr.reviews.nodes;
        var firstComment = reviewNodes.FirstOrDefault(n => GitHubStates.Commented.Equals(n.state));
        var firstReview = reviewNodes.FirstOrDefault();
        DateTime? reviewDate = firstComment != null ?
                            firstComment.updatedAt > firstReview.updatedAt ? firstReview.updatedAt : firstComment.updatedAt
                            : firstReview is null ? null : firstReview.updatedAt;
                            
        SetTimeElapsed(reviewDate.HasValue && pr.mergedAt.HasValue ?
                pr.mergedAt.Value.GetWeekdayTimeSpan(reviewDate.Value)
                : TimeSpan.Zero, span => metrics.DevExMetricItem.ReviewTime = span);
    }
}
