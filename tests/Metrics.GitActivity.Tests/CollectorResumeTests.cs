using System.Net;
using Metrics.GitHub;
using Metrics.Infrastructure;
using Metrics.GitActivity.Collection;
using Metrics.GitActivity.Data;
using Metrics.GitActivity.Periods;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metrics.GitActivity.Tests;

public sealed class CollectorResumeTests : IDisposable
{
    static readonly DateWindow Window = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

    readonly SqliteConnection _connection = new("Data Source=:memory:");
    readonly FakeSource _source = new();
    readonly List<TimeSpan> _delays = [];

    public CollectorResumeTests()
    {
        _connection.Open();
        using var db = NewDb();
        db.Database.Migrate();

        // 10 PRs, one every 8 days, so they span several 30-day chunks.
        for (var i = 1; i <= 10; i++)
            _source.PullRequests.Add(Pr(i, Window.Start.AddDays(8 * (i - 1)).AddHours(3)));
        for (var i = 1; i <= 7; i++)
            _source.Commits.Add(Commit($"c{i}", Window.Start.AddDays(10 * i)));
    }

    public void Dispose() => _connection.Dispose();

    ActivityDbContext NewDb() => new(new DbContextOptionsBuilder<ActivityDbContext>().UseSqlite(_connection).Options);

    ActivityCollector NewCollector(ActivityDbContext db, TimeSpan? maxWait = null) =>
        new(_source, db, new GitHubActivityMapper(new Dictionary<string, string>()),
            new RateLimitBackoff(NullLogger<RateLimitBackoff>.Instance)
            {
                MaxTotalWait = maxWait ?? TimeSpan.FromHours(1),
                Delay = (d, _) => { _delays.Add(d); return Task.CompletedTask; }
            },
            NullLogger<ActivityCollector>.Instance);

    async Task<CollectionRunRecord> CollectAsync(bool restart = false, bool commits = false, TimeSpan? maxWait = null)
    {
        await using var db = NewDb();
        return await NewCollector(db, maxWait).CollectAsync("acme", "app", Window, "main", includePullRequests: !commits, includeCommits: commits, restart: restart);
    }

    async Task<CollectionCheckpoint> CheckpointAsync(CollectionPhase phase)
    {
        await using var db = NewDb();
        return await db.CollectionCheckpoints.SingleAsync(c => c.Phase == phase);
    }

    async Task<int> StoredPullRequestsAsync()
    {
        await using var db = NewDb();
        return await db.PullRequests.CountAsync();
    }

    [Fact]
    public async Task Resumes_pull_requests_from_the_last_stored_page()
    {
        _source.FailOnCall = 3;   // chunk 1 page 1, chunk 1 page 2, then fail
        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync());

        var checkpoint = await CheckpointAsync(CollectionPhase.PullRequests);
        Assert.False(checkpoint.IsComplete);
        Assert.Equal(4, checkpoint.ItemsStored);
        Assert.Equal("4", checkpoint.LastItemKey);
        Assert.Equal(4, await StoredPullRequestsAsync());

        _source.FailOnCall = null;
        _source.Calls.Clear();
        var run = await CollectAsync();

