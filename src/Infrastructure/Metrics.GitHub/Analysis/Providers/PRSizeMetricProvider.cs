using System;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Pr Size Metric.
/// </summary>
public sealed class PRSizeMetricProvider : GitHubMetricProvider
{
    const int MicroPRThreshold = 100;
    const int SmallPRThreshold = 250;
    const int MediumPRThreshold = 1000; 
    const string SmallPRSize = "Small";
    const string MediumPRSize = "Medium";
    const string LargePRSize = "Large";
    const string MicroPRSize = "Micro"; 

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <returns></returns>
    public PRSizeMetricProvider(ILogger<PRSizeMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Caluclates the PR size metric
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        //Size	Total Line Changes	Description
        // Micro < 100 lines	Very easy to review, low risk
        //🟢 Small	< 250 lines	Easy to review, often isolated
        //🟡 Medium	250–1000 lines	Moderate effort, some risk
        //🔴 Large	> 1000 lines	High effort, more error-prone
        var metricItem = metrics.DevExMetricItem;
        var totalLines = pr.additions + pr.deletions;
        metricItem.PRSize = totalLines switch
        {
            <= MicroPRThreshold => MicroPRSize,
            > MicroPRThreshold and <= SmallPRThreshold => SmallPRSize,
            > SmallPRThreshold and <= MediumPRThreshold => MediumPRSize,
            > MediumPRThreshold => LargePRSize
        };
    }
}
