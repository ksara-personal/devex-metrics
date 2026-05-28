using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Metrics.Models;

namespace Metrics.EF;

partial class MetricsPersistenceService<TContext>
{
    /// <summary>
    /// Gets the last run at
    /// </summary>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public virtual async Task<(DateTime?,int?)> GetLastRunAt(string repoWithOwner)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);
        var status = await context.Runs.Where(r => r.Repository == repoWithOwner)
            .SingleOrDefaultAsync();

        return (status?.LastRunAt, status?.PRNumber);
    }

    /// <summary>
    /// Updates the last run with the datetime and exception if available.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public virtual async Task UpdateLastRunAt(DateTime dateTime, string repoWithOwner, int? prNumber, Exception? exception = null)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        var status = await context.Runs.Where(r => r.Repository == repoWithOwner)
            .SingleOrDefaultAsync();

        var isNew = status is null;
        if (isNew)
            status = new RunStatus();
        
        status.LastRunAt = dateTime;
        status.PRNumber = prNumber;
        status.Repository = repoWithOwner;
        status.LastError = exception == null ? null : exception.GetAllMessages();
        var _ = isNew ? context.Runs.Add(status) : context.Runs.Update(status);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts the run status asynchronously.
    /// This method is used to log the status of a run, including whether it was successful or failed, and any associated error message.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<int> InsertRunStatusAsync(IEnumerable<RunStatus> runStatuses)
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);

        context.Runs.AddRange(runStatuses);
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the run statuses.
    /// This method retrieves the run statuses from the database, ordered by the last run date in descending order.
    /// It is useful for tracking the history of runs and their statuses.
    /// </summary>
    /// <returns>A collection of RunStatus objects.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Microsoft.EntityFrameworkCore.ChangeTracking.EntryCurrentValueComparer<int>))]
    public async Task<IEnumerable<RunStatus>> GetRunStatusesAsync()
    {
        using var context = CreateTenantContext();
        await EnsureCreatedAsync(context);
        return await context.Runs
            .OrderByDescending(r => r.LastRunAt)
            .ToListAsync();
    }
}
