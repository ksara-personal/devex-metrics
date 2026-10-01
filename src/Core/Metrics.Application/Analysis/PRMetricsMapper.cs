
namespace Metrics.Application;

/// <summary>
/// Static class for mapping PRMetrics objects, including contributors, reviewer metrics, copilot metrics, and DevEx metrics.
/// Provides a generic helper for merging collections.
/// </summary>
static class PRMetricsMapper
{
    /// <summary>
    /// Converts a PRMetrics object to a different type.
    /// </summary>
    /// <param name="prMetrics"></param>
    /// <param name="includeId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T CopyTo<T>(this PRMetrics src, T target = default, bool includeId = true, bool includeRelationships = true) where T : PRMetrics, new()
    {
        if (src is null)
            throw new ArgumentNullException(nameof(src));

        var result = target ?? new T();
        result.PrNumber = src.PrNumber;
        result.Author = src.Author;
        result.MergedBy = src.MergedBy;
        result.Repository = src.Repository;
        result.BaseBranch = src.BaseBranch;
        result.State = src.State;
        result.MergedAt = src.MergedAt;
        result.CreatedAt = src.CreatedAt;
        result.UpdatedAt = src.UpdatedAt;
        result.TotalComments = src.TotalComments;
        result.TotalLines = src.TotalLines;
        result.IsFeature = src.IsFeature;
        result.DraftTransitions = src.DraftTransitions;
        result.TotalCommits = src.TotalCommits;
        result.ChangedFiles = src.ChangedFiles;
        result.WorkItemId = src.WorkItemId;
        result.WorkItemId2 = src.WorkItemId2;

        if (includeId)
            result.Id = src.Id;
            
        if (includeRelationships)
        {
            result.Contributors = src.Contributors;
            result.ReviewerDailyMetrics = src.ReviewerDailyMetrics;
            result.CopilotReviewMetrics = src.CopilotReviewMetrics;
            result.Team = src.Team;
            result.TeamId = src.TeamId;
            result.DevExMetricItem = src.DevExMetricItem;
        }

        return result;
    }
    
