using Metrics.ADO.EF;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Metrics.ADO;

/// <summary>
/// ADO Extension Configuration Support
/// </summary>
sealed class ADOServiceConfigurator : IServiceConfigurator
{
    internal const string ADOSectionName = "ADO";

    /// <summary>
    /// Configures the services for the ADO extension.
    /// Validation of tenant-specific settings occurs when services are resolved within tenant scope.
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Register ADO clients as scoped for tenant-specific configuration
        services.AddScoped(serviceProvider =>
        {
            var tenantConfig = serviceProvider.GetRequiredService<ITenantSettingsProvider>();
            var tenantSettings = tenantConfig.GetSettings<ADOSettings>(ADOServiceConfigurator.ADOSectionName)
                ?? throw new InvalidOperationException("ADO settings not configured for tenant");

            var organizationUrl = $"https://dev.azure.com/{tenantSettings.Organization}";
            var connection = new VssConnection(new Uri(organizationUrl), new VssBasicCredential(string.Empty, tenantSettings.PersonalAccessToken));
            return connection;
        })
        .AddScoped(serviceProvider =>
        {
            var connection = serviceProvider.GetRequiredService<VssConnection>();
            return connection.GetClient<WorkItemTrackingHttpClient>();
        })
        .AddScoped(serviceProvider =>
        {
            var connection = serviceProvider.GetRequiredService<VssConnection>();
            return connection.GetClient<ServiceEndpointHttpClient>();
        })
        .RegisterHttpClient<ADOApiClient>(sp =>
        {
            var tenantConfig = sp.GetRequiredService<ITenantSettingsProvider>();
            var tenantSettings = tenantConfig.GetSettings<ADOSettings>(ADOServiceConfigurator.ADOSectionName)
                ?? throw new InvalidOperationException("ADO settings not configured for tenant");

            if (string.IsNullOrEmpty(tenantSettings.Organization))
            throw new ArgumentNullException(nameof(tenantSettings.Organization), $"Organization value not found for {nameof(ADOSettings)}");

            if (string.IsNullOrEmpty(tenantSettings.PersonalAccessToken))
            throw new ArgumentNullException(nameof(tenantSettings.PersonalAccessToken), $"PAT not found for {nameof(ADOSettings)}");

            if (string.IsNullOrEmpty(tenantSettings.Project))
                throw new ArgumentNullException(nameof(tenantSettings.Project), $"Project value not found for {nameof(ADOSettings)}");

            return (tenantSettings.PersonalAccessToken, $"https://analytics.dev.azure.com/{tenantSettings.Organization}/{tenantSettings.Project}/_odata/v4.0-preview/WorkItems");
        }, "ADO WorkItem Extension 1.0")
        .AddScoped<WorkItemMapper>()
        .AddScoped<WorkItemClient>()
        .RegisterMetricDbContext<ADOMetricsDbContext>()
        .AddMigrationRunners<ADOMetricsDbContext>()
        .AddScoped<IMetricsScheduler, ADOMetricsScheduler>()
        .AddScoped<DataSyncService<ADOMetricsDbContext>>()
        .AddKeyedScoped<DataSynchronizer, ADOMetricsSynchronizer>(typeof(ADOMetricsDbContext))
        .AddScoped<ADOMetricsSynchronizer>();
    }
}
