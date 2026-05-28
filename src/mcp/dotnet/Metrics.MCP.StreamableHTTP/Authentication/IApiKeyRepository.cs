using System;
using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace Metrics.MCP.StreamableHTTP.Authentication;

/// <summary>
/// Interface for API key repository.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Gets the API key by its value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<IApiKey> GetApiKeyAsync(string key);
}

/// <summary>
/// Represents an API key.
/// </summary>
public sealed class ApiKey : IApiKey
{
    public ApiKey(string key, string owner, List<Claim> claims = null)
    {
        Key = key;
        OwnerName = owner;
        Claims = claims ?? new List<Claim>();
    }

    public string Key { get; }
    public string OwnerName { get; }
    public IReadOnlyCollection<Claim> Claims { get; }
}
