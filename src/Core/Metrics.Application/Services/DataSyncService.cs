using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics.Application;

/// <summary>
/// Data synchronization service that fans out to every <see cref="DataSynchronizer"/>
/// registered under the <typeparamref name="TKey"/> service key.
/// </summary>
/// <typeparam name="TKey">
/// Type used purely as the DI service key that groups a set of synchronizers - callers key
/// on the DbContext type, but nothing here touches the context itself.
/// </typeparam>
public sealed class DataSyncService<TKey> : DataSynchronizer where TKey : class
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
