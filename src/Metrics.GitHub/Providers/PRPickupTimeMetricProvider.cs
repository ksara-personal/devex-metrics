using Microsoft.Extensions.Logging;
using Metrics.Models;
using Metrics;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Calculates the pickup metric for a pr.
/// </summary>
public sealed class PRPickupTimeMetricProvider : GitHubMetricProvider
{
    readonly MultiOrgUserService<GitHubOrganization> _userService;

    public PRPickupTimeMetricProvider(ILogger<PRPickupTimeMetricProvider> logger, MultiOrgUserService<GitHubOrganization> userService)
        : base(logger) => (_userService) = (userService);

    /// <summary>
    /// Caluclates the pick up time metric. It is the time elpased from when the PR is raised to first review time.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        // Time from PR creation to first review (comment or approval)
        var firstReview = pr.reviews.nodes.FirstOrDefault();
        DateTime? firstReviewDate = firstReview != null ? firstReview.submittedAt : null;
        // check if we have comments not from author and bot. if the comment is added before the review, then use that start date.
        if (pr.totalCommentsCount > 0)
        {
            var org = _userService.GetOrgUserService(pr.repository.owner?.login)?.Organization;
            var firstCommentNode = pr.comments.nodes.FirstOrDefault(n => !string.Equals(pr.author?.login, n.author?.login, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(org.IgnoreCommentsFromBotName, n.author?.login, StringComparison.OrdinalIgnoreCase));

            if (firstCommentNode is not null)
            {
                firstReviewDate = firstCommentNode.createdAt < firstReviewDate ? firstCommentNode.createdAt : firstReviewDate;
            }
        }
        SetTimeElapsed(firstReviewDate != null ? firstReviewDate.Value.GetWeekdayTimeSpan(pr.createdAt) : TimeSpan.Zero, span => metrics.DevExMetricItem.PickupTime = span);
    }
}
