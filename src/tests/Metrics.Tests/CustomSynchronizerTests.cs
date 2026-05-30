using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Metrics;
using Metrics.MultiTenant;
using System.Text.Json;
using Metrics.GitHub;

namespace Metrics.Tests;

public sealed class CustomSynchronizerTests : TestBase
{
    public CustomSynchronizerTests() : base()
    {
    }

    protected override void AddServices(IServiceCollection services)
    {
        base.AddServices(services);
        services.AddScoped<CustomGitHubPRMetricsSynchronizer>();
    }

    [Theory]
    [InlineData("tenant-1", "2025-10-01", "2025-10-31", "your-github-org", "repo-1")]
    [InlineData("tenant-1", "2025-10-01", "2025-10-31", "your-github-org", "repo-2")]
    public async Task Custom_GitHubPRMetrics_Synchronizer_TestAsync(string tenantId, DateTime start, DateTime end, string ownerOrg, string repo)
    {
        SetTenant(tenantId);
        var synchronizer = _host.Services.GetRequiredService<CustomGitHubPRMetricsSynchronizer>();
        synchronizer.SetOrganization(new GitHubOrganization
        {
            Owner = ownerOrg,
            Repositories = new List<string> { repo },
            DefaultTeam = "Default Team",
            DefaultRegion = "Default Region"
        });
        await synchronizer.WriteMetricsAsync(start, end);
    }

    [Theory]
    [InlineData("your-github-org_repo-1_PRMetrics_20251109.json", true)]
    [InlineData("your-github-org_repo-2_PRMetrics_20251109.json", false)]
    public async Task Test_VsReviewers_Time_Async(string fileName, bool isUltra = true)
    {
        var ceReviewers = new List<string>
        {
            "code-excellence-team-1"
        };
        var repo1CEReviewers = new List<string>
        {
            "reviewer-1", "reviewer-2", "reviewer-3"
        };
        var repo2CEReviewers = new List<string>
        {
            "reviewer-4", "reviewer-5", "reviewer-6"
        };
        var reviewers = isUltra ? repo1CEReviewers : repo2CEReviewers;

        var prs = await Path.GetFullPath(fileName).ReadFromFileAsync<GitHubPRRoot>();
        _logger.LogInformation("Total PRs loaded from file {FileName}: {Count}", fileName, prs.Count);

        var reviewStatistics = new List<ReviewStatistics>();
        int count = 0;
        foreach (var root in prs)
        {
            var pr = root.data.repository.pullRequest;
            var reviews = pr.reviews.nodes;
            DateTime? reviewedByTeam = null, reviewedByCE = null;
            foreach (var timeline in pr.timelineItems.nodes)
            {
                if (timeline.__typename == GitHubTimelineStates.ReviewRequestedEvent)
                {
                    switch (timeline.requestedReviewer?.__typename)
                    {
                        case GitHubRequestedReviewerTypes.Team:
                            var team = timeline.requestedReviewer?.slug;
                            if (ceReviewers.Contains(team))
                            {
                                var requestedByReview = reviews.FirstOrDefault(r => r.onBehalfOf.nodes.Any(m => m.slug == team));
                                //_logger.LogInformation("PR #{PRNumber} code excellence requested review from {Team} at {RequestedAt}, Reviewed On:{ReviewedOn}",
                                //    pr.number, team, timeline.createdAt, requestedByReview?.submittedAt);
                                if (reviewedByCE == null)
                                    reviewedByCE = requestedByReview?.submittedAt;
                            }
                            else
                            {
                                var requestedByReview = reviews.FirstOrDefault(r => r.onBehalfOf.nodes.Any(m => m.slug == team));
                                //_logger.LogInformation("PR #{PRNumber} requested review from {Team} at {RequestedAt}, Reviewed On:{ReviewedOn}",
                                //    pr.number, team, timeline.createdAt, requestedByReview?.submittedAt);
                                if (reviewedByTeam == null)
                                    reviewedByTeam = requestedByReview?.submittedAt;
                            }
                            break;
                        case GitHubRequestedReviewerTypes.User:
                            var user = timeline.requestedReviewer?.login;
                            if (reviewers.Contains(user))
                            {
                                var requestedByReview = reviews.FirstOrDefault(r => r.author.login == user);
                                //_logger.LogInformation("PR #{PRNumber} code excellence requested review from {User} at {RequestedAt}, Reviewed On:{ReviewedOn}",
                                //    pr.number, user, timeline.createdAt, requestedByReview?.submittedAt);
                                if (reviewedByCE == null)
                                    reviewedByCE = requestedByReview?.submittedAt;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            if (reviewedByTeam.HasValue && reviewedByCE.HasValue && reviewedByCE < reviewedByTeam)
            {
                var stats = new ReviewStatistics
                {
                    PRNumber = pr.number,
                    Title = pr.title,
                    Url = pr.url,
                    Author = pr.author?.login,
                    State = pr.state,
                    TeamReviewedAt = reviewedByTeam.Value,
                    CEReviewedAt = reviewedByCE
                };
                reviewStatistics.Add(stats);
                //_logger.LogInformation("{Stats}", stats);
                count++;
            }
        }
        _logger.LogInformation("Total PRs where CE review happened before team review: {Count}", count);
        await Task.CompletedTask;
    }
}

class ReviewStatistics
{
    public int PRNumber { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string Author { get; set; }
    public string State { get; set; }
    public DateTime TeamReviewedAt { get; set; }
    public DateTime? CEReviewedAt { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

sealed class CustomGitHubPRMetricsSynchronizer : GitHubPRMetricsSynchronizer
{
    Organization _org;

    public CustomGitHubPRMetricsSynchronizer(ILogger<CustomGitHubPRMetricsSynchronizer> logger,
        GitHubApiClient apiClient,
        TenantConfigurationService tenantConfig,
        PRAnalyzer<GitHubPRRoot> analyzer,
        IMetricsPersistenceService persistenceService) : base(logger, apiClient, tenantConfig, analyzer, persistenceService)
    {
    }

    public void SetOrganization(Organization org) => _org = org ?? throw new ArgumentNullException(nameof(org));

    protected override async Task EnumerateOrgsAndWriteMetricsAsync(DateTime? startDate, DateTime endDate)
    {
        if (_org == null)
        {
            throw new InvalidOperationException("Organization is not set.");
        }

        foreach (var repo in _org.Repositories)
        {
            _logger.LogInformation("Synchronizing metrics for organization {Owner} and repo {Repo}", _org.Owner, repo);
            await SyncPullRequests(string.Concat(_org.Owner, "/", repo), startDate, endDate, false);
        }
    }

    protected override async Task ProcessSearchResults(string repoWithOwner, SearchRoot results, int? lastPRNumber, bool useLastRun)
    {
        // write to file.
        var fileName = $"{repoWithOwner.Replace("/", "_")}_PRMetrics_{DateTime.UtcNow:yyyyMMdd}.json";
        var edges = results.data.search.edges;
        _logger.LogInformation("Writing {Count} PR metrics to file {FileName}", edges.Count, fileName);
        foreach (var segment in edges.Chunk(30))
        {
            var newMetrics = await Task.WhenAll(segment.Select(edge =>
            {
                var repo = edge.node.repository;
                return GetPRDetailsAsync(repo.owner.login, repo.name, edge.node.number);
            }));
            await newMetrics.WriteOrAppendToJsonFile(fileName, true);
        }
    }
}