    /// <summary>
    /// Maps all relevant fields and collections from the source PRMetrics to the target PRMetrics.
    /// Copies direct fields, contributors, copilot review metrics, reviewer metrics, and DevEx metrics.
    /// Uses UpsertCollection to merge collections by key and update or add items as needed.
    /// </summary>
    /// <param name="src">Source PRMetrics object</param>
    /// <param name="target">Target PRMetrics object</param>
    public static void Map(PRMetrics src, PRMetrics target)
    {
        target = src.CopyTo<PRMetrics>(target, false, false);

        // Map contributors: add new or update existing, and set PRMetric reference.
        UpsertCollection(src.Contributors, target.Contributors, target,
            (t, s) => string.Equals(t.Contributor, s.Contributor, StringComparison.OrdinalIgnoreCase),
            (targetContributor, srcContributor) =>
            {
                targetContributor.Commits = srcContributor.Commits;
                targetContributor.LOC = srcContributor.LOC;
            });

        // Map Copilot review metrics: add or update, and set PRMetric reference.
        var targetCopilotMetrics = target.CopilotReviewMetrics;
        if (targetCopilotMetrics is null)
        {
            target.CopilotReviewMetrics = src.CopilotReviewMetrics;
            if (target.CopilotReviewMetrics is not null)
                target.CopilotReviewMetrics.PRMetric = target;
        }
        else
        {
            targetCopilotMetrics.Comments = src.CopilotReviewMetrics.Comments;
            targetCopilotMetrics.FilesChanged = src.CopilotReviewMetrics.FilesChanged;
            targetCopilotMetrics.FilesReviewed = src.CopilotReviewMetrics.FilesReviewed;
        }
        var targetDevExMetrics = target.DevExMetricItem;
        if (targetDevExMetrics is null)
        {
            target.DevExMetricItem = src.DevExMetricItem;
            if (target.DevExMetricItem is not null)
                target.DevExMetricItem.PRMetric = target;
        }
        else
        {
            var srcDevExMetrics = src.DevExMetricItem;
            if (srcDevExMetrics is not null)
            {
                targetDevExMetrics.ApproveTime = srcDevExMetrics.ApproveTime;
                targetDevExMetrics.TotalReviewChangesRequested = srcDevExMetrics.TotalReviewChangesRequested;
                targetDevExMetrics.CodeExcellenceRequestedChanges = srcDevExMetrics.CodeExcellenceRequestedChanges;
                targetDevExMetrics.TeamRequestedChanges = srcDevExMetrics.TeamRequestedChanges;
                targetDevExMetrics.OthersRequestedChanges = srcDevExMetrics.OthersRequestedChanges;
                targetDevExMetrics.CodingTime = srcDevExMetrics.CodingTime;
                targetDevExMetrics.TotalSmallCommits = srcDevExMetrics.TotalSmallCommits;
                targetDevExMetrics.TotalSmallCommitComments = srcDevExMetrics.TotalSmallCommitComments;
                targetDevExMetrics.TotalMediumCommits = srcDevExMetrics.TotalMediumCommits;
                targetDevExMetrics.TotalMediumCommitComments = srcDevExMetrics.TotalMediumCommitComments;
                targetDevExMetrics.TotalLargeCommits = srcDevExMetrics.TotalLargeCommits;
                targetDevExMetrics.TotalLargeCommitComments = srcDevExMetrics.TotalLargeCommitComments;
                targetDevExMetrics.CycleTime = srcDevExMetrics.CycleTime;
                targetDevExMetrics.LeadTime = srcDevExMetrics.LeadTime;
                targetDevExMetrics.MaturityPercentage = srcDevExMetrics.MaturityPercentage;
                targetDevExMetrics.InitialLinesChanged = srcDevExMetrics.InitialLinesChanged;
                targetDevExMetrics.SubsequentLinesChanged = srcDevExMetrics.SubsequentLinesChanged;
                targetDevExMetrics.MergeTime = srcDevExMetrics.MergeTime;
                targetDevExMetrics.PickupTime = srcDevExMetrics.PickupTime;
                targetDevExMetrics.TotalReviewCommentsAfterFinalApproval = srcDevExMetrics.TotalReviewCommentsAfterFinalApproval;
                targetDevExMetrics.ReviewTime = srcDevExMetrics.ReviewTime;
                targetDevExMetrics.PRSize = srcDevExMetrics.PRSize;
                targetDevExMetrics.AvgReviewCommentsPerCommit = srcDevExMetrics.AvgReviewCommentsPerCommit;
                targetDevExMetrics.TotalReviewComments = srcDevExMetrics.TotalReviewComments;
            }
        }
        
        // Map reviewer metrics: add new or update existing, and set PRMetric reference.
        UpsertCollection(src.ReviewerDailyMetrics, target.ReviewerDailyMetrics, target,
        (t, s) => string.Equals(t.Reviewer, s.Reviewer, StringComparison.OrdinalIgnoreCase) && t.Date == s.Date,
        (targetMetrics, srcMetrics) =>
        {
            targetMetrics.Approved = srcMetrics.Approved;
            targetMetrics.ChangesRequested = srcMetrics.ChangesRequested;
            targetMetrics.ReviewComments = srcMetrics.ReviewComments;
            targetMetrics.AverageResponseTimeHours = srcMetrics.AverageResponseTimeHours;
            targetMetrics.CommentCount = srcMetrics.CommentCount;
            targetMetrics.ReviewsRequested = srcMetrics.ReviewsRequested;
            targetMetrics.ReviewsSubmitted = srcMetrics.ReviewsSubmitted;
            targetMetrics.Date = srcMetrics.Date;
            targetMetrics.Reviewer = srcMetrics.Reviewer;
        });
    }

    /// <summary>
    /// Merges items from the source collection into the target collection by key.
    /// If an item exists, updates it using the provided updateAction; otherwise, adds it and sets PRMetric reference.
    /// </summary>
    /// <typeparam name="T">Type of collection item, must inherit PRMetricRelationship</typeparam>
    /// <param name="srcCol">Source collection</param>
    /// <param name="targetCol">Target collection</param>
    /// <param name="target">Target PRMetrics object for reference</param>
    /// <param name="targetSelector">Function to match items by key</param>
    /// <param name="updateAction">Action to update existing items</param>
    internal static void UpsertCollection<T>(ICollection<T> srcCol,
        ICollection<T> targetCol,
        PRMetrics target,
        Func<T, T, bool> targetSelector,
        Action<T, T> updateAction) where T : PRMetricRelationship
    {
        foreach (var item in srcCol)
        {
            var targetMetrics = targetCol.FirstOrDefault(c => targetSelector(c, item));
            if (targetMetrics is null)
            {
                targetCol.Add(item);
                item.PRMetric = target;
            }
            else
            {
                updateAction(targetMetrics, item);
            }
        }
    }
}
