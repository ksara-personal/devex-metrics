using Microsoft.EntityFrameworkCore;
using Metrics.Models;

namespace Metrics.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Runs parallel tasks.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="totalCount"></param>
    /// <param name="callback"></param>
    public static async IAsyncEnumerable<IEnumerable<T>> RunParallelTasks<T, T2>(this IEnumerable<T2> enumerable, Func<T2, Task<T>> callback, int increment = 20)
        where T2 : class
    {
        var values = enumerable.ToArray();
        int currentIndex = 0, max = values.Length;
        bool exit = false;
        int thresholdForInclusion = increment + (increment / 2);
        while (currentIndex < max)
        {
            int remainingItems = max - currentIndex;
            List<T2> itemsSliced;

            if (remainingItems <= thresholdForInclusion)
            {
                itemsSliced = values.Skip(currentIndex).ToList();
                exit = true;
            }
            else
            {
                itemsSliced = values.Skip(currentIndex).Take(increment).ToList();
                currentIndex += increment;
            }
            var tasks = itemsSliced.Select(item => callback(item)).ToList();
            yield return await Task.WhenAll(tasks);

            if (exit)
                break;
        }
    }

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
