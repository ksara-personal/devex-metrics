using System;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Calculates the lead time for a pr.
/// </summary>
public sealed class PRLeadTimeMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRLeadTimeMetricProvider(ILogger<PRLeadTimeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the lead time since the pr is created and merged.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics) =>
        SetTimeElapsed(pr.mergedAt.HasValue ? pr.mergedAt.Value.GetWeekdayTimeSpan(pr.createdAt) : TimeSpan.Zero, span => metrics.DevExMetricItem.LeadTime = span);

}
