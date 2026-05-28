using Metrics.GitHub.Extensions;
using Metrics.Models;

namespace Metrics.GitHub.ReviewerMetrics;

/// <summary>
/// Tracks review metrics for a pull request.
/// </summary>
/// <summary>
public sealed class PRReviewMetricsTracker : IDisposable
{
    PullRequest _pullRequest;
    string _prAuthor;

    Dictionary<string, TimeSlicedReviewerMetrics> _reviewerMetrics = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PRReviewMetricsTracker"/> class.
    /// </summary>
    /// <param name="pullRequest"></param>
    public PRReviewMetricsTracker(PullRequest pullRequest)
    {
        _pullRequest = pullRequest;
        _prAuthor = pullRequest.author?.login;
    }

    /// <summary>
    /// Aggregates the metrics for the pull request.
    /// </summary>
    public Dictionary<string, TimeSlicedReviewerMetrics> AggregateMetrics()
    {
        // remove request removed events from timeline.
        SanitizeTimelineItems();
        var nodes = CombineTimelineItemsAndReviewsAndSort();
        foreach (var node in nodes)
        {
            if (node.__typename == GitHubTimelineStates.ReviewRequestedEvent)
            {
                switch (node.requestedReviewer?.__typename)
                {
                    case GitHubRequestedReviewerTypes.User:
                        HandleTimelineByUser(node);
                        break;
                    case GitHubRequestedReviewerTypes.Team:
                        HandleTimelineByTeam(node);
                        break;
                    default:
                        break;
                }
            }
            else if (string.IsNullOrEmpty(node.__typename))
            {
                var reviewer = node.author?.login;
                if (ExcludeUser(reviewer))
                    continue;

                var reviewerMetrics = GetOrCreateReviewerMetrics(reviewer);
                reviewerMetrics.HandleReview(node);
            }
        }
        
        // handle comments
        var comments = _pullRequest.comments.nodes;
        foreach (var comment in comments)
        {
            var reviewer = comment.author?.login;
            if (ExcludeUser(reviewer))
                continue;

            var reviewerMetrics = GetOrCreateReviewerMetrics(reviewer);
            reviewerMetrics.HandleComment(comment);
        }
        return _reviewerMetrics;
    }

    /// <summary>
    /// Sanitizes the timeline items by removing any review request events that have been removed.
    /// </summary>
    void SanitizeTimelineItems()
    {
        var timelineItems = _pullRequest.timelineItems.nodes;
        var removedEvents = timelineItems.Where(n => n.__typename == GitHubTimelineStates.ReviewRequestRemovedEvent).OrderBy(n => n.createdAt).ToList();
        var reviews = _pullRequest.reviews.nodes;
        List<PRNode> toRemoveNodes = new();
        foreach (var node in timelineItems)
        {
            if (node.__typename == GitHubTimelineStates.ReviewRequestedEvent)
            {
                switch (node.requestedReviewer?.__typename)
                {
                    case GitHubRequestedReviewerTypes.User:
                        HandleUserReviewRequestRemoval(node, removedEvents, toRemoveNodes);
                        break;
                    case GitHubRequestedReviewerTypes.Team:
                        HandleTeamReviewRequestRemoval(node, removedEvents, toRemoveNodes);
                        break;
                    default:
                        break;
                }
            }
        }

        foreach (var removeNode in toRemoveNodes)
        {
            timelineItems.Remove(removeNode);
        }
    }

    /// <summary>
    /// Handles the removal of a user review request.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="removedEvents"></param>
    /// <param name="toRemoveNodes"></param>
    void HandleUserReviewRequestRemoval(PRNode node, List<PRNode> removedEvents, List<PRNode> toRemoveNodes)
    {
        var requestedBy = node.requestedReviewer?.login;
        var removedEvent = removedEvents.FirstOrDefault(e => e.requestedReviewer?.login == requestedBy && e.createdAt.Value >= node.createdAt.Value);
        if (removedEvent is not null)
        {
            toRemoveNodes.Add(node);
            toRemoveNodes.Add(removedEvent);
        }
    }
    
    /// <summary>
    /// Handles the removal of a team review request.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="removedEvents"></param>
    /// <param name="toRemoveNodes"></param>
    void HandleTeamReviewRequestRemoval(PRNode node, List<PRNode> removedEvents, List<PRNode> toRemoveNodes)
    {
        var reviews = _pullRequest.reviews.nodes;
        var teamName = node.requestedReviewer?.slug;
        var requestedByReview = reviews.FirstOrDefault(r => r.onBehalfOf.nodes.Any(m => m.slug == teamName));
        var removedEvent = removedEvents.FirstOrDefault(e => e.requestedReviewer?.login == requestedByReview?.author?.login && e.createdAt.Value >= node.createdAt.Value);
        if (removedEvent is not null)
        {
            toRemoveNodes.Add(node);
            toRemoveNodes.Add(removedEvent);
        }
        else
        {
            // before a review, it could have been removed.
            removedEvent = removedEvents.FirstOrDefault(e => e.requestedReviewer?.slug == teamName && e.createdAt.Value >= node.createdAt.Value);
            if (removedEvent is not null)
            {
                toRemoveNodes.Add(node);
                toRemoveNodes.Add(removedEvent);
            }
        }
    }

