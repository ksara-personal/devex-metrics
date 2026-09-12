using System;
using Microsoft.Extensions.Logging;

namespace Metrics.Application;

/// <summary>
/// Base class for data synchronizers.
/// </summary>
public abstract class DataSynchronizer
{
    protected readonly ILogger _logger;
    protected readonly IMetricsPersistenceService _persistenceService;
    public static readonly DateTime DefaultStartDate = DateTime.SpecifyKind(DateTime.Parse("2023-01-01"), DateTimeKind.Utc);
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSynchronizerEx"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="persistenceService"></param>
    protected DataSynchronizer(ILogger<DataSynchronizer> logger,
        IMetricsPersistenceService persistenceService)
    {
        _logger = logger;
        _persistenceService = persistenceService;
    }
    /// <summary>
    /// Writes the metrics to the persistence store.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public abstract Task WriteMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Updates the metrics in the persistence store.
    /// </summary>
    /// <returns></returns>
    public abstract Task<int> UpdateMetricsAsync();
    //internal abstract Task<PRMetrics> GetPRMetricsAsync(string owner, string repo, int prNumber);
}
