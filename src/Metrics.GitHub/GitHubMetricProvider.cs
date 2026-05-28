using Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// Abstract class GitHubMetricProvider.
/// </summary>
public abstract class GitHubMetricProvider : MetricProvider<GitHubPRRoot>
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    protected GitHubMetricProvider(ILogger<GitHubMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the metric.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected abstract void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics);

    /// <summary>
    /// Calculates rhe metric.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    public override void CalculateAndAddMetric(GitHubPRRoot source, PRMetrics metrics) =>
        CalculateAndAddMetric(source.data.repository.pullRequest, metrics);
}
