namespace Metrics.Domain;

/// <summary>
/// Represents the authentication settings.
/// </summary>
public sealed class AuthenticationSettings
{
    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional Okta authority/issuer, e.g. https://dev-12345.okta.com/oauth2/default
    /// </summary>
    public string? OktaAuthority { get; set; }

    /// <summary>
    /// Optional expected audience for incoming JWTs (aud claim).
    /// </summary>
    public string? OktaAudience { get; set; }

    /// <summary>
    /// Enable Okta/JWT-based authentication when true.
    /// </summary>
    public bool OktaEnabled { get; set; } = false;

    /// <summary>
    /// Optional server URL used as audience when validating tokens. Defaults to localhost:5100 if not set.
    /// </summary>
    public string? ServerUrl { get; set; }
}
