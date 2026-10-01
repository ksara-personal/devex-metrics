using Metrics.ADO.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Metrics.ADO.Migrations.Postgres;

public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ADOMetricsDbContext>
{
    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public ADOMetricsDbContext CreateDbContext(string[] args)
    {
        var currentDir = Path.Combine(Environment.CurrentDirectory, "..");
        // Build config from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)  // Important for EF CLI
            .AddJsonFile(Path.Combine(currentDir, "../../../configs/appsettings.json"))
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ADOMetricsDbContext>();
        var connectionString = GetConnectionString(configuration, DataStoreType.Postgres);
        optionsBuilder.UseNpgsql(connectionString, action =>
        {
            action.MigrationsAssembly("Metrics.ADO.Migrations.Postgres");
            action.MigrationsHistoryTable(ADOMetricsDbContext.ADOMigrationTable);
        })
        .UseSnakeCaseNamingConvention();

        return new ADOMetricsDbContext(optionsBuilder.Options, configuration);
    }

    protected string GetConnectionString(IConfiguration configuration, DataStoreType dataStoreType = DataStoreType.InMemory)
    {
        var connectionString = configuration.GetConnectionString(dataStoreType.ToString());
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException($"{dataStoreType} connection string is not configured");

        return connectionString;
    }
}
