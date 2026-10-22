using System.Text.RegularExpressions;
using Metrics.GitHub;
using Metrics.GitActivity.Data;

namespace Metrics.GitActivity.Collection;

/// <summary>
/// Maps GitHub GraphQL responses to the stored records and derives the review timings.
/// </summary>
public partial class GitHubActivityMapper(IReadOnlyDictionary<string, string> userEmails)
{
    [GeneratedRegex(@"\b[A-Z][A-Z0-9]+-\d+\b")]
    private static partial Regex TicketKeyRegex();

    /// <summary>
    /// Email for a login: configured mapping first, then the public GitHub email.
    /// </summary>
    public string? ResolveEmail(string? login, string? githubEmail)
    {
        if (!string.IsNullOrEmpty(login) && userEmails.TryGetValue(login, out var mapped))
            return mapped;
        return string.IsNullOrWhiteSpace(githubEmail) ? null : githubEmail;
    }

    public PullRequestRecord Map(string repository, GqlPullRequest pr, DateTime collectedAt)
    {
        var authorLogin = pr.author?.login;
        var reviews = (pr.reviews?.nodes ?? []).Where(r => r.state != "PENDING").ToList();
        var comments = pr.comments?.nodes ?? [];

        bool IsOther(GqlActor? actor) => actor?.login is not null && !string.Equals(actor.login, authorLogin, StringComparison.OrdinalIgnoreCase);

        var otherReviews = reviews.Where(r => IsOther(r.author) && r.submittedAt is not null).ToList();
        var formalReviewAt = otherReviews
            .Where(r => r.state is "APPROVED" or "CHANGES_REQUESTED")
            .Min(r => r.submittedAt);
        var firstActivityAt = otherReviews.Select(r => r.submittedAt)
            .Concat(comments.Where(c => IsOther(c.author)).Select(c => (DateTime?)c.createdAt))
            .Min();
        var changesRequested = otherReviews.Count(r => r.state == "CHANGES_REQUESTED");

        var record = new PullRequestRecord
        {
            Repository = repository,
            Number = pr.number,
            Title = pr.title,
            Url = pr.url,
            Description = pr.bodyText,
            State = pr.state,
            IsDraft = pr.isDraft,
            AuthorLogin = authorLogin,
            AuthorName = pr.author?.name,
            AuthorEmail = ResolveEmail(authorLogin, pr.author?.email),
            CreatedAt = pr.createdAt,
            MergedAt = pr.mergedAt,
            ClosedAt = pr.closedAt,
            SourceBranch = pr.headRefName,
            TargetBranch = pr.baseRefName,
            ReviewDecision = pr.reviewDecision,
            CommentsCount = pr.totalCommentsCount,
            LinesAdded = pr.additions,
            LinesDeleted = pr.deletions,
            FilesChanged = pr.changedFiles,
            CommitsCount = pr.commits?.totalCount ?? 0,
            CiStatus = pr.commits?.nodes?.LastOrDefault()?.commit.statusCheckRollup?.state,
            ReviewSubmittedAt = formalReviewAt,
            FirstReviewAt = firstActivityAt,
            ReviewRounds = otherReviews.Count == 0 ? 0 : changesRequested + 1,
            CollectedAt = collectedAt,
            Reviews = reviews.Select(r => new PullRequestReviewRecord
            {
                ReviewerLogin = r.author?.login,
                ReviewerEmail = ResolveEmail(r.author?.login, r.author?.email),
                State = r.state,
                SubmittedAt = r.submittedAt,
                CommentsCount = r.comments?.totalCount ?? 0
            }).ToList(),
            Comments = comments.Select(c => new PullRequestCommentRecord
            {
                AuthorLogin = c.author?.login,
                AuthorEmail = ResolveEmail(c.author?.login, c.author?.email),
                CreatedAt = c.createdAt
            }).ToList(),
            ReviewRequests = (pr.reviewRequests?.nodes ?? [])
                .Select(r => r.requestedReviewer)
                .Where(r => r is not null && (r.login is not null || r.slug is not null))
                .Select(r => new PullRequestReviewRequestRecord
                {
                    Reviewer = r!.login ?? $"team:{r.slug}",
                    ReviewerEmail = r.login is null ? null : ResolveEmail(r.login, r.email)
                }).ToList(),
            Labels = (pr.labels?.nodes ?? []).Select(l => new PullRequestLabelRecord { Name = l.name }).ToList(),
            LinkedIssues = LinkedIssues(pr).ToList()
        };
        return record;
    }

    /// <summary>
    /// GitHub closing-issue references plus ticket keys (e.g. PROJ-123) found in the title, branch and description.
    /// </summary>
    static IEnumerable<PullRequestLinkedIssueRecord> LinkedIssues(GqlPullRequest pr)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var issue in pr.closingIssuesReferences?.nodes ?? [])
        {
            var key = $"#{issue.number}";
            if (seen.Add(key))
                yield return new PullRequestLinkedIssueRecord { Reference = key, Url = issue.url };
        }

        foreach (var text in new[] { pr.title, pr.headRefName, pr.bodyText })
        {
            if (string.IsNullOrEmpty(text))
                continue;
            foreach (Match match in TicketKeyRegex().Matches(text))
            {
                if (seen.Add(match.Value))
                    yield return new PullRequestLinkedIssueRecord { Reference = match.Value };
            }
        }
    }

    public CommitRecord Map(string repository, string branch, GqlCommit commit, DateTime collectedAt)
    {
        var parents = (commit.parents?.nodes ?? []).Select(p => p.oid).ToList();
        return new CommitRecord
        {
            Repository = repository,
            Branch = branch,
            Hash = commit.oid,
            AuthorName = commit.author?.name,
            AuthorEmail = commit.author?.email,
            AuthorDate = commit.author?.date ?? commit.authoredDate,
            CommitterName = commit.committer?.name,
            CommitterEmail = commit.committer?.email,
            CommitterDate = commit.committer?.date ?? commit.committedDate,
            Subject = commit.messageHeadline,
            Body = string.IsNullOrWhiteSpace(commit.messageBody) ? null : commit.messageBody,
            ParentHashes = string.Join(' ', parents),
            ParentCount = parents.Count,
            CollectedAt = collectedAt
        };
    }
}
