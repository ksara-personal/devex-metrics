using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Metrics.GitHub.Providers;

/// <summary>
/// PRMaturity metric provider uses the first commit and finds the number of lines changed as a base and adds the subsequent
/// lines changed in order to provide a metric on PR Maturity.
/// </summary>
public sealed class PRMaturityMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public PRMaturityMetricProvider(ILogger<PRMaturityMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculates the maturity metric
    /// https://linearb.helpdocs.io/article/12bihr2x2s-metrics-glossary
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        // let's take the first commit as the golden start for metrics.

        // Definitions
        // Changes After PR Issued: The sum of additions and deletions made after the PR was published.
        //Current PR Size: The total size of the PR, defined as the sum of all additions and deletions in the PR

        // Total size (Current PR Size): 100 lines (additions + deletions).
        //Changes made after PR was issued (Changes After PR Issued): 20 lines.
        //The PR Maturity Metric is calculated as: round(max(0.1, 1 - (20 / 100)), 2) = 0.8
        //Result: The PR Maturity Metric for this example is 80% (0.8).

        // https://linearb.helpdocs.io/article/12bihr2x2s-metrics-glossary#Definitions:
        // https://linearb.helpdocs.io/article/6xa33vvc81-untitled-understanding-the-differences-between-pr-size-and-code-changes-in-linear-b

        var commits = pr.commits;
        int initialLinesChanged = 0, subsequentLinesChanged = 0;
        var metricItem = metrics.DevExMetricItem;
        if (commits.nodes.Count > 0)
        {
            var filteredCommits = commits.nodes.Where(n => n.commit.committedDate <= pr.createdAt).ToArray();
            initialLinesChanged = filteredCommits.Sum(c => c.commit.additions + c.commit.deletions);
            subsequentLinesChanged = commits.nodes.Except(filteredCommits).Sum(n => n.commit.additions + n.commit.deletions);
            var totalLines = initialLinesChanged + subsequentLinesChanged;

            var maturity = Math.Round(Math.Max(0.1, 1 - (subsequentLinesChanged * 1.0f / totalLines)), 2);
            metricItem.MaturityPercentage = (int)(maturity * 100);
        }
        metricItem.InitialLinesChanged = initialLinesChanged;
        metricItem.SubsequentLinesChanged = subsequentLinesChanged;
    }
}
