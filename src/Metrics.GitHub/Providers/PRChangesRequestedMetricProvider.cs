using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Helps to calculate the number of times changes were requested on a PR.
/// </summary>
public sealed class PRChangesRequestedMetricProvider : GitHubMetricProvider
{
    readonly MultiOrgUserService<GitHubOrganization> _repoUserService;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="cache"></param>
    public PRChangesRequestedMetricProvider(ILogger<PRChangesRequestedMetricProvider> logger, MultiOrgUserService<GitHubOrganization> userService)
        : base(logger) => _repoUserService = userService;

    /// <summary>
    /// Calculates the total changes requested on a PR.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        var nodes = pr.reviews.nodes;
        if (nodes.Count > 0)
        {
            //optimize with a single loop.
            int excellenceSquadReviewers = 0, teamReviewers = 0, nonExcellenceSquadReviewers = 0;
            var owner = pr.repository?.owner?.login;
            foreach (var node in nodes)
            {
                if (node.state.Equals(GitHubStates.ChangesRequested))
                {
                    var author = node.author?.login;
                    var _ = _repoUserService.IsCEReviewer(owner, author) ? excellenceSquadReviewers++
                        : _repoUserService.IsTeamReviewer(owner, metrics.Team, author) ? teamReviewers++ : nonExcellenceSquadReviewers++;
                }
            }
            var metricItem = metrics.DevExMetricItem;
            metricItem.TotalReviewChangesRequested = excellenceSquadReviewers + nonExcellenceSquadReviewers + teamReviewers;
            metricItem.CodeExcellenceRequestedChanges = excellenceSquadReviewers;
            metricItem.TeamRequestedChanges = teamReviewers;
            metricItem.OthersRequestedChanges = nonExcellenceSquadReviewers;
        }
    }
}
