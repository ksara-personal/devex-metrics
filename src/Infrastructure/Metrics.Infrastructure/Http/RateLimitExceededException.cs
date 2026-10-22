using System.Net;

namespace Metrics.Infrastructure;

/// <summary>
/// Thrown when the remote API rejects a request because a primary or secondary rate limit was hit.
/// Derives from <see cref="HttpRequestException"/> so existing handlers keep working.
/// </summary>
public sealed class RateLimitExceededException(string message, DateTimeOffset? resetAt, TimeSpan? retryAfter, HttpStatusCode? statusCode)
    : HttpRequestException(message, null, statusCode)
{
    /// <summary>When the primary rate limit window resets, if the API said so.</summary>
    public DateTimeOffset? ResetAt { get; } = resetAt;

    /// <summary>Delay requested through the <c>Retry-After</c> header (secondary limits).</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;

    /// <summary>
    /// How long to wait before retrying: Retry-After, else until reset, else <paramref name="fallback"/>.
    /// </summary>
    public TimeSpan GetWaitTime(DateTimeOffset now, TimeSpan fallback)
    {
        if (RetryAfter is { } retry && retry > TimeSpan.Zero)
            return retry;
        if (ResetAt is { } reset && reset > now)
            return reset - now + TimeSpan.FromSeconds(1);
        return fallback;
    }
}
