using Metrics.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Metrics.GitActivity.Collection;

/// <summary>
/// Retries a call after GitHub rate limits by sleeping until the limit resets (primary limit)
/// or for the <c>Retry-After</c> delay (secondary limit). Gives up once the total wait of a run
/// would exceed <see cref="MaxTotalWait"/>; the checkpoint then lets the next run resume.
/// </summary>
public sealed class RateLimitBackoff(ILogger<RateLimitBackoff> logger, TimeProvider? timeProvider = null)
{
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    TimeSpan _waited = TimeSpan.Zero;

    /// <summary>Wait used when GitHub gives neither a reset time nor Retry-After.</summary>
    public TimeSpan FallbackWait { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Total time a run may spend waiting on rate limits.</summary>
    public TimeSpan MaxTotalWait { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Replaceable for tests.</summary>
    public Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = (d, ct) => Task.Delay(d, ct);

    public TimeSpan TotalWaited => _waited;

    public async Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> call, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                return await call();
            }
            catch (RateLimitExceededException ex)
            {
                var wait = ex.GetWaitTime(_time.GetUtcNow(), FallbackWait);
                if (_waited + wait > MaxTotalWait)
                {
                    logger.LogError("{Operation}: rate limited and the wait of {Wait} would exceed the {Max} budget; stopping. Re-run to resume.",
                        operation, wait, MaxTotalWait);
                    throw;
                }

                logger.LogWarning("{Operation}: rate limited ({Message}); waiting {Wait} (reset {ResetAt:u})",
                    operation, ex.Message, wait, ex.ResetAt);
                _waited += wait;
                await Delay(wait, cancellationToken);
            }
        }
    }
}
