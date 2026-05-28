using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication;

namespace Metrics.MCP.StreamableHTTP.Authentication;

/// <summary>
/// Extension methods for registering Basic Authentication
/// </summary>
static class BasicAuthenticationExtensions
{
    /// <summary>
    /// Adds Basic Authentication scheme with a specific scheme name
    /// </summary>
    /// <param name="builder">The authentication builder</param>
    /// <param name="authenticationScheme">The authentication scheme name</param>
    /// <param name="configureOptions">Configuration options</param>
    /// <returns>The authentication builder</returns>
    public static AuthenticationBuilder AddApiKeyInBasic(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<ApiKeyOptions>? configureOptions = null)
    {
        return builder.AddScheme<ApiKeyOptions, BasicAuthenticationHandler>(authenticationScheme, authenticationScheme, configureOptions);
    }
}
