using System;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Cycle time metric provider.
/// </summary>
public sealed class PRCycleTimeMetricProvider : GitHubMetricProvider
{
    public PRCycleTimeMetricProvider(ILogger<PRCycleTimeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates and fills the metric for cycle time. It is the time between first commit and PR merge.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        TimeSpan cycleTime = TimeSpan.Zero;
        if (pr.commits.nodes.Count > 0)
        {
            var firstCommit = pr.commits.nodes.First();
            cycleTime = pr.mergedAt.HasValue ? pr.mergedAt.Value.GetWeekdayTimeSpan(firstCommit.commit.committedDate) : TimeSpan.Zero;
        }
        SetTimeElapsed(cycleTime, span => metrics.DevExMetricItem.CycleTime = span);
    }
}
