using Metrics.GitHub;
using Metrics.Infrastructure;
using Metrics.GitActivity.Data;
using Metrics.GitActivity.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.GitActivity.Collection;

/// <summary>
/// Collects PRs (by created date) and branch commits (by commit date) for one repository within a window.
/// </summary>
/// <remarks>
/// Each page is stored together with a <see cref="CollectionCheckpoint"/> in one transaction. When a run
/// fails (rate limit budget exhausted, network, crash), re-running the same window resumes from the last
/// stored page. A window that completed is collected again from the start, refreshing the stored rows.
/// PRs are read oldest first through search in date chunks, mirroring GitHubSynchronizer's last-run tracking.
/// </remarks>
public sealed class ActivityCollector(IActivitySource source, ActivityDbContext db, GitHubActivityMapper mapper, RateLimitBackoff rateLimits, ILogger<ActivityCollector> logger)
{
    /// <summary>GitHub search returns at most this many results per query.</summary>
    public const int SearchResultLimit = 1000;

    static readonly TimeSpan MinChunk = TimeSpan.FromHours(1);

    /// <summary>Created-date span of one PR search; halved automatically when a chunk exceeds the search limit.</summary>
    public TimeSpan ChunkSize { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Names of the owner's repositories that start with <paramref name="prefix"/> (case-insensitive), sorted.
    /// Archived repositories are skipped unless <paramref name="includeArchived"/>.
    /// </summary>
    public async Task<IReadOnlyList<string>> FindRepositoriesAsync(string owner, string prefix, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var names = new List<string>();
        string? cursor = null;
        bool hasNext;
        do
        {
            var page = await rateLimits.ExecuteAsync($"{owner} repositories",
                () => source.ListRepositoriesAsync(owner, cursor, cancellationToken), cancellationToken)
                ?? throw new InvalidOperationException($"Organization or user '{owner}' not found.");

            names.AddRange(page.nodes
                .Where(r => r.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && (includeArchived || !r.isArchived))
                .Select(r => r.name));
            hasNext = page.pageInfo.hasNextPage;
            cursor = page.pageInfo.endCursor;
        } while (hasNext);

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    /// <summary>
    /// True when every requested phase of <paramref name="repository"/> has a completed checkpoint for the window.
    /// </summary>
    public async Task<bool> IsCompleteAsync(string repository, DateWindow window, bool includePullRequests, bool includeCommits, CancellationToken cancellationToken = default)
    {
        var phases = new List<CollectionPhase>();
        if (includePullRequests) phases.Add(CollectionPhase.PullRequests);
        if (includeCommits) phases.Add(CollectionPhase.Commits);

        var completed = await db.CollectionCheckpoints.AsNoTracking()
            .Where(c => c.Repository == repository && c.WindowStart == window.Start && c.WindowEnd == window.End && c.IsComplete)
            .Select(c => c.Phase)
            .ToListAsync(cancellationToken);
        return phases.All(completed.Contains);
    }

    public async Task<CollectionRunRecord> CollectAsync(string owner, string repo, DateWindow window, string? branch,
        bool includePullRequests = true, bool includeCommits = true, bool restart = false, CancellationToken cancellationToken = default)
    {
        var repository = $"{owner}/{repo}";
        var run = new CollectionRunRecord
        {
            Repository = repository,
            WindowStart = window.Start,
            WindowEnd = window.End,
            StartedAt = DateTime.UtcNow
        };
        db.CollectionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        try
        {
            if (includePullRequests)
            {
                var (count, resumed) = await CollectPullRequestsAsync(owner, repo, repository, window, restart, cancellationToken);
                run.PullRequestCount = count;
                run.Resumed |= resumed;
            }

            if (includeCommits)
            {
                branch = string.IsNullOrWhiteSpace(branch)
                    ? await rateLimits.ExecuteAsync("default branch", () => source.GetDefaultBranchAsync(owner, repo, cancellationToken), cancellationToken)
                    : branch;
                if (string.IsNullOrEmpty(branch))
                    throw new InvalidOperationException($"Could not resolve the default branch of {repository}.");

                var (count, resumed) = await CollectCommitsAsync(owner, repo, repository, branch, window, restart, cancellationToken);
                run.CommitCount = count;
                run.Resumed |= resumed;
            }

            run.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            run.Error = ex.Message;
            throw;
        }
        finally
        {
            db.ChangeTracker.Clear();
            db.CollectionRuns.Update(run);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        return run;
    }

    async Task<(int stored, bool resumed)> CollectPullRequestsAsync(string owner, string repo, string repository, DateWindow window,
        bool restart, CancellationToken cancellationToken)
    {
        var (checkpoint, resumed) = await LoadCheckpointAsync(repository, CollectionPhase.PullRequests, window, null, window.Start, restart, cancellationToken);
        var trustCursor = !resumed || checkpoint.Cursor is null;
        var stored = 0;
        var chunk = ChunkSize;

        try
        {
            while (checkpoint.Position < window.End)
            {
                var chunkStart = checkpoint.Position;
                var chunkEnd = Min(chunkStart + chunk, window.End);

                GqlPRSearch? page;
                try
                {
                    page = await rateLimits.ExecuteAsync($"{repository} PRs {chunkStart:u}",
                        () => source.SearchPullRequestsAsync(owner, repo, chunkStart, chunkEnd.AddSeconds(-1), checkpoint.Cursor, cancellationToken),
                        cancellationToken);
                }
                catch (HttpRequestException ex) when (ex is not RateLimitExceededException && !trustCursor)
                {
                    // The stored cursor may have expired; continue from the last stored PR instead.
                    logger.LogWarning("{Repository}: resume cursor rejected ({Message}); continuing from last stored PR at {LastItemAt:u}",
                        repository, ex.Message, checkpoint.LastItemAt);
                    checkpoint.Position = TruncateToSecond(checkpoint.LastItemAt ?? checkpoint.Position);
                    checkpoint.Cursor = null;
                    trustCursor = true;
                    continue;
                }
                trustCursor = true;

                if (page is null)
                    throw new InvalidOperationException($"No search results returned for {repository}.");

                if (checkpoint.Cursor is null && page.issueCount > SearchResultLimit)
                {
                    if (chunkEnd - chunkStart > MinChunk)
                    {
                        chunk = TimeSpan.FromTicks((chunkEnd - chunkStart).Ticks / 2);
                        logger.LogInformation("{Repository}: {Count} PRs from {Start:u} exceed the search limit; narrowing chunk to {Chunk}",
                            repository, page.issueCount, chunkStart, chunk);
                        continue;
                    }
                    logger.LogWarning("{Repository}: {Count} PRs within {Chunk} from {Start:u}; only the first {Limit} can be read",
                        repository, page.issueCount, chunkEnd - chunkStart, chunkStart, SearchResultLimit);
                }

                var now = DateTime.UtcNow;
                var records = page.nodes
                    .Where(p => p is not null && p.number > 0 && window.Contains(p.createdAt))
                    .Select(p => mapper.Map(repository, p, now))
                    .ToList();

                checkpoint.Cursor = page.pageInfo.hasNextPage ? page.pageInfo.endCursor : null;
                if (!page.pageInfo.hasNextPage)
                {
                    checkpoint.Position = chunkEnd;
                    chunk = ChunkSize;
                }
                if (records.Count > 0)
                {
                    var last = records[^1];
                    checkpoint.LastItemAt = last.CreatedAt;
                    checkpoint.LastItemKey = last.Number.ToString();
                    checkpoint.ItemsStored += records.Count;
                }

                await SavePageAsync(checkpoint, async () =>
                {
                    var numbers = records.Select(r => r.Number).ToList();
                    await db.PullRequests.Where(p => p.Repository == repository && numbers.Contains(p.Number)).ExecuteDeleteAsync(cancellationToken);
                    db.PullRequests.AddRange(records);
                }, cancellationToken);

                stored += records.Count;
                if (records.Count > 0)
                    logger.LogInformation("{Repository}: stored {Count} PRs up to #{Number} ({CreatedAt:yyyy-MM-dd}), {Total} this run",
                        repository, records.Count, checkpoint.LastItemKey, checkpoint.LastItemAt, stored);
            }

            checkpoint.IsComplete = true;
            await SavePageAsync(checkpoint, () => Task.CompletedTask, cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordErrorAsync(checkpoint, ex);
            throw;
        }
        return (stored, resumed);
    }

    async Task<(int stored, bool resumed)> CollectCommitsAsync(string owner, string repo, string repository, string branch, DateWindow window,
        bool restart, CancellationToken cancellationToken)
    {
        // History is newest first, so the checkpoint position is the upper (inclusive) commit-date bound.
        var (checkpoint, resumed) = await LoadCheckpointAsync(repository, CollectionPhase.Commits, window, branch, window.End.AddSeconds(-1), restart, cancellationToken);
        var trustCursor = !resumed || checkpoint.Cursor is null;
        var stored = 0;

        try
        {
            while (true)
            {
                GqlCommitHistory? history;
                try
                {
                    history = await rateLimits.ExecuteAsync($"{repository}@{branch} commits",
                        () => source.GetCommitHistoryAsync(owner, repo, branch, window.Start, checkpoint.Position, checkpoint.Cursor, cancellationToken),
                        cancellationToken);
                }
                catch (HttpRequestException ex) when (ex is not RateLimitExceededException && !trustCursor)
                {
                    logger.LogWarning("{Repository}@{Branch}: resume cursor rejected ({Message}); continuing from last stored commit at {LastItemAt:u}",
                        repository, branch, ex.Message, checkpoint.LastItemAt);
                    checkpoint.Position = TruncateToSecond(checkpoint.LastItemAt ?? checkpoint.Position);
                    checkpoint.Cursor = null;
                    trustCursor = true;
                    continue;
                }
                trustCursor = true;

                if (history is null)
                    throw new InvalidOperationException($"Branch '{branch}' not found in {repository}.");

                var now = DateTime.UtcNow;
                var records = history.nodes.Select(c => mapper.Map(repository, branch, c, now)).ToList();

                checkpoint.Cursor = history.pageInfo.hasNextPage ? history.pageInfo.endCursor : null;
                if (history.nodes.Count > 0)
                {
                    checkpoint.LastItemAt = history.nodes[^1].committedDate;
                    checkpoint.LastItemKey = history.nodes[^1].oid;
                    checkpoint.ItemsStored += records.Count;
                }

                await SavePageAsync(checkpoint, async () =>
                {
                    var hashes = records.Select(r => r.Hash).ToList();
                    await db.Commits.Where(c => c.Repository == repository && hashes.Contains(c.Hash)).ExecuteDeleteAsync(cancellationToken);
                    db.Commits.AddRange(records);
                }, cancellationToken);

                stored += records.Count;
                if (records.Count > 0)
                    logger.LogInformation("{Repository}@{Branch}: stored {Count} commits back to {CommittedAt:yyyy-MM-dd}, {Total} this run",
                        repository, branch, records.Count, checkpoint.LastItemAt, stored);

                if (!history.pageInfo.hasNextPage)
                    break;
            }

            checkpoint.IsComplete = true;
            await SavePageAsync(checkpoint, () => Task.CompletedTask, cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordErrorAsync(checkpoint, ex);
            throw;
        }
        return (stored, resumed);
    }

    /// <summary>
    /// Returns the checkpoint for the phase. An unfinished one (same branch for commits) is resumed
    /// unless <paramref name="restart"/>; anything else starts over at <paramref name="initialPosition"/>.
    /// </summary>
    async Task<(CollectionCheckpoint checkpoint, bool resumed)> LoadCheckpointAsync(string repository, CollectionPhase phase, DateWindow window,
        string? branch, DateTime initialPosition, bool restart, CancellationToken cancellationToken)
    {
        var checkpoint = await db.CollectionCheckpoints.AsNoTracking().SingleOrDefaultAsync(c =>
            c.Repository == repository && c.Phase == phase && c.WindowStart == window.Start && c.WindowEnd == window.End, cancellationToken);

        var resume = checkpoint is { IsComplete: false } && !restart && checkpoint.Branch == branch;
        checkpoint ??= new CollectionCheckpoint { Repository = repository, Phase = phase, WindowStart = window.Start, WindowEnd = window.End };

        if (resume)
        {
            logger.LogInformation("{Repository}: resuming {Phase} from checkpoint (position {Position:u}, last {LastItemKey} at {LastItemAt:u}, {Stored} already stored)",
                repository, phase, checkpoint.Position, checkpoint.LastItemKey, checkpoint.LastItemAt, checkpoint.ItemsStored);
        }
        else
        {
            checkpoint.Branch = branch;
            checkpoint.Position = initialPosition;
            checkpoint.Cursor = null;
            checkpoint.LastItemAt = null;
            checkpoint.LastItemKey = null;
            checkpoint.ItemsStored = 0;
            checkpoint.IsComplete = false;
        }
        checkpoint.LastError = null;

        await SavePageAsync(checkpoint, () => Task.CompletedTask, cancellationToken);
        return (checkpoint, resume);
    }

    /// <summary>
    /// Applies <paramref name="writeData"/> and the checkpoint in one transaction.
    /// </summary>
    async Task SavePageAsync(CollectionCheckpoint checkpoint, Func<Task> writeData, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await writeData();
        checkpoint.UpdatedAt = DateTime.UtcNow;
        if (checkpoint.Id == 0)
            db.CollectionCheckpoints.Add(checkpoint);
        else
            db.CollectionCheckpoints.Update(checkpoint);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    async Task RecordErrorAsync(CollectionCheckpoint checkpoint, Exception ex)
    {
        try
        {
            db.ChangeTracker.Clear();
            // Persist only the error; position/cursor stay at the last committed page.
            await db.CollectionCheckpoints.Where(c => c.Id == checkpoint.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastError, ex.Message).SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        }
        catch (Exception saveError)
        {
            logger.LogWarning(saveError, "Could not record the checkpoint error");
        }
    }

    static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    static DateTime TruncateToSecond(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
}
