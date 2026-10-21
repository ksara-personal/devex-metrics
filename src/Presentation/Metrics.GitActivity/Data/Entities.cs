namespace Metrics.GitActivity.Data;

/// <summary>
/// A pull request of a repository. Unique per (Repository, Number).
/// </summary>
public sealed record PullRequestRecord
{
    public long Id { get; set; }
    public required string Repository { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }

    /// <summary>OPEN, CLOSED or MERGED.</summary>
    public required string State { get; set; }
    public bool IsDraft { get; set; }

    public string? AuthorLogin { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? MergedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }

    /// <summary>APPROVED, CHANGES_REQUESTED, REVIEW_REQUIRED or null.</summary>
    public string? ReviewDecision { get; set; }

    public int CommentsCount { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public int FilesChanged { get; set; }
    public int CommitsCount { get; set; }

    /// <summary>Status-check rollup of the head commit: SUCCESS, FAILURE, PENDING, ERROR, EXPECTED or null.</summary>
    public string? CiStatus { get; set; }

    /// <summary>First approve / changes-requested review by someone other than the author.</summary>
    public DateTime? ReviewSubmittedAt { get; set; }

    /// <summary>First review or comment of any kind by someone other than the author.</summary>
    public DateTime? FirstReviewAt { get; set; }

    /// <summary>Changes-requested reviews + 1 when the PR was reviewed at all.</summary>
    public int ReviewRounds { get; set; }

    public DateTime CollectedAt { get; set; }

    public List<PullRequestReviewRecord> Reviews { get; set; } = [];
    public List<PullRequestCommentRecord> Comments { get; set; } = [];
    public List<PullRequestReviewRequestRecord> ReviewRequests { get; set; } = [];
    public List<PullRequestLabelRecord> Labels { get; set; } = [];
    public List<PullRequestLinkedIssueRecord> LinkedIssues { get; set; } = [];
}

public sealed record PullRequestReviewRecord
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }
    public string? ReviewerLogin { get; set; }
    public string? ReviewerEmail { get; set; }

    /// <summary>APPROVED, CHANGES_REQUESTED, COMMENTED, DISMISSED or PENDING.</summary>
    public required string State { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int CommentsCount { get; set; }
}

/// <summary>A conversation (issue) comment on the PR.</summary>
public sealed record PullRequestCommentRecord
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }
    public string? AuthorLogin { get; set; }
    public string? AuthorEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A reviewer (user or team) that is still requested on the PR.</summary>
public sealed record PullRequestReviewRequestRecord
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }

    /// <summary>User login, or <c>team:slug</c> for team requests.</summary>
    public required string Reviewer { get; set; }
    public string? ReviewerEmail { get; set; }
}

public sealed record PullRequestLabelRecord
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }
    public required string Name { get; set; }
}

public sealed record PullRequestLinkedIssueRecord
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }

    /// <summary>GitHub issue number, or a Jira-style key parsed from title / branch / description.</summary>
    public required string Reference { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// A commit of a repository branch. Unique per (Repository, Hash).
/// </summary>
public sealed record CommitRecord
{
    public long Id { get; set; }
    public required string Repository { get; set; }
    public required string Branch { get; set; }
    public required string Hash { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public DateTime AuthorDate { get; set; }
    public string? CommitterName { get; set; }
    public string? CommitterEmail { get; set; }
    public DateTime CommitterDate { get; set; }
    public required string Subject { get; set; }
    public string? Body { get; set; }

    /// <summary>Space separated parent hashes (git <c>%P</c>).</summary>
    public string ParentHashes { get; set; } = string.Empty;
    public int ParentCount { get; set; }
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Collection phase a checkpoint belongs to.
/// </summary>
public enum CollectionPhase
{
    PullRequests,
    Commits
}

/// <summary>
/// Resume point for one phase of a (repository, window) collection. Written in the same
/// transaction as each page of data, so after a failure the next run continues from the
/// last stored page instead of the beginning.
/// </summary>
public class CollectionCheckpoint
{
    public long Id { get; set; }
    public required string Repository { get; set; }
    public CollectionPhase Phase { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }

    /// <summary>Commit branch; null for pull requests.</summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Pull requests: start of the created-date chunk being read (chunks move forward).
    /// Commits: upper commit-date bound being read (history is newest first).
    /// </summary>
    public DateTime Position { get; set; }

    /// <summary>GraphQL cursor of the last stored page within <see cref="Position"/>; null at a chunk boundary.</summary>
    public string? Cursor { get; set; }

    /// <summary>Created date (PR) or commit date (commit) of the last stored item. Fallback resume point when the cursor is rejected.</summary>
    public DateTime? LastItemAt { get; set; }

    /// <summary>PR number or commit hash of the last stored item.</summary>
    public string? LastItemKey { get; set; }

    public int ItemsStored { get; set; }
    public bool IsComplete { get; set; }
    public string? LastError { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// One collect invocation for a repository.
/// </summary>
public class CollectionRunRecord
{
    public long Id { get; set; }
    public required string Repository { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PullRequestCount { get; set; }
    public int CommitCount { get; set; }

    /// <summary>True when at least one phase continued from a checkpoint left by a failed run.</summary>
    public bool Resumed { get; set; }
    public string? Error { get; set; }
}
