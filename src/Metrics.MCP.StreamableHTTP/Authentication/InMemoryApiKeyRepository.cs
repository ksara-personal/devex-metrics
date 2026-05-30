using AspNetCore.Authentication.ApiKey;
using Metrics.Models;
using Metrics.MultiTenant;
using Microsoft.Extensions.Options;

namespace Metrics.MCP.StreamableHTTP.Authentication;

/// <summary>
/// In-memory implementation of the API key repository.
/// </summary>
public sealed class InMemoryApiKeyRepository : IApiKeyRepository
{
    readonly AuthenticationSettings _settings;
    readonly List<IApiKey> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryApiKeyRepository"/> class.
    /// </summary>
    /// <param name="tenantConfigurationService"></param>
    public InMemoryApiKeyRepository(TenantConfigurationService tenantConfigurationService)
    {
        _settings = tenantConfigurationService.GetSettings<AuthenticationSettings>(ConfigSectionNames.Authentication)
            ?? throw new InvalidOperationException("Authentication settings not configured");

        _cache.Add(new ApiKey(_settings.ApiKey, "Admin"));
    }

    /// <summary>
    /// Gets the API key by its value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<IApiKey> GetApiKeyAsync(string key)
    {
        var apiKey = _cache.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(apiKey);
    }
}
