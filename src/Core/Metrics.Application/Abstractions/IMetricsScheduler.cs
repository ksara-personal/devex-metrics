using System;

namespace Metrics.Application;

/// <summary>
/// Interface for a metrics scheduler that runs asynchronously.
/// </summary>
public interface IMetricsScheduler
{
    /// <summary>
    /// Gets the priority of the metrics scheduler.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Runs the metrics scheduler asynchronously.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
