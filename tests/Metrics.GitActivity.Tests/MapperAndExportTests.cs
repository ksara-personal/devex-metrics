using ClosedXML.Excel;
using Metrics.GitHub;
using Metrics.GitActivity.Collection;
using Metrics.GitActivity.Data;
using Metrics.GitActivity.Export;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Metrics.GitActivity.Tests;

public class MapperAndExportTests
{
    static readonly DateTime Created = new(2025, 1, 15, 10, 32, 0, DateTimeKind.Utc);

    static readonly GitHubActivityMapper Mapper = new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["jane"] = "jane@company.com"
    });

    static GqlPullRequest SamplePr() => new(
        number: 4521,
        title: "PROJ-123: Implement login flow",
        url: "https://github.com/acme/product-core/pull/4521",
        bodyText: "Adds OAuth2 login flow per PROJ-123, also PROJ-124",
        state: "MERGED",
        isDraft: false,
        createdAt: Created,
        mergedAt: Created.AddDays(2),
        closedAt: Created.AddDays(2),
        baseRefName: "main",
        headRefName: "feature/PROJ-123-login-flow",
        additions: 342,
        deletions: 87,
        changedFiles: 14,
        totalCommentsCount: 12,
        reviewDecision: "APPROVED",
        author: new GqlActor("jane", "Jane", null),
        commits: new GqlCountedNodes<GqlLastCommit>(6, [new GqlLastCommit(new GqlLastCommitInner(new GqlStatusCheckRollup("SUCCESS")))]),
        labels: new GqlNodes<GqlLabel>([new("bug"), new("frontend")]),
        closingIssuesReferences: new GqlNodes<GqlLinkedIssue>([new(77, "https://github.com/acme/product-core/issues/77")]),
        reviewRequests: new GqlNodes<GqlReviewRequest>([new(new GqlRequestedReviewer("alex", null, "alex@company.com", null)), new(new GqlRequestedReviewer(null, null, null, "core-team"))]),
        reviews: new GqlCountedNodes<GqlReview>(4,
        [
            new(new GqlActor("jane", null, null), "COMMENTED", Created.AddHours(1), new GqlTotalCount(1)),
            new(new GqlActor("sam", null, "sam@company.com"), "COMMENTED", Created.AddHours(6), new GqlTotalCount(2)),
            new(new GqlActor("alex", null, "alex@company.com"), "CHANGES_REQUESTED", Created.AddDays(1), new GqlTotalCount(3)),
            new(new GqlActor("alex", null, "alex@company.com"), "APPROVED", Created.AddDays(1.5), new GqlTotalCount(0)),
        ]),
        comments: new GqlCountedNodes<GqlIssueComment>(2,
        [
            new(new GqlActor("jane", null, null), Created.AddMinutes(5)),
            new(new GqlActor("pat", null, null), Created.AddHours(3)),
        ]));

    [Fact]
    public void Maps_pull_request_and_derives_review_fields()
    {
        var pr = Mapper.Map("acme/product-core", SamplePr(), DateTime.UtcNow);

        Assert.Equal("jane@company.com", pr.AuthorEmail);
        Assert.Equal(6, pr.CommitsCount);
        Assert.Equal("SUCCESS", pr.CiStatus);
        Assert.Equal(Created.AddDays(1), pr.ReviewSubmittedAt);   // first approve/changes-requested by someone else
        Assert.Equal(Created.AddHours(3), pr.FirstReviewAt);      // pat's comment beats sam's review
        Assert.Equal(2, pr.ReviewRounds);                         // one changes-requested + final round
        Assert.Equal(["#77", "PROJ-123", "PROJ-124"], pr.LinkedIssues.Select(i => i.Reference));
        Assert.Equal(["alex", "team:core-team"], pr.ReviewRequests.Select(r => r.Reviewer));
    }

    [Fact]
    public void Unreviewed_pr_has_no_rounds()
    {
        var pr = Mapper.Map("acme/x", SamplePr() with { reviews = null, comments = null }, DateTime.UtcNow);
        Assert.Equal(0, pr.ReviewRounds);
        Assert.Null(pr.FirstReviewAt);
    }

    [Fact]
    public void Maps_commit_parents_and_body()
    {
        var commit = Mapper.Map("acme/x", "main", new GqlCommit("abc", "PROJ-1 fix", "", Created, Created.AddHours(1),
            new GqlGitActor("Jane", "jane@company.com", Created),
            new GqlGitActor("GitHub", "noreply@github.com", Created.AddHours(1)),
            new GqlNodes<GqlCommitOid>([new("p1"), new("p2")])), DateTime.UtcNow);

        Assert.Equal("p1 p2", commit.ParentHashes);
        Assert.Equal(2, commit.ParentCount);
        Assert.Null(commit.Body);
        Assert.Equal(Created.AddHours(1), commit.CommitterDate);
    }

    [Fact]
    public void Export_columns_match_the_requested_layout()
    {
        Assert.Equal(
        [
            "PR ID / Number", "Repository", "Author Email", "Created Date", "Merged Date", "Status", "Source Branch",
            "Target Branch", "Reviewer(s)", "Review Decision", "Comments Count", "Lines Added", "Lines Deleted",
            "Files Changed", "Commits Count", "Title", "Review Submitted Date", "Comment Authors", "First Review Date",
            "Labels / Tags", "Review Rounds", "Description", "Linked Issues", "Requested Reviewers", "CI / Build Status"
        ], ActivityWorkbookExporter.PullRequestColumns.Select(c => c.Header));

        Assert.Equal(
        [
            "Commit Hash", "Author Email", "Author Date", "Subject Line", "Parent Hashes", "Author Name",
            "Committer Email", "Committer Date", "Committer Name", "Message Body"
        ], ActivityWorkbookExporter.CommitColumns.Select(c => c.Header));
    }

    [Fact]
    public async Task Exports_prefix_repositories_into_one_workbook()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var db = new ActivityDbContext(new DbContextOptionsBuilder<ActivityDbContext>().UseSqlite(connection).Options);
        await db.Database.MigrateAsync();

        foreach (var repo in new[] { "acme/source.claims", "acme/Source.Billing", "acme/api" })
        {
            db.PullRequests.Add(Mapper.Map(repo, SamplePr(), DateTime.UtcNow));
            db.Commits.Add(new CommitRecord
            {
                Repository = repo, Branch = "main", Hash = $"h-{repo}", Subject = "s",
                AuthorDate = Created, CommitterDate = Created
            });
        }
        // A repository with commits only is still found.
        db.Commits.Add(new CommitRecord { Repository = "acme/source.tools", Branch = "main", Hash = "t", Subject = "s", AuthorDate = Created, CommitterDate = Created });
        await db.SaveChangesAsync();

        var exporter = new ActivityWorkbookExporter(db);
        var repos = await exporter.FindStoredRepositoriesAsync("acme", "source.");
        Assert.Equal(["acme/Source.Billing", "acme/source.claims", "acme/source.tools"], repos);
        Assert.Equal(["acme/api", "acme/Source.Billing", "acme/source.claims", "acme/source.tools"],
            await exporter.FindStoredRepositoriesAsync("acme", string.Empty));   // --org

        var path = Path.Combine(Path.GetTempPath(), $"repo-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            var (prs, commits) = await exporter.ExportAsync(repos, null, path, includeRepositoryColumn: true);
            Assert.Equal((2, 3), (prs, commits));

            using var workbook = new XLWorkbook(path);
            Assert.Equal(["Pull Requests", "Commits"], workbook.Worksheets.Select(w => w.Name));

            var prSheet = workbook.Worksheet("Pull Requests");
            Assert.Equal("acme/Source.Billing", prSheet.Cell(2, 2).GetString());
            Assert.Equal("acme/source.claims", prSheet.Cell(3, 2).GetString());

            var commitSheet = workbook.Worksheet("Commits");
            Assert.Equal("Repository", commitSheet.Cell(1, 1).GetString());
            Assert.Equal("Commit Hash", commitSheet.Cell(1, 2).GetString());
            Assert.Equal(["acme/Source.Billing", "acme/source.claims", "acme/source.tools"],
                Enumerable.Range(2, 3).Select(r => commitSheet.Cell(r, 1).GetString()));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Exports_stored_repository_to_two_sheets()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ActivityDbContext>().UseSqlite(connection).Options;
        await using var db = new ActivityDbContext(options);
        await db.Database.MigrateAsync();

        db.PullRequests.Add(Mapper.Map("acme/product-core", SamplePr(), DateTime.UtcNow));
        db.PullRequests.Add(Mapper.Map("acme/other", SamplePr(), DateTime.UtcNow));
        db.Commits.Add(new CommitRecord
        {
            Repository = "acme/product-core", Branch = "main", Hash = "abc", Subject = "PROJ-123 fix",
            AuthorEmail = "jane@company.com", AuthorDate = Created, CommitterDate = Created, ParentHashes = "p1"
        });
        await db.SaveChangesAsync();

        var path = Path.Combine(Path.GetTempPath(), $"repo-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            var (prs, commits) = await new ActivityWorkbookExporter(db).ExportAsync("acme/product-core", null, path);
            Assert.Equal((1, 1), (prs, commits));

            using var workbook = new XLWorkbook(path);
            var prSheet = workbook.Worksheet("Pull Requests");
            Assert.Equal(4521, prSheet.Cell(2, 1).GetValue<int>());
            Assert.Equal("jane@company.com", prSheet.Cell(2, 3).GetString());
            Assert.Equal("Merged", prSheet.Cell(2, 6).GetString());
            Assert.Equal("sam@company.com, alex@company.com", prSheet.Cell(2, 9).GetString());
            Assert.Equal("Approved", prSheet.Cell(2, 10).GetString());
            Assert.Equal("pat, sam@company.com, alex@company.com", prSheet.Cell(2, 18).GetString());
            Assert.Equal("bug, frontend", prSheet.Cell(2, 20).GetString());
            Assert.Equal("Passed", prSheet.Cell(2, 25).GetString());

            var commitSheet = workbook.Worksheet("Commits");
            Assert.Equal("abc", commitSheet.Cell(2, 1).GetString());
            Assert.Equal(Created, commitSheet.Cell(2, 3).GetDateTime());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
