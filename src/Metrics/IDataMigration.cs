using System.Threading.Tasks;
using Metrics.EF;
using Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Metrics.DataMigrations;

/// <summary>
/// Represents a data migration.
/// </summary>
public interface IDataMigration<TContext> where TContext : MetricDbContext
{
    /// <summary>
    /// Gets the entity framework identifier for the data migration.
    /// </summary>
    /// <value></value>
    string EFMigrationId { get; }

    /// <summary>
    /// Runs the data migration.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task RunAsync(TContext context);
}

/// <summary>
/// Base class for data migrations.
/// </summary>
public abstract class DataMigration<TContext> : IDataMigration<TContext> where TContext : MetricDbContext
{
    protected static readonly DateTime UpdatedAtDefault = new(2000, 1, 1);
    protected readonly ILogger _logger;
    protected readonly DataSynchronizer _dataSynchronizer;

    /// <summary>
    /// Constructs a new instance of the migration.
    /// </summary>
    /// <param name="dataSynchronizer"></param>
    /// <param name="logger"></param>
    protected DataMigration(DataSynchronizer dataSynchronizer, ILogger<DataMigration<TContext>> logger)
    {
        _dataSynchronizer = dataSynchronizer;
        _logger = logger;
    }

    public abstract string EFMigrationId { get; }

    public abstract Task RunAsync(TContext context);
}
