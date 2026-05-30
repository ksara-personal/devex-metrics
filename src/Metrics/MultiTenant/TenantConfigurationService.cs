using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Metrics.MultiTenant;

/// <summary>
/// Service for accessing tenant-specific configuration.
/// Resolves values using a layered strategy:
///   1. Tenant's inline Settings dictionary
///   2. Tenant's ConfigurationFile (e.g., appsettings.tenant-1.json)
///   3. Global application configuration
/// </summary>
public sealed class TenantConfigurationService
{
    readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    readonly IConfiguration _configuration;
    readonly TenantConfigurationProvider _tenantConfigProvider;

    public TenantConfigurationService(
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        IConfiguration configuration,
        TenantConfigurationProvider tenantConfigProvider)
    {
        _tenantAccessor = tenantAccessor;
        _configuration = configuration;
        _tenantConfigProvider = tenantConfigProvider;
    }

    /// <summary>
    /// Gets a strongly-typed settings object by binding from global config first,
    /// then overlaying tenant-specific values from the tenant's ConfigurationFile.
    /// This ensures partial tenant configs correctly override only the specified values.
    /// </summary>
    /// <typeparam name="T">The settings type to bind to</typeparam>
    /// <param name="sectionName">The configuration section name (e.g., "GitHub", "ADO")</param>
    /// <returns>The merged settings object, or null if the section doesn't exist in any config</returns>
    public T? GetSettings<T>(string sectionName) where T : class
    {
        var globalSection = _configuration.GetSection(sectionName);
        if (!globalSection.Exists())
        {
            var tenantOnlySection = GetTenantSection(sectionName);
            return tenantOnlySection != null && tenantOnlySection.Exists()
                ? tenantOnlySection.Get<T>()
                : null;
        }

        var settings = globalSection.Get<T>();
        if (settings == null) 
            return null;

        // Overlay tenant-specific values (includes env var overrides)
        var tenantSection = GetTenantSection(sectionName);
        if (tenantSection != null && tenantSection.Exists())
        {
            tenantSection.Bind(settings);
        }
        return settings;
    }

    /// <summary>
    /// Gets a configuration section for the tenant, checking the tenant's ConfigurationFile first,
    /// then falling back to global configuration if not found.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    IConfigurationSection? GetTenantSection(string sectionName)
    {
        var tenant = _tenantAccessor.MultiTenantContext?.TenantInfo;
        if (tenant == null)
            throw new InvalidOperationException("No tenant context available. Ensure tenants are registered in the configuration.");
        
        var tenantCfg = _tenantConfigProvider.GetConfiguration(tenant?.ConfigurationFile, tenant?.Id);
        var tenantSection = tenantCfg?.GetSection(sectionName);
        return tenantSection != null && tenantSection.Exists() ? tenantSection : null;
    }
}
