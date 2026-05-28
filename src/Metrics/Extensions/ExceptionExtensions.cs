namespace Metrics;

/// <summary>
/// Extension methods for exceptions.
/// Provides methods to retrieve all messages and stack traces from an exception, including inner exceptions.
/// </summary>
static class ExceptionExtensions
{
    /// <summary>
    /// Gets all message of an exception.
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static string GetAllMessages(this Exception ex)
    {
        var messages = new List<string>();
        CollectMessages(ex, messages);
        return string.Join(" -> ", messages);
    }

    /// <summary>
    /// Recursively collects all messages and stack traces from the given exception and its inner exceptions.
    /// Handles both regular and AggregateException types.
    /// </summary>
    /// <param name="ex">The exception to process.</param>
    /// <param name="messages">The list to which messages and stack traces are added.</param>
    static void CollectMessages(Exception ex, List<string> messages)
    {
        if (ex == null) return;

        if (ex is AggregateException aggEx)
        {
            foreach (var innerEx in aggEx.InnerExceptions)
            {
                CollectMessages(innerEx, messages);
            }
        }
        else
        {
            messages.Add(ex.Message);
            messages.Add(ex.StackTrace);
            CollectMessages(ex.InnerException, messages);
        }
    }
}
