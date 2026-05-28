
using System.Diagnostics;
using System.Text.Json;
using Metrics.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


/// <summary>
/// Provides functionality to export metrics data from one persistence service to another.
/// </summary>
namespace Metrics;

/// <summary>
/// Service for exporting metrics from a source persistence service to a target persistence service.
/// This class uses dependency injection to resolve the appropriate source and target persistence services
/// based on the specified <see cref="DataStoreType"/>. It supports bulk export of metrics and run status data
/// between different data stores (e.g., SQLite, Postgres, SQL Server, or file-based stores).
/// </summary>
public sealed class MetricsExportService
{
    /// <summary>
    /// The service provider used to resolve persistence services for different data stores.
    /// </summary>
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Logger for export progress and errors.
    /// </summary>
    readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for export events.</param>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> or <paramref name="logger"/> is null.</exception>
    public MetricsExportService(ILogger<MetricsExportService> logger, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all metrics and run status data from the specified source data store to the target data store.
    /// The export is performed in batches of 100 metrics for efficiency. Both source and target persistence services
    /// are resolved using keyed dependency injection. Throws if the source and target are the same, or if either is not registered.
    /// </summary>
    /// <param name="sourceType">The data store type to export from (e.g., SQLite, Postgres, SQL Server, File).</param>
    /// <param name="targetType">The data store type to export to.</param>
    /// <param name="team">Optional team name to filter metrics by team.</param>
    /// <param name="start">Optional start date to filter metrics by creation date.</param>
    /// <param name="end">Optional end date to filter metrics by creation date.</param
    /// <param name="exportRunStatus">Optional export run status.</param
    /// <returns>A task representing the asynchronous export operation.</returns>
    /// <exception cref="ArgumentException">Thrown if source and target types are the same.</exception>
    /// <exception cref="InvalidOperationException">Thrown if source or target persistence service is not registered.</exception>
    public async Task ExportMetricsAsync(DataStoreType sourceType, DataStoreType targetType, string? team = null, DateTime? start = null, DateTime? end = null, bool exportRunStatus = true)
    {
        if (sourceType == targetType)
        {
            throw new ArgumentException("Source and target data store types cannot be the same.");
        }

        // Resolve the source and target persistence services using keyed DI
        var source = _serviceProvider.GetRequiredKeyedService<IMetricsPersistenceService>(sourceType);
        var target = _serviceProvider.GetRequiredKeyedService<IMetricsPersistenceService>(targetType);

        if (source == null || target == null)
        {
            throw new InvalidOperationException("Source or target persistence service is not registered.");
        }

        _logger.LogInformation("Exporting metrics from {Source} to {Target}", sourceType, targetType);

        Stopwatch sw = Stopwatch.StartNew();
        var metrics = source.GetAllMetricsAsync(team, start, end, targetType != DataStoreType.File);
        await foreach (var batch in metrics)
        {
            var batchMetrics = batch;
            // serialize to json and restore from it.
            if (sourceType != DataStoreType.File)
            {
                var json = JsonSerializer.Serialize(batch, PRMetricsContext.Default.IEnumerablePRMetrics);
                var deserializedMetrics = JsonSerializer.Deserialize<IEnumerable<PRMetrics>>(json, PRMetricsContext.Default.IEnumerablePRMetrics);
                foreach (var metric in deserializedMetrics)
                {
                    if (metric.Team is null && !string.IsNullOrEmpty(metric.TeamName))
                    {
                        metric.Team = new Team { Name = metric.TeamName, Region = metric.TeamRegion, ValueStream = metric.ValueStream };
                    }
                }
                batchMetrics = deserializedMetrics;
            }
            await target.InsertIfNotExistsRangeAsync(batchMetrics);
        }
        if (exportRunStatus)
        {
            _logger.LogInformation("Updating last run time.");
            var runStatuses = await source.GetRunStatusesAsync();
            foreach (var status in runStatuses)
            {
                status.Id = 0;
            }
            await target.InsertRunStatusAsync(runStatuses);
        }

        sw.Stop();
        _logger.LogInformation("Metrics export completed in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }
}