    /// <summary>
    /// Combines the timeline items and reviews for the pull request and sorts them by creation date.
    /// </summary>
    /// <returns></returns>
    List<PRNode> CombineTimelineItemsAndReviewsAndSort()
    {
        var combinedItems = new List<PRNode>();
        combinedItems.AddRange(_pullRequest.timelineItems.nodes);
        combinedItems.AddRange(_pullRequest.reviews.nodes);
        combinedItems.Sort((a, b) => a.createdAt.Value.CompareTo(b.createdAt.Value));
        return combinedItems;
    }

    /// <summary>
    /// Handles the timeline item for a user.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool HandleTimelineByUser(PRNode node) => HandleTimeline(node, () => node.requestedReviewer?.login);

    /// <summary>
    /// Handles the timeline item for a user or team
    /// </summary>
    /// <param name="node"></param>
    /// <param name="removedEvents"></param>
    /// <param name="removedEventSelector"></param>
    /// <param name="requestedBySelector"></param>
    /// <returns></returns>
    bool HandleTimeline(PRNode node, Func<string> requestedBySelector)
    {
        var requestedBy = requestedBySelector();
        if (ExcludeUser(requestedBy))
            return false;

        var reviewerMetrics = GetOrCreateReviewerMetrics(requestedBy);
        reviewerMetrics.HandleReviewRequested(node);
        return true;
    }

    /// <summary>
    /// Handles the timeline item for a team.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool HandleTimelineByTeam(PRNode node)
    {
        return HandleTimeline(node, () =>
        {
            var teamName = node.requestedReviewer?.slug;
            var reviews = _pullRequest.reviews.nodes;
            var requestedByReview = reviews.FirstOrDefault(r => r.onBehalfOf.nodes.Any(m => m.slug == teamName));
            return requestedByReview?.author?.login;
        });
    }

    /// <summary>
    /// Determines whether the specified author should be excluded from metrics.
    /// </summary>
    /// <param name="author"></param>
    /// <returns></returns>
    bool ExcludeUser(string author) => string.IsNullOrEmpty(author) || string.Equals(author, _prAuthor, StringComparison.OrdinalIgnoreCase) || GitHubAuthors.IsBotOrAI(author);

    /// <summary>
    /// Gets or creates the user metrics for the specified author.
    /// </summary>
    /// <param name="author"></param>
    /// <returns></returns>
    TimeSlicedReviewerMetrics GetOrCreateReviewerMetrics(string author)
    {
        if (!_reviewerMetrics.TryGetValue(author, out var metrics))
        {
            metrics = new TimeSlicedReviewerMetrics(author);
            _reviewerMetrics.Add(author, metrics);
        }
        return metrics;
    }

    /// <summary>
    /// Disposes the resources used by the PRReviewMetricsTracker.
    /// </summary>
    public void Dispose()
    {
        _reviewerMetrics.Clear();
        _pullRequest = null!;
        _reviewerMetrics = null!;
    }
}

/// <summary>
/// Represents the time sliced reviewer metrics.
/// </summary>
/// </summary>
/// <value></value>
public sealed record TimeSlicedReviewerMetrics
{
    List<TimeSpan> _responseTimes = new();
    PRNode requestedReviewEvent = null!;
    public string Reviewer { get; set; }
    public Dictionary<DateOnly, ReviewerDailyMetrics> ReviewerMetrics { get; set; } = new();

    public TimeSlicedReviewerMetrics(string reviewer) => Reviewer = reviewer;

    /// <summary>
    /// Gets or creates the reviewer metrics for the specified author.
    /// </summary>
    /// <param name="author"></param>
    /// <returns></returns>
    ReviewerDailyMetrics GetOrCreateReviewerMetrics(DateOnly date)
    {
        if (!ReviewerMetrics.TryGetValue(date, out var metrics))
        {
            metrics = new ReviewerDailyMetrics(Reviewer)
            {
                Date = date
            };
            ReviewerMetrics.Add(date, metrics);
        }
        return metrics;
    }

    /// <summary>
    /// Handles the review requested event for the reviewer.
    /// </summary>
    /// <param name="node"></param>
    public void HandleReviewRequested(PRNode node)
    {
        requestedReviewEvent = node;
        var timeMetric = GetOrCreateReviewerMetrics(DateOnly.FromDateTime(node.createdAt.Value));
        timeMetric.HandleReviewRequested(node);
    }

    /// <summary>
    /// Handles the comment event for the reviewer.
    /// </summary>
    /// <param name="comment"></param>
    public void HandleComment(PRNode comment)
    {
        var timeMetric = GetOrCreateReviewerMetrics(DateOnly.FromDateTime(comment.createdAt.Value));
        timeMetric.HandleComment(comment);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="review"></param>
    /// <param name="nodes"></param>
    public void HandleReview(PRNode review)
    {
        var timeMetric = GetOrCreateReviewerMetrics(DateOnly.FromDateTime(review.submittedAt.Value));
        timeMetric.HandleReview(review, requestedReviewEvent);
        requestedReviewEvent = null;
    }
}
