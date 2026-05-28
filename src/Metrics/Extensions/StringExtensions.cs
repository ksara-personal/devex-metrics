namespace Metrics;

/// <summary>
/// Extension methods for string operations related to GitHub repository names.
/// This class provides a method to extract the owner and repository names from a full repository name string
/// </summary>
static class StringExtensions
{
    /// <summary>
    /// Gets the owner and repo names.
    /// </summary>
    /// <param name="ownerOrOrg"></param>
    /// <param name="repo"></param>
    /// <returns></returns>
    public static (string, string) GetOwnerAndRepoNames(this string repo)
    {
        // find if the repo has owner.
        var arr = repo.Split('/');
        var repository = repo;
        string ownerOrOrg = string.Empty;
        if (arr.Length > 1)
        {
            ownerOrOrg = arr[0];
            repository = arr[1];
        }
        else
        {
            throw new ArgumentException("Repository name must be in the format 'owner/repo'.", nameof(repo));
        }
        return (ownerOrOrg, repository);
    }
}
