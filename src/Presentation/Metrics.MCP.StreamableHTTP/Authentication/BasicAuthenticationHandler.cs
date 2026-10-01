using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;

namespace Metrics.MCP.StreamableHTTP.Authentication;

/// <summary>
/// Basic authentication handler.
/// </summary>
public sealed class BasicAuthenticationHandler : ApiKeyHandlerBase
{
    const string DefaultScheme = "Basic";

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="encoder"></param>
    public BasicAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Gets the WWW-Authenticate header value for the challenge response.
    /// </summary>
    /// <returns></returns>
    protected override string GetWwwAuthenticateInParameter() => DefaultScheme;

    /// <summary>
    /// Parses the API key from the request.
    /// </summary>
    /// <returns></returns>
    protected override Task<string> ParseApiKeyAsync()
    {
        if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var value) && value.Count > 0 &&
            value[0].StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            var token = value[0]["Basic ".Length..].Trim();
            var credentialsBytes = Convert.FromBase64String(token);
            var credentials = Encoding.UTF8.GetString(credentialsBytes);

            var parts = credentials.Split(':', 2);
            if (parts.Length == 2)
            {
                return Task.FromResult(parts[1]);
            }
        }
        return Task.FromResult(string.Empty);
    }
}
