using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub;

/// <summary>
/// PR Analyzer that can handle GitHub specific objects.
/// </summary>
public sealed class GitHubPRAnalyzer : PRAnalyzer<GitHubPRRoot>
{
    readonly MultiOrgUserService<GitHubOrganization> _crossOrgUserService;
    const string WorkItemId = "WorkItemId";
    static readonly Regex _issueRegex = new Regex(@"(?n)AB#(?<WorkItemId>\d+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public GitHubPRAnalyzer(IServiceProvider serviceProvider, ILogger<GitHubPRAnalyzer> logger,
            MultiOrgUserService<GitHubOrganization> crossOrgUserService)
        : base(serviceProvider, logger) => _crossOrgUserService = crossOrgUserService;

    /// <summary>
    /// Creates the metrics for the given pr.
    /// </summary>
    /// <param name="prRoot"></param>
    /// <returns></returns>
    protected override PRMetrics CreateMetrics(GitHubPRRoot prRoot)
    {
        var pr = prRoot.data.repository.pullRequest;
        var arr = pr.headRefName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var login = pr.author?.login;
        var teamInfo = !string.IsNullOrEmpty(login) ?
             _crossOrgUserService.ResolveUser2TeamAndVS(pr.repository.owner?.login, !GitHubAuthors.IsBotOrAI(login) ? login : pr.mergedBy?.login ?? login)
            : DefaultUserMembershipService<GitHubOrganization>._ghostTeam;

        var metrics = new PRMetrics
        {
            PrNumber = pr.number,
            Author = login,
            MergedBy = pr.mergedBy?.login,
            MergedAt = pr.mergedAt,
            CreatedAt = pr.createdAt,
            UpdatedAt = pr.updatedAt,
            TotalComments = pr.totalCommentsCount,
            TotalCommits = pr.commits.totalCount,
            TotalLines = pr.additions + pr.deletions,
            IsFeature = GitHubBranchRefPrefixes.Feature.Equals(arr[0]),
            BaseBranch = pr.baseRefName,
            State = pr.state,
            Team = teamInfo,
            Repository = pr.repository?.nameWithOwner,
            ChangedFiles = pr.changedFiles
        };
        metrics.DevExMetricItem = new()
        {
            PRMetric = metrics
        };
        
        if (!string.IsNullOrEmpty(pr.bodyText))
        {
            var matches = _issueRegex.Matches(pr.bodyText);
            if (matches.Count > 0)
            {
                foreach (var (match, index) in matches.Select((m, i) => (m, i)))
                {
                    var _ = index switch
                    {
                        0 when match.Groups[WorkItemId].Success => metrics.WorkItemId = match.Groups[WorkItemId].Value,
                        1 when match.Groups[WorkItemId].Success => metrics.WorkItemId2 = match.Groups[WorkItemId].Value,
                        _ => null
                    };
                    if (index > 2)
                        break;
                }
            }
        }
        if(string.IsNullOrEmpty(metrics.WorkItemId) && string.IsNullOrEmpty(metrics.WorkItemId2) && !string.IsNullOrEmpty(pr.title))
        {
            // check if title has the issue id.
            var match = _issueRegex.Match(pr.title);
            if (match.Success)
            {
                metrics.WorkItemId = match.Groups[WorkItemId].Value;
            }
        }
        return metrics;
    }

    /// <summary>
    /// Sanitizes jenkins and merge to develop branches from the commits.
    /// </summary>
    /// <param name="prRoot"></param>
    protected override GitHubPRRoot Sanitize(GitHubPRRoot prRoot)
    {
        var pr = prRoot.data.repository.pullRequest;
        // author could be empty due to users removed.
        if (pr.author is null)
        {
            //_logger.LogInformation("PR '{Url}' author is null, setting it to ghost", pr.url);
            prRoot = prRoot with
            {
                data = prRoot.data with
                {
                    repository = prRoot.data.repository with
                    {
                        pullRequest = prRoot.data.repository.pullRequest with
                        {
                            author = new Author(GitHubAuthors.Ghost)
                        }
                    }
                }
            };
        }
        // remove all commits that are either specific to jenkins or merge from develop.
        var commits = pr.commits;
        // convert to HashSet to remove collection modification errors and remove duplicate items
        var removableCommits = (from node in commits.nodes
                                let msg = node.commit.messageHeadline
                                where node.commit.parents.totalCount > 1 // remove multiple parent commits.
                                || msg.Contains("Merge branch 'develop'", StringComparison.OrdinalIgnoreCase)
                                || msg.Contains("jenkins", StringComparison.OrdinalIgnoreCase)
                                select node).ToHashSet();

        var org = _crossOrgUserService.GetOrgUserService(pr.repository.owner?.login)?.Organization;
        var botLogin = org.IgnoreReviewsFromBotName;
        // remove copilot bot reviews as it can affect the metrics.
        if (!string.IsNullOrWhiteSpace(botLogin) && pr.reviews?.nodes?.Count > 0)
        {
            var node = pr.reviews.nodes.FirstOrDefault(n => botLogin.Equals(n.author?.login, StringComparison.OrdinalIgnoreCase));
            pr.CopilotReviewNode = node;
            if (node != null)
            {
                pr.reviews.nodes.Remove(node);
            }
        }

        foreach (var node in removableCommits)
        {
            commits.nodes.Remove(node);
        }
        return prRoot;
    }

    /// <summary>
    /// Gets the total draft workflow transitions.
    /// </summary>
    /// <param name="prRoot"></param>
    /// <returns></returns>
    protected override int GetTotalDraftWorkflowTransitions(GitHubPRRoot prRoot)
    {
        var pr = prRoot.data.repository.pullRequest;
        // find state transitions and eliminate internal commits.
        var transitions = FindStateTransitionDateRanges(pr.timelineItems);
        pr.DraftTransitions = transitions;
        return transitions.Count;
    }

    /// <summary>
    /// Find the state transitions and returns the date ranges.
    /// </summary>
    /// <param name="prRoot"></param>
    /// <returns></returns>
    static List<(DateTime, DateTime)> FindStateTransitionDateRanges(TimelineItems events)
    {
        // possible transitions
        // ReviewRequestedEvent -> ConvertToDraftEvent
        // ConvertToDraftEvent -> ReviewRequestedEvent
        var fromDraftTransitions = new List<(DateTime, DateTime)>(3);

        if (events is not null && events.nodes?.Count() > 0)
        {
            (string? reviewRequested, DateTime? reviewRequestedAt) = (null, null);
            (string? draftRequested, DateTime? draftRequestedAt) = (null, null);


            foreach (var node in events.nodes)
            {
                var current = node.__typename;
                if ( GitHubTimelineStates.ReviewRequestedEvent.Equals(current))
                {
                    if (draftRequested is not null)
                    {
                        fromDraftTransitions.Add((draftRequestedAt.Value, node.createdAt.Value));
                        draftRequested = null;
                        draftRequestedAt = null;
                    }
                    reviewRequested = current;
                    reviewRequestedAt = node.createdAt;
                }
                if (GitHubTimelineStates.ConvertToDraftEvent.Equals(current))
                {
                    if (reviewRequested is not null)
                    {
                        reviewRequestedAt = null;
                        reviewRequested = null;
                    }
                    draftRequested = current;
                    draftRequestedAt = node.createdAt;
                }
            }
        }
        return fromDraftTransitions;
    }
}
