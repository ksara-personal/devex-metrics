using System.Text.Json.Serialization;

namespace Metrics.GitHub;

// GraphQL response shapes for the repository-wide PR list and commit history queries
// (RepoPRListQuery.txt, RepoCommitHistoryQuery.txt, RepoDefaultBranchQuery.txt).

/// <summary>
/// A user or bot. <c>name</c>/<c>email</c> are only populated for users; email is empty unless public.
/// </summary>
public record GqlActor(string? login, string? name, string? email);

public record GqlNodes<T>(List<T>? nodes);
public record GqlCountedNodes<T>(int totalCount, List<T>? nodes);
public record GqlTotalCount(int totalCount);

public record GqlLabel(string name);
public record GqlLinkedIssue(int number, string url);

/// <summary>Requested reviewer is either a user (login) or a team (slug).</summary>
public record GqlRequestedReviewer(string? login, string? name, string? email, string? slug);
public record GqlReviewRequest(GqlRequestedReviewer? requestedReviewer);

public record GqlReview(GqlActor? author, string state, DateTime? submittedAt, GqlTotalCount? comments);
public record GqlIssueComment(GqlActor? author, DateTime createdAt);

public record GqlStatusCheckRollup(string state);
public record GqlLastCommitInner(GqlStatusCheckRollup? statusCheckRollup);
public record GqlLastCommit(GqlLastCommitInner commit);

public record GqlPullRequest(
    int number,
    string title,
    string url,
    string? bodyText,
    string state,
    bool isDraft,
    DateTime createdAt,
    DateTime? mergedAt,
    DateTime? closedAt,
    string baseRefName,
    string headRefName,
    int additions,
    int deletions,
    int changedFiles,
    int totalCommentsCount,
    string? reviewDecision,
    GqlActor? author,
    GqlCountedNodes<GqlLastCommit>? commits,
    GqlNodes<GqlLabel>? labels,
    GqlNodes<GqlLinkedIssue>? closingIssuesReferences,
    GqlNodes<GqlReviewRequest>? reviewRequests,
    GqlCountedNodes<GqlReview>? reviews,
    GqlCountedNodes<GqlIssueComment>? comments);

/// <summary>Search results, ordered by created date ascending.</summary>
public record GqlPRSearch(int issueCount, PageInfo pageInfo, List<GqlPullRequest> nodes);
public record GqlPRSearchData(GqlPRSearch? search);
public record GqlPRSearchRoot(GqlPRSearchData? data);

public record GqlGitActor(string? name, string? email, DateTime? date);
public record GqlCommitOid(string oid);

public record GqlCommit(
    string oid,
    string messageHeadline,
    string? messageBody,
    DateTime authoredDate,
    DateTime committedDate,
    GqlGitActor? author,
    GqlGitActor? committer,
    GqlNodes<GqlCommitOid>? parents);

public record GqlCommitHistory(PageInfo pageInfo, List<GqlCommit> nodes);
public record GqlCommitTarget(GqlCommitHistory? history);
public record GqlRef(string name, GqlCommitTarget? target);
public record GqlCommitHistoryRepository(string nameWithOwner, GqlRef? @ref);
public record GqlCommitHistoryData(GqlCommitHistoryRepository? repository);
public record GqlCommitHistoryRoot(GqlCommitHistoryData? data);

public record GqlBranchName(string name);
public record GqlDefaultBranchRepository(string nameWithOwner, GqlBranchName? defaultBranchRef);
public record GqlDefaultBranchData(GqlDefaultBranchRepository? repository);
public record GqlDefaultBranchRoot(GqlDefaultBranchData? data);

public record GqlSummary(string name, bool isArchived, bool isFork);
public record GqlSummaryConnection(PageInfo pageInfo, List<GqlSummary> nodes);
public record GqlOwner(string login, GqlSummaryConnection repositories);
public record GqlListData(GqlOwner? repositoryOwner);
public record GqlListRoot(GqlListData? data);

// Request variables.
public record PostRepoListVariables(string owner, string? after, int pageSize = 100) : PostVariablesWithOwner(owner);
public record PostRepoPRSearchVariables(string searchQuery, string? after, int pageSize = 25) : PostWithEmptyVariables;
public record PostRepoCommitHistoryVariables(string owner, string repo, string qualifiedName,
    string since, string until, string? after, int pageSize = 100) : PostVariables(owner, repo);

[JsonSerializable(typeof(PostWithVariables<PostVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostRepoPRSearchVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostRepoCommitHistoryVariables>))]
[JsonSerializable(typeof(GqlPRSearchRoot))]
[JsonSerializable(typeof(PostWithVariables<PostRepoListVariables>))]
[JsonSerializable(typeof(GqlListRoot))]
[JsonSerializable(typeof(GqlCommitHistoryRoot))]
[JsonSerializable(typeof(GqlDefaultBranchRoot))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
public partial class GitActivityGqlContext : JsonSerializerContext
{
}
