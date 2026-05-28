
using Microsoft.Extensions.Logging;
using Metrics.Models;
namespace Metrics.GitHub.Providers;

/// <summary>
/// Metric provider for calculating contributors to a pull request.
/// </summary>
public sealed class PRContributorMetricProvider : GitHubMetricProvider
{
    /// <summary>
    /// Service for resolving user/team information from commit authors.
    /// </summary>
    readonly MultiOrgUserService<GitHubOrganization> _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PRContributorMetricProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for metric provider events.</param>
    /// <param name="userService">Service for resolving user/team information.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="userService"/> is null.</exception>
    public PRContributorMetricProvider(ILogger<GitHubMetricProvider> logger, MultiOrgUserService<GitHubOrganization> userService) : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Calculates and adds contributor metrics to the given <see cref="PRMetrics"/> instance.
    /// Groups commits by resolved contributor login, counts commits, and sums lines of code (additions + deletions).
    /// </summary>
    /// <param name="pr">The pull request to analyze.</param>
    /// <param name="metrics">The metrics object to update with contributor data.</param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        var groupedCommits = pr.commits.nodes.GroupBy(cn => _userService.Resolve2UserLogin(pr.repository?.owner?.login, cn.commit.author.name) ?? cn.commit.author.name);
        List<PRContributor> contributors = new(
            groupedCommits
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Select(g => new PRContributor
                {
                    Contributor = g.Key,
                    Commits = g.Count(),
                    LOC = g.Sum(i => i.commit.additions + i.commit.deletions)
                })
        );
        metrics.Contributors = contributors;
    }
}
