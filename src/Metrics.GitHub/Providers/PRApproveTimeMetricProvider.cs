using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Metrics.Models;
using Metrics;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Calculate the approve time for a pr.
/// </summary>
public sealed class PRApproveTimeMetricProvider : GitHubMetricProvider
{
    readonly MultiOrgUserService<GitHubOrganization> _userService;
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRApproveTimeMetricProvider(ILogger<PRApproveTimeMetricProvider> logger, MultiOrgUserService<GitHubOrganization> userService)
        : base(logger) => (_userService) = (userService);

    /// <summary>
    /// Calculates the approve time metric. It is the time elapsed since the first commit to first approval.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        // Time to Approve = Time of Final Approval - Time the PR was Picked Up for Review
        var approveTime = TimeSpan.Zero;
        if (pr.commits.nodes.Count > 0)
        {
            var reviews = pr.reviews.nodes;
            var firstReview = reviews.FirstOrDefault();
            DateTime? firstReviewDate = firstReview != null ? firstReview.submittedAt : null;
            var org = _userService.GetOrgUserService(pr.repository.owner?.login)?.Organization;
            var firstCommentNode = pr.comments.nodes.FirstOrDefault(n => !string.Equals(pr.author?.login, n.author?.login, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(org.IgnoreCommentsFromBotName, n.author?.login, StringComparison.OrdinalIgnoreCase));

            if (firstCommentNode is not null)
            {
                firstReviewDate = firstCommentNode.createdAt < firstReviewDate ? firstCommentNode.createdAt : firstReviewDate;
            }

            var finalApprovedReview = reviews.LastOrDefault(n => GitHubStates.Approved.Equals(n.state));
            approveTime = firstReview != null && finalApprovedReview != null ? finalApprovedReview.updatedAt.GetWeekdayTimeSpan(firstReviewDate.Value) : TimeSpan.Zero;
        }
        SetTimeElapsed(approveTime, span => metrics.DevExMetricItem.ApproveTime = span);
    }
}
