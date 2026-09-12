using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Metrics.GitHub.Migrations.Postgres;

public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DevExMetricDbContext>
{
    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public DevExMetricDbContext CreateDbContext(string[] args)
    {
        var currentDir = Path.Combine(Environment.CurrentDirectory, "..");
        // Build config from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)  // Important for EF CLI
            .AddJsonFile(Path.Combine(currentDir, "../../../configs/appsettings.json"))
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DevExMetricDbContext>();
        var connectionString = GetConnectionString(configuration, DataStoreType.Postgres);
        optionsBuilder.UseNpgsql(connectionString, action =>
        {
            action.MigrationsAssembly("Metrics.GitHub.Migrations.Postgres");
        })
        .UseSnakeCaseNamingConvention();

        return new DevExMetricDbContext(optionsBuilder.Options, configuration);
    }

    protected string GetConnectionString(IConfiguration configuration, DataStoreType dataStoreType = DataStoreType.InMemory)
    {
        var connectionString = configuration.GetConnectionString(dataStoreType.ToString());
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException($"{dataStoreType} connection string is not configured");

        return connectionString;
    }
}
