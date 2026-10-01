namespace Metrics.Infrastructure;

/// <summary>
/// Persistence contract for callers that need to compose queries directly against the
/// Entity Framework model rather than through the storage-agnostic
/// <see cref="IMetricsPersistenceService"/> port.
/// </summary>
/// <remarks>
/// These two members expose <see cref="DevExMetricDbContext"/>, so they belong to the
/// infrastructure layer and are deliberately kept off the application port.
/// </remarks>
public interface IDbContextMetricsPersistenceService : IMetricsPersistenceService
{
    /// <summary>
    /// Executes a query against the database context.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Task<IEnumerable<T>> QueryAsync<T>(Func<DevExMetricDbContext, IQueryable<T>> query);

    /// <summary>
    /// Upserts a record in the database.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public Task<int> UpsertAsync(Func<DevExMetricDbContext, Task> operation);
}
