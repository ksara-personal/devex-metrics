using System;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Pr commit Metric.
/// </summary>
public sealed class PRCommitMetricProvider : GitHubMetricProvider
{
    const int SmallCommitThreshold = 20;
    const int MediumCommitThreshold = 100;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRCommitMetricProvider(ILogger<PRCommitMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Caluclates the PR size metric
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        var commentedReviews = pr.reviews.nodes.Where(r => r.state == GitHubStates.Commented && !string.IsNullOrEmpty(r.commit.abbreviatedOid)).ToList();
        var commentedReviewsCount = commentedReviews.Count;
        var totalCommits = pr.commits.totalCount;
        var commits = pr.commits.nodes.ToDictionary(k => k.commit.abbreviatedOid, v => v.commit.additions + v.commit.deletions, StringComparer.OrdinalIgnoreCase);

        var metricItem = metrics.DevExMetricItem;
        metricItem.AvgReviewCommentsPerCommit = Convert.ToSingle(totalCommits > 0 ? Math.Round(commentedReviewsCount * 1.0f / totalCommits, 1) : 0.0f);
        metricItem.TotalReviewComments = commentedReviewsCount;

        // find the comments per commit.
        //Scenario	Comments per Commit (typical)
        //Small, focused commit (1–20 LOC)	0–2
        //Medium commit (20–100 LOC)	1–5
        //Large commit (100+ LOC)	3–10+

        (int commits, int comments) small = (0, 0), medium = (0, 0), large = (0, 0);
        foreach (var node in commentedReviews.GroupBy(r => r.commit.abbreviatedOid))
        {
            var commitId = node.Key;
            if (commits.TryGetValue(commitId, out var changes))
            {
                var _ = changes switch
                {
                    < SmallCommitThreshold => small.comments += node.Count(),
                    >= SmallCommitThreshold and <= MediumCommitThreshold => medium.comments += node.Count(),
                    _ => large.comments += node.Count(),
                };
            }
        }
        
        foreach (var commit in commits)
        {
            var changes = commit.Value;
            var _ = changes switch
            {
                < SmallCommitThreshold => small.commits++,
                >= SmallCommitThreshold and <= MediumCommitThreshold => medium.commits++,
                _ => large.commits++,
            };
        }
        metricItem.TotalSmallCommits = small.commits;
        metricItem.TotalSmallCommitComments = small.comments;

        metricItem.TotalMediumCommits = medium.commits;
        metricItem.TotalMediumCommitComments = medium.comments;

        metricItem.TotalLargeCommits = large.commits;
        metricItem.TotalLargeCommitComments = large.comments;
    }
}
