using System;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Metrics.Extensions.ADO.Tests;

public abstract class ADOTestBase
{
    protected readonly IHost _host;
    protected ADOTestBase()
    {
        _host = CreateHost();
    }

    IHost CreateHost()
    {
        IConfigurationBuilder configurationBuilder = null;
        var hostBuilder = Host.CreateDefaultBuilder(null)
            .ConfigureAppConfiguration((context, config) => configurationBuilder = config);

        var host = hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddDIServices(configurationBuilder);
        })
        .Build();

        return host;
    }

    /// <summary>
    /// Sets the tenant context for the test.
    /// Use this method before calling services that depend on tenant configuration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier (e.g., "tenant-1", "tenant-2")</param>
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
        
        var multiTenantContext = new Finbuckle.MultiTenant.Abstractions.MultiTenantContext<AppTenantInfo>( tenant );
        contextSetter.MultiTenantContext = multiTenantContext;
    }
}
