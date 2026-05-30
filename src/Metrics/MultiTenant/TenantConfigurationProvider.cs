using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Metrics.MultiTenant;

/// <summary>
/// Loads and caches tenant-specific IConfiguration instances from their ConfigurationFile paths.
/// Supports environment variable overrides using the format: {tenantId}__{section}__{key}
/// Example: tenant-1__GitHub__PAT=xxx overrides GitHub.PAT for the "tenant-1" tenant.
/// Registered as a singleton so each tenant's config file is loaded only once.
/// </summary>
public sealed class TenantConfigurationProvider
{
    readonly ConcurrentDictionary<string, IConfigurationRoot> _cache = new(StringComparer.OrdinalIgnoreCase);
    readonly string _basePath;
    readonly ILogger<TenantConfigurationProvider> _logger;

    public TenantConfigurationProvider(string basePath, ILogger<TenantConfigurationProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;
    }

    /// <summary>
    /// Gets the <see cref="IConfigurationRoot"/> for the given tenant configuration file and tenant ID.
    /// Returns null if the file name is empty or the file does not exist.
    /// Results are cached so the file is only read once per unique file name + tenant ID combination.
    /// 
    /// Environment variables can override settings using: {tenantId}__{section}__{key}
    /// Example: tenant-1__GitHub__PAT=xxx
    /// </summary>
    /// <param name="configurationFile">The tenant-specific configuration file name (e.g., "appsettings.tenant-1.json").</param>
    /// <param name="tenantId">The tenant identifier (e.g., "tenant-1", "tenant-2") used for env var prefix.</param>
    public IConfigurationRoot? GetConfiguration(string? configurationFile, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(configurationFile))
            return null;

        var cacheKey = string.IsNullOrWhiteSpace(tenantId) 
            ? configurationFile 
            : $"{configurationFile}|{tenantId}";

        return _cache.GetOrAdd(cacheKey, _ =>
        {
            var fullPath = Path.Combine(_basePath, configurationFile);
            var builder = new ConfigurationBuilder();

            if (File.Exists(fullPath))
            {
                _logger.LogInformation("Loading tenant configuration from {FilePath}", fullPath);
                builder.AddJsonFile(fullPath, optional: false, reloadOnChange: true);
            }
            else
            {
                _logger.LogWarning("Tenant configuration file not found: {FilePath}", fullPath);
            }

            // Add tenant-specific environment variables with prefix: {tenantId}__
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var prefix = $"{tenantId}__";
                _logger.LogDebug("Adding environment variables with prefix: {Prefix}", prefix);
                builder.AddEnvironmentVariables(prefix);
            }

            return builder.Build();
        });
    }
}
