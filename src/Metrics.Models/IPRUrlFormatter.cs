using System;

namespace Metrics;

/// <summary>
/// Interface for formatting pull request URLs.
/// </summary>
public interface IPRUrlFormatter
{
    /// <summary>
    /// Formats a pull request URL based on the repository name with owner and the pull request number.
    /// </summary>
    /// <param name="repositoryNameWithOwner">The repository name including the owner (e.g., "owner/repo").</param>
    /// <param name="prNumber">The pull request number.</param>
    /// <returns>A formatted URL string for the pull request.</returns>
    string Format(string repositoryNameWithOwner, int prNumber);
}
