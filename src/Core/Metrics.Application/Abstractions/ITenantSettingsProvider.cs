namespace Metrics.Application;

/// <summary>
/// Port for reading tenant-scoped configuration.
/// </summary>
/// <remarks>
/// Application code needs per-tenant settings but must not know how a tenant is
/// resolved or where its configuration lives. The infrastructure layer supplies the
/// implementation (see <c>ITenantSettingsProvider</c>), which layers the tenant's
/// own configuration file and environment overrides on top of global configuration.
/// </remarks>
public interface ITenantSettingsProvider
{
    /// <summary>
    /// Binds a configuration section for the current tenant.
    /// </summary>
    /// <typeparam name="T">The settings type to bind to.</typeparam>
    /// <param name="sectionName">The configuration section name (for example "GitHub" or "ADO").</param>
    /// <returns>The merged settings, or <c>null</c> when the section is absent from every source.</returns>
    T? GetSettings<T>(string sectionName) where T : class;
}
