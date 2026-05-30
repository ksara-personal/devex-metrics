using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Metrics.Models;

/// <summary>
/// Base class for metric-related DbContext.
/// Provides common functionality for configuring database providers and managing data store types.
/// Implements IMultiTenantDbContext for Finbuckle.MultiTenant data isolation.
/// </summary>
public abstract class MetricDbContext : DbContext, IMultiTenantDbContext
{
    protected readonly IConfiguration _configuration;
    private readonly DataStoreType? _dataStoreType;

    /// <inheritdoc />
    public ITenantInfo? TenantInfo { get; internal set; }

    /// <inheritdoc />
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Overwrite;

    /// <inheritdoc />
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    static MetricDbContext()
    {
        // Enable legacy timestamp behavior for PostgreSQL
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public MetricDbContext(DbContextOptions options, IConfiguration configuration, IMultiTenantContextAccessor? multiTenantContextAccessor = null) : base(options)
    {
        _configuration = configuration;
        _configuration.GetValue<string>("DataStoreType");
        if (Enum.TryParse<DataStoreType>(_configuration.GetValue<string>("DataStoreType"), true, out var parsedDataStoreType))
        {
            _dataStoreType = parsedDataStoreType;
        }
        TenantInfo = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo;
    }

    /// <summary>
    /// Enforces multi-tenant data isolation on save.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Enforces multi-tenant data isolation on async save.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Case insensitive collation for SQLite. Used for case insensitive string comparisons in the database.
    /// </summary>
    protected virtual string? CaseInsensitiveCollation => _dataStoreType == DataStoreType.Postgres ? null : "NOCASE";

    /// <summary>
    /// Case insensitive column type for SQLite. Used for case insensitive string comparisons in the database.
    /// </summary>
    protected virtual string? CaseInsensitiveColumnType => _dataStoreType == DataStoreType.Postgres ? "citext" : null;

    /// <summary>
    /// DbSet for data migration history. Represents the history of data migrations applied to the database.
    /// </summary>
    /// <value></value>
    public DbSet<DataMigrationHistory> DataMigrations { get; set; }

    /// <summary>
    /// Configures the database provider based on the data store type from configuration.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for the context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && _dataStoreType.HasValue)
        {
            ConfigureProvider(optionsBuilder, _dataStoreType.Value);
        }
    }

    /// <summary>
    /// Configures the database provider based on the specified data store type.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="dataStoreType">The type of data store to configure.</param>
    private void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, DataStoreType dataStoreType)
    {
        switch (dataStoreType)
        {
            case DataStoreType.SQLite:
                var sqliteConnectionString = GetConnectionString(DataStoreType.SQLite);
                optionsBuilder.UseSqlite(sqliteConnectionString, action => BuildSqliteMigrationsInfrastructure(action))
                    .UseSnakeCaseNamingConvention();
                break;

            case DataStoreType.Postgres:
                var postgresConnectionString = GetConnectionString(DataStoreType.Postgres);
                optionsBuilder.UseNpgsql(postgresConnectionString, action => BuildPostgresMigrationsInfrastructure(action))
                    .UseSnakeCaseNamingConvention();
                break;

            case DataStoreType.InMemory:
                optionsBuilder.UseInMemoryDatabase("DevExMetrics-InMemory")
                    .UseSnakeCaseNamingConvention();
                break;

            default:
                throw new NotSupportedException($"Data store type {dataStoreType} is not supported.");
        }
    }

    protected abstract void BuildPostgresMigrationsInfrastructure(NpgsqlDbContextOptionsBuilder action);

    protected abstract void BuildSqliteMigrationsInfrastructure(SqliteDbContextOptionsBuilder action);

    /// <summary>
    /// Gets the connection string to use.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception> <summary>
    ///
    /// </summary>
    /// <returns></returns>
    protected string GetConnectionString(DataStoreType dataStoreType = DataStoreType.InMemory)
    {
        // Get it from the environment variable first before looking in the configuration

        var connectionString = _configuration.GetConnectionString(dataStoreType.ToString());
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException($"{dataStoreType} connection string is not configured");

        return connectionString;
    }
}

/// <summary>
/// Factory class for creating instances of <see cref="DevExMetricDbContext"/> at design time.
/// This is used by EF Core tools to create migrations and update the database.
/// It reads configuration from appsettings.json and environment variables to set up the DbContext.
/// </summary>
public abstract class MetricDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : MetricDbContext
{
    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public TContext CreateDbContext(string[] args)
    {
        // Build config from appsettings.json
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory);  // Important for EF CLI

        ConfigureJsonFiles(configurationBuilder);

        IConfigurationRoot configuration = configurationBuilder.AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        var dataStoreType = configuration.GetValue<string>("DataStoreType");
        Enum.TryParse<DataStoreType>(dataStoreType, true, out var parsedDataStoreType);
        var connectionString = GetConnectionString(configuration, parsedDataStoreType);
        switch (parsedDataStoreType)
        {
            case DataStoreType.SQLite:
                optionsBuilder.UseSqlite(connectionString, action => BuildSqliteMigrationsInfrastructure(action))
                    .UseSnakeCaseNamingConvention();
                break;

            case DataStoreType.Postgres:
                optionsBuilder.UseNpgsql(connectionString, action => BuildPostgresMigrationsInfrastructure(action))
                    .UseSnakeCaseNamingConvention();
                break;

            case DataStoreType.InMemory:
                optionsBuilder.UseInMemoryDatabase("DevExMetrics-InMemory")
                    .UseSnakeCaseNamingConvention();
                break;

            default:
                throw new NotSupportedException($"Data store type {dataStoreType} is not supported.");
        }
        return CreateDbContext(optionsBuilder, configuration, parsedDataStoreType);
    }
    
    protected virtual void BuildPostgresMigrationsInfrastructure(NpgsqlDbContextOptionsBuilder action)
    {
    }

    protected virtual void BuildSqliteMigrationsInfrastructure(SqliteDbContextOptionsBuilder action)
    {
    }

    /// <summary>
    /// Configures the JSON files to be used for configuration. This method can be overridden to customize the configuration sources.
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void ConfigureJsonFiles(IConfigurationBuilder builder)
    {
        var currentDir = Environment.CurrentDirectory;
        builder.AddJsonFile(Path.Combine(currentDir, "../../configs/appsettings.json"))
            .AddJsonFile(Path.Combine(currentDir, "../../configs/appsettings.github.json"), optional: true);
    }
    
    /// <summary>
    /// Gets the connection string for the specified data store type.
    /// </summary>
    /// <param name="dataStoreType"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    protected string GetConnectionString(IConfiguration configuration, DataStoreType dataStoreType = DataStoreType.InMemory)
    {
        var connectionString = configuration.GetConnectionString(dataStoreType.ToString());
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException($"{dataStoreType} connection string is not configured");

        return connectionString;
    }

    /// <summary>
    /// Creates a new instance of the DbContext with the specified options, configuration, and data store type.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    /// <param name="dataStoreType"></param>
    /// <returns></returns>
    protected abstract TContext CreateDbContext(DbContextOptionsBuilder options, IConfiguration configuration, DataStoreType dataStoreType);
}
