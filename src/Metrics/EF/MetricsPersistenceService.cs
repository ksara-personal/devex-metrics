using System.Diagnostics.CodeAnalysis;
using Finbuckle.MultiTenant.Abstractions;
using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.EF;

/// <summary>
/// Provides a persistence service for storing and retrieving pull request metrics and related data using a specified Entity Framework database context.
/// Handles bulk insertions, reviewer management, run status updates, and complex queries for PR metrics by team, author, value stream, and region.
/// </summary>
/// <typeparam name="TContext">The type of the database context, must inherit from DevExMetricDbContext.</typeparam>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public partial class MetricsPersistenceService<TContext> : IMetricsPersistenceService
    where TContext : DevExMetricDbContext
{
    protected readonly IDbContextFactory<TContext> _contextFactory;
    protected ILogger _logger;
    protected readonly SprintCalendar _sprintCalendar;
    private readonly IMultiTenantContextAccessor? _multiTenantContextAccessor;
    
    public MetricsPersistenceService(IDbContextFactory<TContext> contextFactory, ILogger<TContext> logger, SprintCalendar sprintCalendar,
        IMultiTenantContextAccessor? multiTenantContextAccessor = null)
    {
        _contextFactory = contextFactory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sprintCalendar = sprintCalendar ?? throw new ArgumentNullException(nameof(sprintCalendar));
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    /// <summary>
    /// Creates a DbContext via the factory and sets the current tenant info on it.
    /// This ensures contexts created outside the DI pipeline still have tenant isolation.
    /// </summary>
    protected TContext CreateTenantContext()
    {
        var context = _contextFactory.CreateDbContext();
        if (_multiTenantContextAccessor?.MultiTenantContext?.TenantInfo is { } tenantInfo)
        {
            context.TenantInfo = tenantInfo;
        }
        return context;
    }

    /// <summary>
    /// Ensures that the database is created.
    /// This method is called to ensure that the database schema is created before performing any operations.
    /// </summary>
    /// <param name="context"></param>
    protected virtual async Task EnsureCreatedAsync(TContext context, bool initialLoad = false) => await context.Database.EnsureCreatedAsync();

    /// <summary>
    /// Inserts the records if they don't exist.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<int> InsertIfNotExistsRangeAsync(IEnumerable<PRMetrics> metrics)
    {
        if (metrics is null)
            throw new ArgumentNullException(nameof(metrics));

        var metricKeys = metrics
            .Select(x => string.Join('|', x.Repository, x.PrNumber))
            .ToList();

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);
        var existingIds = await context.PRMetrics
            .Where(p => metricKeys.Contains(p.Repository + "|" + p.PrNumber))
            .Select(p => p.Repository + "|" + p.PrNumber)
            .ToListAsync();

        var newMetrics = metrics
            .Where(p => !existingIds.Contains(p.Repository + "|" + p.PrNumber))
            .ToList();

        if (newMetrics.Any())
        {
            _logger.LogInformation("Inserting {Count} new PR metrics", newMetrics.Count);
            return await CreatePRWithMetricsAsync(newMetrics);
        }
        return 0;
    }

    /// <summary>
    /// Gets the PR urls not existing in the database.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<string>> GetPRUrlsNotExists(IEnumerable<string> values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var existingIds = await context.PRMetrics
            .Where(p => values.Contains(p.Repository + "|" + p.PrNumber))
            .Select(p => p.Repository + "|" + p.PrNumber)
            .ToListAsync();

        var newValues = values
            .Where(p => !existingIds.Contains(p))
            .ToList();

        return newValues;
    }

    /// <summary>
    /// Gets the existing reviewer data
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    async Task<Dictionary<string, Team>> GetOrAddTeams(IEnumerable<Team> values, bool initialLoad = false)
    {
        // due to multiple threads and concurrency issues, use the transaction.
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context, initialLoad);

        using var transaction = context.Database.IsInMemory() ? null : await context.Database.BeginTransactionAsync();
        var keys = values.Select(v => v.ToString()).ToList();
        var existingValues = await context.Teams
                .Where(p => keys.Contains(p.Name + "|" + p.ValueStream + "|" + p.Region))
                .ToDictionaryAsync(k => k.Name + "|" + k.ValueStream + "|" + k.Region, v => v, StringComparer.OrdinalIgnoreCase);

        try
        {
            var newTeams = keys.Except(existingValues.Keys, StringComparer.OrdinalIgnoreCase).Select(k => values.First(v => v.Name + "|" + v.ValueStream + "|" + v.Region == k));
            context.Teams.AddRange(newTeams);
            await context.SaveChangesAsync();

            // re-fetch as bulk insert is not updating the output.
            existingValues = await context.Teams
                .AsTracking()
                .Where(p => keys.Contains(p.Name + "|" + p.ValueStream + "|" + p.Region))
                .ToDictionaryAsync(k => k.Name + "|" + k.ValueStream + "|" + k.Region, v => v, StringComparer.OrdinalIgnoreCase);

            if (transaction is not null)
                await transaction.CommitAsync();
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            throw;
        }
        return existingValues;
    }

    /// <summary>
    /// Creates the PR metrics for the metrics instances.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    protected virtual async Task<int> CreatePRWithMetricsAsync(IEnumerable<PRMetrics> metrics) =>
        await InternalCreatePRWithMetricsAsync(metrics);

    /// <summary>
    /// Creates the entries for PR's.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    protected async Task<int> InternalCreatePRWithMetricsAsync(IEnumerable<PRMetrics> metrics, bool initialLoad = false)
    {
        if (metrics is null)
            throw new ArgumentNullException(nameof(metrics));

        var allTeams = metrics.Select(m => new { m.Team, m });
        var teams = allTeams.DistinctBy(t => t.Team.Name).Select(t => t.Team);
        var teamsDict = await GetOrAddTeams(teams, initialLoad);

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context, initialLoad);

        foreach (var item in allTeams)
        {
            if (teamsDict.TryGetValue(item.Team.ToString(), out var existing))
            {
                item.m.TeamId = existing.Id;
                item.m.Team = null;
            }
        }

        // split the dictionary into separate rows.
        foreach (var metric in metrics)
        {
            //_logger.LogInformation("Inserting PR Metric for PR #{PrNumber} with reviewer metrics {MetricsCount}", metric.PrNumber, metric.ReviewerMetrics.Count);
            context.ReviewerDailyMetrics.AddRange(metric.ReviewerDailyMetrics);
            context.Contributors.AddRange(metric.Contributors);

            if (metric.CopilotReviewMetrics is not null)
            {
                context.CopilotReviewMetrics.Add(metric.CopilotReviewMetrics);
            }
            if (metric.DevExMetricItem is not null)
            {
                context.DevExMetricItems.AddRange(metric.DevExMetricItem);
            }
        }

        context.PRMetrics.AddRange(metrics);
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the pr metrics for the given id.
    /// </summary>
    /// <param name="prId"></param>
    /// <param name="repo"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<PRMetrics?> GetPRWithMetricsAsync(int prId, string repo)
    {
        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentNullException(nameof(repo), "Repository name cannot be null or empty.");
        if (prId <= 0)
            throw new ArgumentOutOfRangeException(nameof(prId), "Pull Request ID must be greater than zero.");

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);
        var results = await CreateMetricsQuery(context, true, true)
            .Where(p => p.PrNumber == prId && p.Repository == repo)
            .ToListAsync();

        // Log the results for debugging purposes
        _logger.LogInformation("Retrieved {Count} PR metrics for PR ID {PRId} in repository {Repo}.", results.Count, prId, repo);

        return results.FirstOrDefault();
    }

    /// <summary>
    /// Updates the prs that have their state changed from open to merged or closed.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    public async Task<int> UpdatePRsAsync(IEnumerable<PRMetrics> metrics)
    {
        if (metrics is null)
            throw new ArgumentNullException(nameof(metrics));

        var metricKeys = metrics
            .Select(x => string.Join('|', x.Repository, x.PrNumber))
            .ToList();

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);
        var existingMetrics = await CreateMetricsQuery(context, true, true, false)
            .Include(p => p.ReviewerDailyMetrics)
            .Where(p => metricKeys.Contains(p.Repository + "|" + p.PrNumber))
            .ToDictionaryAsync(m => $"{m.Repository}|{m.PrNumber}");

        // convert new metrics into devex collection.
        foreach (var metric in metrics)
        {
            if (existingMetrics.TryGetValue($"{metric.Repository}|{metric.PrNumber}", out var existingMetric))
            {
                PRMetricsMapper.Map(metric, existingMetric);
            }
        }
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a record in the database.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public async Task<int> UpsertAsync(Func<DevExMetricDbContext, Task> operation)
    {
        using var context = CreateTenantContext();
        await operation(context);
        return await context.SaveChangesAsync();
    }
}
