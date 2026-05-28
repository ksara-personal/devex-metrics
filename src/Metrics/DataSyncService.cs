using Metrics.EF;
using Metrics.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics;

/// <summary>
/// Data synchronization service for updating and writing metrics.
/// </summary>
public sealed class DataSyncService<TKey> : DataSynchronizer where TKey : MetricDbContext
{
    readonly IServiceProvider _serviceProvider;
    public DataSyncService(ILogger<DataSyncService<TKey>> logger,
        IServiceProvider serviceProvider,
        IMetricsPersistenceService persistenceService)
        : base(logger, persistenceService) => _serviceProvider = serviceProvider;

    /// <summary>
    /// Updates the metrics for all registered data synchronizers.
    /// </summary>
    /// <returns></returns>
    public override async Task<int> UpdateMetricsAsync()
    {
        int totalRecordsAffected = 0;
        var synchronizers = _serviceProvider.GetKeyedServices<DataSynchronizer>(typeof(TKey));
        foreach (var synchronizer in synchronizers)
        {
            totalRecordsAffected += await synchronizer.UpdateMetricsAsync();
        }
        return totalRecordsAffected;
    }

    /// <summary>
    /// Writes the metrics for all registered data synchronizers.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public override async Task WriteMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        DateTime endUtc = endDate.HasValue ? endDate.Value : DateTime.UtcNow;
        endUtc = endUtc > DateTime.UtcNow ? DateTime.UtcNow : endUtc;
        
        var synchronizers = _serviceProvider.GetKeyedServices<DataSynchronizer>(typeof(TKey));
        foreach (var synchronizer in synchronizers)
        {
            _logger.LogInformation("Write metrics started at {Now}, Using start:{start} and end:{end}", DateTime.Now, startDate, endUtc);
            await synchronizer.WriteMetricsAsync(startDate, endUtc);
            _logger.LogInformation("Write metrics completed at {End}", endUtc);

            _logger.LogInformation("Updating open PR's state changes started");
            int recordsAffected = await synchronizer.UpdateMetricsAsync();
            _logger.LogInformation("Updating open PR's state changes completed, Total records affected: {RecordsAffected}", recordsAffected);
        }
    }
}
