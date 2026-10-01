using Finbuckle.MultiTenant.Abstractions;

namespace Metrics.Infrastructure;

/// <summary>
/// Custom tenant info for the DevMetrics application.
/// Each tenant represents a product or organizational unit (e.g. "tenant-1", "tenant-2").
/// </summary>
public sealed class AppTenantInfo : ITenantInfo
{
    /// <summary>
    /// Unique identifier for the tenant (GUID or similar).
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Human-readable identifier used for tenant resolution (e.g. "tenant-1", "tenant-2").
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Path to a tenant-specific configuration file (e.g., "appsettings.tenant-1.json").
    /// If specified, this file will be loaded and merged with the base configuration.
    /// </summary>
    public string ConfigurationFile { get; set; }
}