        Assert.True(run.Resumed);
        Assert.Equal(6, run.PullRequestCount);                         // only the rest was fetched
        Assert.Equal(checkpoint.Position, _source.Calls[0].Start);    // same chunk ...
        Assert.Equal(checkpoint.Cursor, _source.Calls[0].Cursor);     // ... from the stored cursor
        Assert.Equal(10, await StoredPullRequestsAsync());
        Assert.True((await CheckpointAsync(CollectionPhase.PullRequests)).IsComplete);
    }

    [Fact]
    public async Task Completed_window_is_collected_again_from_the_start()
    {
        await CollectAsync();
        _source.Calls.Clear();

        var run = await CollectAsync();

        Assert.False(run.Resumed);
        Assert.Equal(10, run.PullRequestCount);
        Assert.Equal(Window.Start, _source.Calls[0].Start);
        Assert.Null(_source.Calls[0].Cursor);
        Assert.Equal(10, await StoredPullRequestsAsync());             // upserted, not duplicated
    }

    [Fact]
    public async Task Restart_ignores_an_unfinished_checkpoint()
    {
        _source.FailOnCall = 3;
        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync());
        _source.FailOnCall = null;
        _source.Calls.Clear();

        var run = await CollectAsync(restart: true);

        Assert.False(run.Resumed);
        Assert.Equal(Window.Start, _source.Calls[0].Start);
        Assert.Equal(10, run.PullRequestCount);
    }

    [Fact]
    public async Task Rejected_cursor_falls_back_to_last_stored_pr_date()
    {
        _source.FailOnCall = 2;   // after the first page of chunk 1, so a mid-chunk cursor is stored
        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync());
        var checkpoint = await CheckpointAsync(CollectionPhase.PullRequests);
        Assert.NotNull(checkpoint.Cursor);

        _source.FailOnCall = null;
        _source.RejectCursor = checkpoint.Cursor;
        _source.Calls.Clear();
        var run = await CollectAsync();

        Assert.Equal(checkpoint.Cursor, _source.Calls[0].Cursor);
        Assert.Null(_source.Calls[1].Cursor);
        Assert.Equal(checkpoint.LastItemAt, _source.Calls[1].Start);   // PR #2 is re-read and upserted
        Assert.Equal(10, await StoredPullRequestsAsync());
        Assert.True(run.Resumed);
    }

    [Fact]
    public async Task Waits_for_rate_limit_reset_and_continues()
    {
        _source.RateLimitOnCall = 2;
        _source.RateLimit = new RateLimitExceededException("API rate limit exceeded", DateTimeOffset.UtcNow.AddSeconds(30), null, HttpStatusCode.OK);

        var run = await CollectAsync();

        Assert.Single(_delays);
        Assert.InRange(_delays[0], TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(32));
        Assert.Equal(10, run.PullRequestCount);
    }

    [Fact]
    public async Task Stops_when_wait_exceeds_budget_and_resumes_next_run()
    {
        _source.RateLimitOnCall = 2;
        _source.RateLimit = new RateLimitExceededException("secondary rate limit", null, TimeSpan.FromHours(2), HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<RateLimitExceededException>(() => CollectAsync());
        Assert.Empty(_delays);
        var checkpoint = await CheckpointAsync(CollectionPhase.PullRequests);
        Assert.Contains("secondary rate limit", checkpoint.LastError);
        Assert.Equal(2, checkpoint.ItemsStored);

        _source.RateLimitOnCall = null;
        var run = await CollectAsync();
        Assert.True(run.Resumed);
        Assert.Equal(10, await StoredPullRequestsAsync());
    }

    [Fact]
    public async Task Narrows_chunk_when_search_exceeds_result_limit()
    {
        _source.ReportedCount = start => start == Window.Start && _source.Calls.Count == 1 ? 1500 : null;

        var run = await CollectAsync();

        Assert.Equal(TimeSpan.FromDays(30), _source.Calls[0].End - _source.Calls[0].Start + TimeSpan.FromSeconds(1));
        Assert.Equal(TimeSpan.FromDays(15), _source.Calls[1].End - _source.Calls[1].Start + TimeSpan.FromSeconds(1));
        Assert.Equal(10, run.PullRequestCount);
    }

    [Fact]
    public async Task Resumes_commits_from_the_last_stored_page()
    {
        _source.FailOnCall = 3;
        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync(commits: true));
        var checkpoint = await CheckpointAsync(CollectionPhase.Commits);
        Assert.Equal(4, checkpoint.ItemsStored);

        _source.FailOnCall = null;
        _source.Calls.Clear();
        var run = await CollectAsync(commits: true);

        Assert.True(run.Resumed);
        Assert.Equal(3, run.CommitCount);
        Assert.Equal(checkpoint.Cursor, _source.Calls[0].Cursor);
        await using var db = NewDb();
        Assert.Equal(7, await db.Commits.CountAsync());
    }

    [Fact]
    public async Task Finds_repositories_by_prefix_across_pages()
    {
        _source.Repositories.AddRange(
        [
            new("api", false, false), new("Source.Billing", false, false), new("source.claims", false, false),
            new("source.legacy", true, false), new("sourcery", false, false), new("source.members", false, true)
        ]);
        await using var db = NewDb();
        var collector = NewCollector(db);

        // Case-insensitive prefix; "sourcery" and archived repos are excluded; forks are kept.
        Assert.Equal(["Source.Billing", "source.claims", "source.members"],
            await collector.FindRepositoriesAsync("acme", "source.", includeArchived: false));
        Assert.Contains("source.legacy", await collector.FindRepositoriesAsync("acme", "source.", includeArchived: true));
    }

    [Fact]
    public async Task Reports_completion_per_window_and_phase()
    {
        await using var db = NewDb();
        var collector = NewCollector(db);
        Assert.False(await collector.IsCompleteAsync("acme/app", Window, true, true));

        await CollectAsync();   // PRs only

        Assert.True(await collector.IsCompleteAsync("acme/app", Window, includePullRequests: true, includeCommits: false));
        Assert.False(await collector.IsCompleteAsync("acme/app", Window, includePullRequests: true, includeCommits: true));
        Assert.False(await collector.IsCompleteAsync("acme/other", Window, true, false));
    }

    static GqlPullRequest Pr(int number, DateTime createdAt) => new(
        number, $"PR {number}", $"https://x/{number}", null, "MERGED", false, createdAt, createdAt.AddDays(1), createdAt.AddDays(1),
        "main", $"feature/{number}", 1, 1, 1, 0, null, new GqlActor("dev", null, null),
        null, null, null, null, null, null);

    static GqlCommit Commit(string oid, DateTime date) => new(oid, $"commit {oid}", null, date, date,
        new GqlGitActor("dev", "dev@x.com", date), new GqlGitActor("dev", "dev@x.com", date), new GqlNodes<GqlCommitOid>([]));

    record Call(DateTime Start, DateTime End, string? Cursor);

    /// <summary>
    /// In-memory GitHub: pages of two, cursor = index of the next item.
    /// </summary>
    sealed class FakeSource : IActivitySource
    {
        const int PageSize = 2;
        public List<GqlPullRequest> PullRequests { get; } = [];
        public List<GqlCommit> Commits { get; } = [];
        public List<Call> Calls { get; } = [];
        public int? FailOnCall { get; set; }
        public int? RateLimitOnCall { get; set; }
        public RateLimitExceededException? RateLimit { get; set; }
        public string? RejectCursor { get; set; }
        public Func<DateTime, int?> ReportedCount { get; set; } = _ => null;
        int _totalCalls;

        void Before(DateTime start, DateTime end, string? cursor)
        {
            Calls.Add(new Call(start, end, cursor));
            _totalCalls++;
            if (RateLimitOnCall == _totalCalls)
            {
                RateLimitOnCall = RateLimit!.RetryAfter is null ? null : RateLimitOnCall + 1000; // reset-based limits clear after waiting
                throw RateLimit;
            }
            if (FailOnCall == Calls.Count)
                throw new HttpRequestException("network failure");
            if (cursor is not null && cursor == RejectCursor)
            {
                RejectCursor = null; // only the stale cursor is rejected; freshly issued ones are valid
                throw new HttpRequestException("Error in response data: invalid cursor");
            }
        }

        static (List<T> page, PageInfo info) Page<T>(List<T> items, string? cursor)
        {
            var from = cursor is null ? 0 : int.Parse(cursor);
            var page = items.Skip(from).Take(PageSize).ToList();
            var next = from + page.Count;
            return (page, new PageInfo(next < items.Count, next.ToString()));
        }

        public Task<GqlPRSearch?> SearchPullRequestsAsync(string owner, string repo, DateTime start, DateTime end, string? cursor, CancellationToken cancellationToken)
        {
            Before(start, end, cursor);
            var matching = PullRequests.Where(p => p.createdAt >= start && p.createdAt <= end).OrderBy(p => p.createdAt).ToList();
            var (page, info) = Page(matching, cursor);
            return Task.FromResult<GqlPRSearch?>(new GqlPRSearch(ReportedCount(start) ?? matching.Count, info, page));
        }

        public Task<GqlCommitHistory?> GetCommitHistoryAsync(string owner, string repo, string branch, DateTime since, DateTime until, string? cursor, CancellationToken cancellationToken)
        {
            Before(since, until, cursor);
            var matching = Commits.Where(c => c.committedDate >= since && c.committedDate <= until).OrderByDescending(c => c.committedDate).ToList();
            var (page, info) = Page(matching, cursor);
            return Task.FromResult<GqlCommitHistory?>(new GqlCommitHistory(info, page));
        }

        public Task<string?> GetDefaultBranchAsync(string owner, string repo, CancellationToken cancellationToken) => Task.FromResult<string?>("main");

        public List<GqlSummary> Repositories { get; } = [];

        public Task<GqlSummaryConnection?> ListRepositoriesAsync(string owner, string? cursor, CancellationToken cancellationToken)
        {
            var (page, info) = Page(Repositories, cursor);
            return Task.FromResult<GqlSummaryConnection?>(new GqlSummaryConnection(info, page));
        }
    }
}
