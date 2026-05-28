using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Metrics.Models;

namespace Metrics;

/// <summary>
/// Extensions for PRMetrics
/// </summary>
public static class PRMetricsExtensions
{
    /// <summary>
    /// Creates a flattened PR metrics object from a PRMetrics entity.
    /// Copies the base PRMetrics data and maps the DevExMetrics directly to properties.
    /// </summary>
    /// <param name="prMetrics">The source PRMetrics entity</param>
    /// <returns>A flattened PRMetricsEx object</returns>
    public static PRMetricsEx ToPRMetricsEx(this PRMetrics prMetrics)
    {
        var flattened = prMetrics.CopyTo<PRMetricsEx>();
        var metricItem = prMetrics.DevExMetricItem;
        if (metricItem is null)
        {
            return flattened;
        }

        flattened.ApproveTime = Convert.ToString(metricItem.ApproveTime);
        flattened.TotalReviewChangesRequested = metricItem.TotalReviewChangesRequested;
        flattened.CodeExcellenceRequestedChanges = metricItem.CodeExcellenceRequestedChanges;
        flattened.TeamRequestedChanges = metricItem.TeamRequestedChanges;
        flattened.OthersRequestedChanges = metricItem.OthersRequestedChanges;
        flattened.CodingTime = Convert.ToString(metricItem.CodingTime);
        flattened.TotalSmallCommits = metricItem.TotalSmallCommits;
        flattened.TotalSmallCommitComments = metricItem.TotalSmallCommitComments;
        flattened.TotalMediumCommits = metricItem.TotalMediumCommits;
        flattened.TotalMediumCommitComments = metricItem.TotalMediumCommitComments;
        flattened.TotalLargeCommits = metricItem.TotalLargeCommits;
        flattened.TotalLargeCommitComments = metricItem.TotalLargeCommitComments;
        flattened.CycleTime = Convert.ToString(metricItem.CycleTime);
        flattened.LeadTime = Convert.ToString(metricItem.LeadTime);
        flattened.MaturityPercentage = metricItem.MaturityPercentage;
        flattened.InitialLinesChanged = metricItem.InitialLinesChanged;
        flattened.SubsequentLinesChanged = metricItem.SubsequentLinesChanged;
        flattened.MergeTime = Convert.ToString(metricItem.MergeTime);
        flattened.PickupTime = Convert.ToString(metricItem.PickupTime);
        flattened.TotalReviewCommentsAfterFinalApproval = metricItem.TotalReviewCommentsAfterFinalApproval;
        flattened.ReviewTime = Convert.ToString(metricItem.ReviewTime);
        flattened.PRSize = metricItem.PRSize;
        flattened.AvgReviewCommentsPerCommit = metricItem.AvgReviewCommentsPerCommit;
        flattened.TotalReviewComments = metricItem.TotalReviewComments;

        return flattened;
    }
}
