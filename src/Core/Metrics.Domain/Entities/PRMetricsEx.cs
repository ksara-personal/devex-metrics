using System.Collections.Concurrent;

namespace Metrics.Domain;

/// <summary>
/// Flattened PR Metrics model that extends PRMetrics and exposes DevExEffciencyMetrics dictionary values as individual properties.
/// This model is used for OData exposure where the dictionary values are projected as separate columns.
/// DevExMetrics collection is excluded from this model.
/// </summary>
public sealed class PRMetricsEx : PRMetrics
{
    // DevEx Efficiency Metrics flattened as individual properties using MetricNames constants

    /// <summary>
    /// Approve time duration.
    /// </summary>
    public string? ApproveTime { get; set; }

    /// <summary>
    /// Total review changes requested count.
    /// </summary>
    public int? TotalReviewChangesRequested { get; set; }

    /// <summary>
    /// Code excellence requested changes count.
    /// </summary>
    public int? CodeExcellenceRequestedChanges { get; set; }

    /// <summary>
    /// Team requested changes count.
    /// </summary>
    public int? TeamRequestedChanges { get; set; }

    /// <summary>
    /// Others requested changes count.
    /// </summary>
    public int? OthersRequestedChanges { get; set; }

    /// <summary>
    /// Coding time duration.
    /// </summary>
    public string? CodingTime { get; set; }

    /// <summary>
    /// Total small commits count.
    /// </summary>
    public int? TotalSmallCommits { get; set; }

    /// <summary>
    /// Total small commit comments count.
    /// </summary>
    public int? TotalSmallCommitComments { get; set; }

    /// <summary>
    /// Total medium commits count.
    /// </summary>
    public int? TotalMediumCommits { get; set; }

    /// <summary>
    /// Total medium commit comments count.
    /// </summary>
    public int? TotalMediumCommitComments { get; set; }

    /// <summary>
    /// Total large commits count.
    /// </summary>
    public int? TotalLargeCommits { get; set; }

    /// <summary>
    /// Total large commit comments count.
    /// </summary>
    public int? TotalLargeCommitComments { get; set; }

    /// <summary>
    /// Cycle time duration.
    /// </summary>
    public string? CycleTime { get; set; }

    /// <summary>
    /// Lead time duration.
    /// </summary>
    public string? LeadTime { get; set; }

    /// <summary>
    /// Maturity percentage.
    /// </summary>
    public double? MaturityPercentage { get; set; }

    /// <summary>
    /// Initial lines changed count.
    /// </summary>
    public int? InitialLinesChanged { get; set; }

    /// <summary>
    /// Subsequent lines changed count.
    /// </summary>
    public int? SubsequentLinesChanged { get; set; }

    /// <summary>
    /// Merge time duration.
    /// </summary>
    public string? MergeTime { get; set; }

    /// <summary>
    /// Pickup time duration.
    /// </summary>
    public string? PickupTime { get; set; }

    /// <summary>
    /// Total review comments after final approval count.
    /// </summary>
    public int? TotalReviewCommentsAfterFinalApproval { get; set; }

    /// <summary>
    /// Review time duration.
    /// </summary>
    public string? ReviewTime { get; set; }

    /// <summary>
    /// PR size metric.
    /// </summary>
    public string? PRSize { get; set; }

    /// <summary>
    /// Average review comments per commit.
    /// </summary>
    public double? AvgReviewCommentsPerCommit { get; set; }

    /// <summary>
    /// Total review comments count.
    /// </summary>
    public int? TotalReviewComments { get; set; }
}
