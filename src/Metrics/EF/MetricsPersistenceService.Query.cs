using System;
using System.Diagnostics.CodeAnalysis;
using Metrics.Extensions;
using Metrics.Models;
using Microsoft.EntityFrameworkCore;

namespace Metrics.EF;

partial class MetricsPersistenceService<TContext>
{
    /// <summary>
    /// Executes a query against the database context.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(Func<DevExMetricDbContext, IQueryable<T>> query)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var q = query(context);
        var results = await q.ToListAsync();
        return results;
    }

    /// <summary>
    /// Gets all the PR's in a given state, states could be open, merged or closed.
    /// </summary>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllPRsByStateAsync(string state = "OPEN", int pageSize = 200)
    {
        state = state ?? throw new ArgumentNullException(nameof(state), "PR state should be either open, closed or merged");

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = context.PRMetrics
            .Where(p => p.State == state)
            .OrderBy(p => p.Repository)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        await foreach (var metric in query.PaginateQuery(pageSize))
        {
            yield return metric;
        }
    }

    /// <summary>
    /// Gets the reviewer metrics summary for a specific sprint.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <param name="reviewer"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ReviewerSprintMetrics>> GetReviewerMetricsSummaryBySprintAsync(int year, int sprintNumber, string reviewer = null, string repository = null)
    {
        var sprintInfo = _sprintCalendar.GetSprintInfo(year, sprintNumber);

        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = context.ReviewerSprintMetrics
            .Where(rsm => rsm.Year == year && rsm.SprintNumber == sprintNumber)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(repository))
        {
            query = query.Where(rsm => rsm.Repository == repository);
        }
        if(!string.IsNullOrEmpty(reviewer))
        {
            query = query.Where(rsm => rsm.Reviewer == reviewer);
        }
        return await query.ToListAsync();
    }
    
    /// <summary>
    /// Gets a summary of reviewer metrics for a specified month.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <param name="reviewer"></param>
    /// <param name="repository"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ReviewerMonthlyMetrics>> GetReviewerMetricsSummaryByMonthAsync(int year, int month, string reviewer = null, string repository = null)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = context.ReviewerMonthlyMetrics
            .Where(rsm => rsm.Year == year && rsm.Month == month)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(repository))
        {
            query = query.Where(rsm => rsm.Repository == repository);
        }
        if(!string.IsNullOrEmpty(reviewer))
        {
            query = query.Where(rsm => rsm.Reviewer == reviewer);
        }
        return await query.ToListAsync();
    }

    /// <summary>
    /// Creates metrics query with relationships included based on the conditions.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="includeReviewerMetrics"></param>
    /// <param name="includeContributors"></param>
    /// <returns></returns>
    IQueryable<PRMetrics> CreateMetricsQuery(TContext context, bool includeReviewerMetrics = false, bool includeContributors = false, bool asNoTracking = true)
    {
        IQueryable<PRMetrics> query = context.PRMetrics
            .Include(p => p.DevExMetricItem)
            .Include(p => p.Team)
            .Include(p => p.CopilotReviewMetrics);

        if (includeReviewerMetrics)
        {
            query = query.Include(p => p.ReviewerMetrics);
        }
        if (includeContributors)
        {
            query = query.Include(p => p.Contributors);
        }
        if( asNoTracking )
        {
            query = query.AsNoTracking();
        }
        return query.AsSingleQuery()
                    .AsQueryable();
    }

    /// <summary>
    /// Gets all PR metrics for the given filters in a paginated manner.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async IAsyncEnumerable<IEnumerable<PRMetrics>> GetAllMetricsAsync(string team = null, DateTime? start = null, DateTime? end = null, bool includeReviewerMetrics = true)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = CreateMetricsQuery(context, includeReviewerMetrics, true);
        if (!string.IsNullOrEmpty(team))
        {
            query = query.Where(m => m.Team.Equals(team));
        }
        if (start.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= start.Value);
        }
        if (end.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= end.Value);
        }
        query = query.OrderBy(x => x.Id);
        
        await foreach (var metric in query.PaginateQuery())
        {
            yield return metric;
        }
    }

    /// <summary>
    /// Gets all prs by value stream.
    /// </summary>
    /// <param name="valueStream"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<PRMetrics>> GetAllPRsByValueStreamAsync(string valueStream, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(valueStream))
            throw new ArgumentNullException(nameof(valueStream));

        return await QueryPRMetrics(start, end, query => query.Where(m => m.Team.ValueStream == valueStream));
    }

    /// <summary>
    /// Gets all prs by the given team, start and end dates
    /// </summary>
    /// <param name="team"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<PRMetrics>> GetAllPRsByTeamAsync(string team, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(team))
            throw new ArgumentNullException(nameof(team));

        return await QueryPRMetrics(start, end, query => query.Where(m => m.Team.Name == team), true);
    }

    /// <summary>
    /// Gets all prs by the given author
    /// </summary>
    /// <param name="author"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<PRMetrics>> GetAllPRsByAuthorAsync(string author, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(author))
            throw new ArgumentNullException(nameof(author));

        return await QueryPRMetrics(start, end, query => query.Where(m => m.Author == author), true, true);
    }

    /// <summary>
    /// Gets all prs bound to a region
    /// </summary>
    /// <param name="teamRegion"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<PRMetrics>> GetAllPRsByTeamRegionAsync(string teamRegion, DateTime start, DateTime end)
    {
        if (string.IsNullOrEmpty(teamRegion))
            throw new ArgumentNullException(nameof(teamRegion));

        return await QueryPRMetrics(start, end, query => query.Where(m => m.Team.Region == teamRegion));
    }

    /// <summary>
    /// Query PR metrics, constructs query and adds additional filters before executing it.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    async Task<IEnumerable<PRMetrics>> QueryPRMetrics(DateTime start, DateTime end, Func<IQueryable<PRMetrics>, IQueryable<PRMetrics>> callback, bool includeReviewerMetrics = false, bool includeContributors = false)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var dtStart = start.Kind != DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeToUtc(start, TimeZoneInfo.Local) : start;
        var dtEnd = end.Kind != DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeToUtc(end, TimeZoneInfo.Local) : end;

        var query = CreateMetricsQuery(context, includeReviewerMetrics, includeContributors)
            .Where(m => m.CreatedAt >= dtStart && m.CreatedAt <= dtEnd);

        query = callback(query);
        var results = await query.ToListAsync();
        return results;
    }

    /// <summary>
    /// Gets the raw copilot metrics for each PR and given team.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="team"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<PRMetrics>> GetCopilotReviewerMetricsByTeamAsync(DateTime start, DateTime end, string? team = null)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = context.PRMetrics
                .Where(m => m.CopilotReviewMetrics != null && m.CreatedAt >= start && m.CreatedAt <= end)
                .Include(p => p.CopilotReviewMetrics)
                .AsSingleQuery()
                .AsQueryable();

        if (!string.IsNullOrEmpty(team))
            query = query.Where(m => m.Team.Name == team);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets the copilot metrics for all teams.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<CopilotReviewSummary>> GetCopilotReviewerMetricsForAllTeamsAsync(DateTime start, DateTime end)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = context.PRMetrics
                .Where(m => m.CopilotReviewMetrics != null && m.CreatedAt >= start && m.CreatedAt <= end)
                .Include(p => p.CopilotReviewMetrics)
                .AsSingleQuery()
                .AsQueryable();

        return await query
                .GroupBy(m => m.Team.Name)
                .Select(g => new CopilotReviewSummary(g.Key,
                    g.Count(),
                    g.Sum(x => x.CopilotReviewMetrics.Comments),
                    g.Sum(x => x.CopilotReviewMetrics.FilesChanged),
                    g.Sum(x => x.CopilotReviewMetrics.FilesReviewed)
                    ))
                .ToListAsync();
    }

    /// <summary>
    /// Gets author metrics by sprint.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns> 
    public async Task<AuthorMetrics> GetAuthorMetricsBySprintAsync(string author, int year, int sprintNumber)
    {
        var sprint = _sprintCalendar.GetSprintInfo(year, sprintNumber);
        return await GetAuthorMetricsAsync(author, query => query.Where(p => p.CreatedAt >= sprint.StartDate && p.CreatedAt <= sprint.EndDate));
    }

    /// <summary>
    /// Gets author metrics by month.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public async Task<AuthorMetrics> GetAuthorMetricsByMonthAsync(string author, int year, int month)
    {
        if (year < DataSynchronizer.DefaultStartDate.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year), $"Year should be greater than or equal to {DataSynchronizer.DefaultStartDate.Year}");
        }
        if (month < DataSynchronizer.DefaultStartDate.Month || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month should be between 1 and 12");
        }
        
        return await GetAuthorMetricsAsync(author, query => query.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month));
    }

    /// <summary>
    /// Gets team metrics by sprint.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public async Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamBySprintAsync(string team, int year, int sprintNumber)
    {
        var sprint = _sprintCalendar.GetSprintInfo(year, sprintNumber);
        return await GetTeamMetricsAsync(team, query => query.Where(p => p.CreatedAt >= sprint.StartDate && p.CreatedAt <= sprint.EndDate));
    }

    /// <summary>
    /// Gets team metrics by month.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    public async Task<IEnumerable<AuthorMetrics>> GetAuthorMetricsForTeamByMonthAsync(string team, int year, int month)
    {
        if (year < DataSynchronizer.DefaultStartDate.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year), $"Year should be greater than or equal to {DataSynchronizer.DefaultStartDate.Year}");
        }
        if (month < DataSynchronizer.DefaultStartDate.Month || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month should be between 1 and 12");
        }
        return await GetTeamMetricsAsync(team, query => query.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="author"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    async Task<AuthorMetrics?> GetAuthorMetricsAsync(string author, Func<IQueryable<PRMetrics>, IQueryable<PRMetrics>> callback)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = CreateMetricsQuery(context, false, false)
            .Where(m => m.Author == author);

        query = callback(query);
        var metrics = await query.ToListAsync();

        return metrics.Any() ? new AuthorMetrics
        {
            Author = author,
            Team = metrics.FirstOrDefault()?.Team.Name,
            PrsAuthored = metrics.Count,
            PrsClosed = metrics.Count(m => m.State == PullRequestStates.Closed || m.State == PullRequestStates.Merged),
            AvgPrSize = metrics.Any() ? (float)metrics.Average(m => m.TotalLines) : 0,
            AvgCommentCount = metrics.Any() ? (float)metrics.Where(m => m.TotalComments > 0).Average(m => m.TotalComments) : 0,
            AvgCycleTime = metrics.Any() ? (float)metrics.Average(m => m.DevExMetricItem.CycleTime.GetValueOrDefault().TotalHours) : 0,
            AvgReviewComments = metrics.Any() ? (float)metrics.Where(m => m.DevExMetricItem.TotalReviewComments > 0).Average(m => m.DevExMetricItem.TotalReviewComments) : 0,
            AvgPRMaturity = metrics.Any() ? (float)metrics.Average(m => m.DevExMetricItem.MaturityPercentage.GetValueOrDefault()) : 0
        } : null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="team"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    async Task<IEnumerable<AuthorMetrics>> GetTeamMetricsAsync(string team, Func<IQueryable<PRMetrics>, IQueryable<PRMetrics>> callback)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var query = CreateMetricsQuery(context, false, false)
            .Where(m => m.Team.Name == team);

        query = callback(query);
        var metrics = await query.ToListAsync();

        return metrics.GroupBy(m => m.Author).Select(g => new AuthorMetrics
        {
            Author = g.Key,
            Team = team,
            PrsAuthored = g.Count(),
            PrsClosed = g.Count(m => m.State == PullRequestStates.Closed || m.State == PullRequestStates.Merged),
            AvgPrSize = g.Any() ? (float)g.Average(m => m.TotalLines) : 0,
            AvgCommentCount = g.Any() ? (float)g.Average(m => m.TotalComments) : 0,
            AvgCycleTime = g.Any() ? (float)g.Average(m => m.DevExMetricItem.CycleTime.GetValueOrDefault().TotalHours) : 0,
            AvgReviewComments = g.Any() ? (float)g.Average(m => m.DevExMetricItem.TotalReviewComments) : 0,
            AvgPRMaturity = g.Any() ? (float)g.Average(m => m.DevExMetricItem.MaturityPercentage.GetValueOrDefault()) : 0
        });
    }
}
