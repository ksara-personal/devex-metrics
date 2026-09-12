
namespace Metrics.GitHub.Extensions;

/// <summary>
/// 
/// </summary>
internal static class ReviewerDailyMetricsExtensions
{
    /// <summary>
    /// Handles the comment event
    /// </summary>
    /// <param name="comment"></param>
    public static void HandleComment(this ReviewerDailyMetrics metrics, PRNode comment) => metrics.CommentCount = metrics.CommentCount.GetValueOrDefault(0) + 1;

    /// <summary>
    /// Handles the review requested event
    /// </summary>
    /// <param name="node"></param>
    public static void HandleReviewRequested(this ReviewerDailyMetrics metrics, PRNode node) => metrics.ReviewsRequested = metrics.ReviewsRequested.GetValueOrDefault(0) + 1;

    /// <summary>
    /// Handles the review by author
    /// </summary>
    /// <param name="review"></param>
    public static void HandleReview(this ReviewerDailyMetrics metrics, PRNode review, PRNode requestedReviewEvent)
    {
        var count = review.comments.totalCount;
        if (review.state == GitHubStates.Approved || review.state == GitHubStates.ChangesRequested)
        {
            var _ = review.state switch
            {
                GitHubStates.Approved => metrics.Approved = metrics.Approved.GetValueOrDefault(0) + 1,
                GitHubStates.ChangesRequested => metrics.ChangesRequested = metrics.ChangesRequested.GetValueOrDefault(0) + 1,
                _ => 0
            };
            metrics.ReviewsSubmitted = metrics.ReviewsSubmitted.GetValueOrDefault(0) + 1;
            metrics.CommentCount = metrics.CommentCount.GetValueOrDefault(0) + count;
            if (requestedReviewEvent != null && review.createdAt.HasValue && requestedReviewEvent.createdAt.HasValue)
            {
                var responseTime = review.submittedAt.Value.GetWeekdayTimeSpan(requestedReviewEvent.createdAt.Value);
                metrics.ResponseTimes.Add(responseTime);
                metrics.AverageResponseTimeHours = (float)metrics.ResponseTimes.Average(a => a.TotalHours);
            }
        }
        else if (review.state == GitHubStates.Commented)
        {
            metrics.CommentCount = metrics.CommentCount.GetValueOrDefault(0) + count;
            metrics.ReviewComments = metrics.ReviewComments.GetValueOrDefault(0) + count;
        }
    }
}
