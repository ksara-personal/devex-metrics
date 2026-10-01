
namespace Metrics.Application;

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
}
