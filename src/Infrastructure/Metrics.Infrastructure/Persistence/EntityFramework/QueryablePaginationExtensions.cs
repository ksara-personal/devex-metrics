using Metrics.Domain;
using Microsoft.EntityFrameworkCore;

namespace Metrics.Infrastructure;

/// <summary>
/// EF Core specific pagination helpers for <see cref="IQueryable{T}"/> of <see cref="PRMetrics"/>.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Paginates the query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<IEnumerable<PRMetrics>> PaginateQuery(this IQueryable<PRMetrics> query, int batchSize = 100)
    {
        int skip = 0;
        while (true)
        {
            var batch = await query
                .OrderBy(e => e.Id)
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync();

            if (!batch.Any())
                break;

            yield return batch;

            skip += batchSize;
        }
    }

    /// <summary>
    /// Paginates query for migrations where records may be modified during iteration.
    /// Always starts from the beginning since processed records are no longer in the result set.
    /// </summary>
    /// <param name="queryFactory">Factory function to create a fresh query each time</param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<IEnumerable<PRMetrics>> Paginate(this Func<IQueryable<PRMetrics>> queryFactory, int batchSize = 100)
    {
        while (true)
        {
            var query = queryFactory();
            var batch = await query
                .OrderBy(e => e.Id)
                .Take(batchSize)
                .ToListAsync();

            if (!batch.Any())
                break;

            yield return batch;
        }
    }
}
