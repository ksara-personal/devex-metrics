using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics;

/// <summary>
/// Class MetricProvider.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract class MetricProvider<T> where T : PullRequestData
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    protected MetricProvider(ILogger<MetricProvider<T>> logger) => this.Logger = logger;

    /// <summary>
    /// Calculates the metrics.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="metrics"></param>
    public abstract void CalculateAndAddMetric(T source, PRMetrics metrics);

    /// <summary>
    /// Logger
    /// </summary>
    protected ILogger Logger { get; private set; }

    /// <summary>
    /// Sets the time elapsed value ensuring it is not negative.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="setValue"></param>
    protected void SetTimeElapsed(TimeSpan span, Action<TimeSpan> setValue) =>
        setValue(span.TotalHours < 0 ? TimeSpan.Zero : span);
}