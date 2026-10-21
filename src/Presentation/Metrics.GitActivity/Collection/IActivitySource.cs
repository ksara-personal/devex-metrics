using Metrics.GitHub;

namespace Metrics.GitActivity.Collection;

/// <summary>
/// Paged GitHub reads used by <see cref="ActivityCollector"/>. Abstracted so resume and rate-limit
/// handling can be tested without the network.
/// </summary>
public interface IActivitySource
{
    /// <summary>PRs created in <c>[start, end]</c> (inclusive), oldest first.</summary>
    Task<GqlPRSearch?> SearchPullRequestsAsync(string owner, string repo, DateTime start, DateTime end, string? cursor, CancellationToken cancellationToken);

    /// <summary>Commits of <paramref name="branch"/> in <c>[since, until]</c>, newest first. Null when the branch does not exist.</summary>
    Task<GqlCommitHistory?> GetCommitHistoryAsync(string owner, string repo, string branch, DateTime since, DateTime until, string? cursor, CancellationToken cancellationToken);

    Task<string?> GetDefaultBranchAsync(string owner, string repo, CancellationToken cancellationToken);

    /// <summary>One page of the owner's (organization or user) repositories, by name. Null when the owner does not exist.</summary>
    Task<GqlSummaryConnection?> ListRepositoriesAsync(string owner, string? cursor, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IActivitySource"/> over the shared <see cref="GitHubApiClient"/>.
/// </summary>
public class GitHubActivitySource(GitHubApiClient client) : IActivitySource
{
    public async Task<GqlPRSearch?> SearchPullRequestsAsync(string owner, string repo, DateTime start, DateTime end, string? cursor, CancellationToken cancellationToken) =>
        (await client.SearchRepoPullRequestsPageAsync(owner, repo, start, end, cursor))?.data?.search;

    public async Task<GqlCommitHistory?> GetCommitHistoryAsync(string owner, string repo, string branch, DateTime since, DateTime until, string? cursor, CancellationToken cancellationToken) =>
        (await client.GetCommitHistoryPageAsync(owner, repo, branch, since, until, cursor))?.data?.repository?.@ref?.target?.history;

    public Task<string?> GetDefaultBranchAsync(string owner, string repo, CancellationToken cancellationToken) =>
        client.GetDefaultBranchAsync(owner, repo);

    public async Task<GqlSummaryConnection?> ListRepositoriesAsync(string owner, string? cursor, CancellationToken cancellationToken) =>
        (await client.GetRepositoriesPageAsync(owner, cursor))?.data?.repositoryOwner?.repositories;
}
