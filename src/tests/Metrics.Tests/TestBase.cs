using System;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Metrics.EF;
using Metrics.GitHub;
using Metrics.Models;
using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Metrics.Tests;

public abstract class TestBase
{
    protected IHost _host { get; }
    protected ILogger _logger { get; }

    protected TestBase()
    {
        _host = CreateHost();
        _logger = _host.Services.GetRequiredService<ILogger<TestBase>>();
    }

    protected DataSyncService<DevExMetricDbContext> GetDataSynchronizer(DataStoreType storeType = DataStoreType.InMemory)
    {
        var services = _host.Services;
        var configService = services.GetRequiredService<ConfigService>();
        configService.DataStoreType = storeType;

        return services.GetRequiredService<DataSyncService<DevExMetricDbContext>>();
    }

    protected GitHubPRMetricsSynchronizer GetMetricsSynchronizer(DataStoreType storeType = DataStoreType.InMemory)
    {
        var services = _host.Services;
        var configService = services.GetRequiredService<ConfigService>();
        configService.DataStoreType = storeType;

        return services.GetRequiredService<GitHubPRMetricsSynchronizer>();
    }

    protected GitHubReviewerMetricsSynchronizer GetReviewerMetricsSynchronizer(DataStoreType storeType = DataStoreType.InMemory)
    {
        var services = _host.Services;
        var configService = services.GetRequiredService<ConfigService>();
        configService.DataStoreType = storeType;

        return services.GetRequiredService<GitHubReviewerMetricsSynchronizer>();
    }

    /// <summary>
    /// Gets a DataClient instance.
    /// This method is used to get a DataClient instance with the specified api mode and database type.
    /// It configures the ConfigService to use the specified online mode and database type.
    /// The DataClient is used to perform various operations such as writing metrics to a file or database,
    /// and querying metrics by ID, team, or author.
    /// </summary>
    /// <param name="dbType"></param>
    /// <returns></returns>
    protected DataClient GetDataClient(DataStoreType storeType = DataStoreType.InMemory)
    {
        var services = _host.Services;
        // run online mode test.

        var configService = services.GetRequiredService<ConfigService>();
        configService.DataStoreType = storeType;

        return services.GetRequiredService<DataClient>();
    }

    IHost CreateHost()
    {
        IConfigurationBuilder configurationBuilder = null;
        var hostBuilder = Host.CreateDefaultBuilder(null)
            .ConfigureAppConfiguration((context, config) => configurationBuilder = config);

        var host = hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddDIServices(configurationBuilder);
            AddServices(services);
        })
        .Build();

        return host;
    }

    /// <summary>
    /// Sets the tenant context for the current test.
    /// </summary>
    /// <param name="tenantId">The tenant identifier (e.g., "tenant-1" or "tenant-2")</param>
    protected void SetTenant(string tenantId)
    {
        var contextSetter = _host.Services.GetRequiredService<IMultiTenantContextSetter>();
        var tenant = new AppTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = tenantId,
            ConfigurationFile = $"appsettings.{tenantId}.json"
        };
        var multiTenantContext = new MultiTenantContext<AppTenantInfo> { TenantInfo = tenant };
        contextSetter.MultiTenantContext = multiTenantContext;
    }

    /// <summary>
    /// Placeholder to add additional services.
    /// </summary>
    /// <param name="services"></param>
    protected virtual void AddServices(IServiceCollection services)
    {
        services.AddLogging(l => l.AddSimpleConsole(c => c.IncludeScopes = true));
    }
}
