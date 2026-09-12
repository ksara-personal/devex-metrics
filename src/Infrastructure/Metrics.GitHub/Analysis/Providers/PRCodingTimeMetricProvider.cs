using System;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Helps to calculate the coding time between the first commit and when the pr is finally merged.
/// This class cannot be inherited.
/// </summary>
public sealed class PRCodingTimeMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRCodingTimeMetricProvider(ILogger<PRCodingTimeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the approve time metric. It is the time elapsed since the first commit to pr is created.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        TimeSpan codingTime = TimeSpan.Zero;
        if (pr.commits.nodes.Count > 0)
        {
            var firstCommit = pr.commits.nodes.First();
            // the first commit is before the pr created date, so we can use that as a base.
            // not sure how this happens, but handle weird scenarios though updatedAt happens very frquently.
            var target = pr.createdAt < firstCommit.commit.committedDate ? pr.updatedAt : pr.createdAt;
            codingTime = target.GetWeekdayTimeSpan(firstCommit.commit.committedDate);

            var transitions = pr.DraftTransitions;
            // draft to open transition times are considered as coding time.
            if (transitions is not null && transitions.Count > 0)
            {
                foreach (var (start, end) in transitions)
                {
                    codingTime += end.GetWeekdayTimeSpan(start);
                }
            }
        }
        SetTimeElapsed(codingTime, span => metrics.DevExMetricItem.CodingTime = span);
    }
}
