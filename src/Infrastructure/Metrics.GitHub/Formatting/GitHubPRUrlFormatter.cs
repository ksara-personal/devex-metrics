using System;

namespace Metrics.GitHub;

/// <summary>
/// Formats GitHub pull request URLs.
/// </summary>
public sealed class GitHubPRUrlFormatter : IPRUrlFormatter
{
    /// <summary>
    /// Formats the URL for a given GitHub pull request.
    /// </summary>
    /// <param name="repositoryNameWithOwner">The repository name with owner (e.g., "owner/repo").</param>
    /// <param name="prNumber">The pull request number.</param>
    /// <returns>The formatted pull request URL.</returns>
    public string Format(string repositoryNameWithOwner, int prNumber) =>
        string.Format(GitHubConstants.GitHubPullRequestUrl, repositoryNameWithOwner, prNumber);
}
