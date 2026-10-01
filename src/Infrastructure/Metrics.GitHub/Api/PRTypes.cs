using System;

namespace Metrics.GitHub;

/// <summary>
/// Represents a GitHub actor with a login name.
/// </summary>
/// <param name="login">The login name of the actor.</param>
public record Actor(string login);

/// <summary>
/// Represents a GitHub author with a login name.
/// </summary>
/// <param name="login">The login name of the author.</param>
public record Author(string login);

/// <summary>
/// Represents a GitHub author identified by name.
/// </summary>
/// <param name="name">The name of the author.</param>
public record AuthorByName(string name);

/// <summary>
/// Represents a short form of a commit with its abbreviated object ID.
/// </summary>
/// <param name="abbreviatedOid">The abbreviated object ID of the commit.</param>
public record CommitShort(string abbreviatedOid);

/// <summary>
/// Represents a commit with details such as message, date, stats, author, and parents.
/// </summary>
/// <param name="abbreviatedOid">The abbreviated object ID of the commit.</param>
/// <param name="committedDate">The date the commit was made.</param>
/// <param name="additions">The number of additions in the commit.</param>
/// <param name="deletions">The number of deletions in the commit.</param>
/// <param name="author">The author of the commit.</param>
/// <param name="parents">The parent commits.</param>
public record Commit(
 string abbreviatedOid,
 string messageHeadline,
 DateTime committedDate,
 int additions,
 int deletions,
 AuthorByName author,
 Parents parents
    );

/// <summary>
/// Represents the parent commits of a commit.
/// </summary>
/// <param name="totalCount">The total number of parent commits.</param>
/// <param name="nodes">The list of parent commit nodes.</param>
public record Parents(int totalCount);

/// <summary>
/// Represents a collection of commit nodes and their total count.
/// </summary>
/// <param name="totalCount">The total number of commits.</param>
/// <param name="pageInfo">The list of commit nodes.</param>
/// <param name="nodes">The list of commit nodes.</param>
public record Commits(int totalCount, PageInfo pageInfo, List<CommitNode> nodes);

/// <summary>
/// Represents the data wrapper for a pull request repository.
/// </summary>
/// <param name="repository">The repository containing the pull request.</param>
public record PRData(Repository repository);

/// <summary>
/// Represents a node containing a commit.
/// </summary>
/// <param name="commit">The commit object.</param>
public record CommitNode(Commit commit);
public record ReviewComment(int totalCount);

/// <summary>
/// Represents a pull request node with author, body, state, commit, and reviewer information.
/// </summary>
public record PRNode(
 Author author,
 OnBehalfOf onBehalfOf,
 ReviewComment comments,
 string body,
 string bodyText,
 string state,
 DateTime? submittedAt,
 DateTime updatedAt,
 CommitShort commit,
 string __typename,
 DateTime? createdAt,
 Actor actor,
 RequestedReviewer requestedReviewer
    );
public record OnBehalfOf(List<OnBehalfOfTeamNode> nodes);

public record OnBehalfOfTeamNode(string slug, string name);

public record RepositoryShort(string nameWithOwner, RepositoryOwner owner);
public record RepositoryOwner(string login);

/// <summary>
/// Represents a pull request with all its metadata, stats, comments, reviews, and timeline items.
/// </summary>
/// <param name="number">The pull request number.</param>
/// <param name="bodyText">The pull request body text.</param>
/// <param name="author">The author of the pull request.</param>
/// <param name="mergedBy">The user who merged the pull request.</param>
/// <param name="baseRefName">The base reference name.</param>
/// <param name="repository">The repository name.</param>
/// <param name="additions">The number of additions in the pull request.</param>
/// <param name="deletions">The number of deletions in the pull request.</param>
/// <param name="totalCommentsCount">The total number of comments on the pull request.</param>
/// <param name="changedFiles">The total number of files changed in the pull request.</param>
/// <param name="createdAt">The creation date of the pull request.</param>
/// <param name="updatedAt">The last updated date of the pull request.</param>
/// <param name="mergedAt">The date the pull request was merged.</param>
/// <param name="state">The current state of the pull request.</param>
/// <param name="title">The title of the pull request.</param>
/// <param name="url">The URL of the pull request.</param>
/// <param name="headRefName">The head reference name.</param>
/// <param name="comments">The comments on the pull request.</param>
/// <param name="reviews">The reviews for the pull request.</param>
/// <param name="timelineItems">The timeline items of the pull request.</param>
/// <param name="commits">The commits associated with the pull request.</param>
public record PullRequest(
 int number,
 string bodyText,
 Author author,
 Author mergedBy,
 string baseRefName,
 RepositoryShort repository,
 int additions,
 int deletions,
 int totalCommentsCount,
 int changedFiles,
 DateTime createdAt,
 DateTime updatedAt,
 DateTime? mergedAt,
 string state,
 string title,
 string url,
 string headRefName,
 Comments comments,
 Reviews reviews,
 TimelineItems timelineItems,
 Commits commits
    )
{
    internal IList<(DateTime start, DateTime end)> DraftTransitions { get; set; } = null;
    internal PRNode? CopilotReviewNode { get; set; } = null;
}

/// <summary>
/// Represents a repository containing a pull request.
/// </summary>
/// <param name="pullRequest">The pull request object.</param>
public record Repository(PullRequest pullRequest);

/// <summary>
/// Represents a requested reviewer for a pull request.
/// </summary>
/// <param name="__typename">The type name of the reviewer.</param>
public record RequestedReviewer(string __typename, string name, string login, string slug);

/// <summary>
/// Represents a collection of reviews for a pull request.
/// </summary>
/// <param name="totalCount">The total number of reviews.</param>
/// <param name="pageInfo">The pagination information.</param>
/// <param name="nodes">The list of review nodes.</param>
public record Reviews(int totalCount, PageInfo pageInfo, List<PRNode> nodes);

/// <summary>
/// Represents the root object for a GitHub pull request API response.
/// </summary>
/// <param name="data">The data object containing pull request information.</param>
public record GitHubPRRoot(PRData data) : PullRequestData();

/// <summary>
/// Represents a collection of timeline items for a pull request.
/// </summary>
/// <param name="totalCount"></param>
/// <param name="pageInfo"></param>
/// <param name="nodes"></param>
/// <returns></returns>
public record TimelineItems(int totalCount, PageInfo pageInfo, List<PRNode> nodes);

/// <summary>
/// Represents a collection of comments for a pull request.
/// </summary>
/// <param name="totalCount">The total number of comments.</param>
/// <param name="pageInfo">The pagination information.</param>
/// <param name="nodes">The list of comment nodes.</param>
public record Comments(int totalCount, PageInfo pageInfo, List<PRNode> nodes);

