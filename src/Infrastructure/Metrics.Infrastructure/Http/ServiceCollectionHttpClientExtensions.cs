using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Metrics.Infrastructure;

/// <summary>
/// Service collection extensions for http client.
/// </summary>
public static class ServiceCollectionHttpClientExtensions
{
    /// <summary>
    /// Registers an HTTP client with the specified personal access token, base address, and user agent.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="baseAddressFactory"></param>
    /// <param name="userAgent"></param>
    /// <typeparam name="TClient"></typeparam>
    /// <returns></returns>
    public static IServiceCollection RegisterHttpClient<TClient>(this IServiceCollection services,
            Func<IServiceProvider, (string? token, string? baseAddress)>? baseAddressFactory,
            string userAgent = "MetricsApp 1.0")
        where TClient : ApiClient
    {
        if (baseAddressFactory is null)
            throw new ArgumentNullException(nameof(baseAddressFactory), "Base address factory must be provided to register the HTTP client.");

        var name = string.Concat(typeof(TClient).Name, "_HttpClient_", Guid.NewGuid().ToString("N"));
        services.AddHttpClient<TClient>(name, (sp,client) =>
        {
            using var scope = sp.CreateScope();
            var (personalAccessToken, baseAddress) = baseAddressFactory.Invoke(scope.ServiceProvider);
            if (string.IsNullOrEmpty(baseAddress) || string.IsNullOrEmpty(personalAccessToken))            
            {
                throw new ArgumentException("Base address and personal access token must be provided for the HTTP client.");
            }

            client.BaseAddress = new Uri(baseAddress ?? string.Empty);
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
        return services;
    }

    /// <summary>
    /// Gets the retry policy for the http client.
    /// This is used to retry the request in case of transient errors.
    /// </summary>
    /// <returns></returns>
    static IAsyncPolicy<System.Net.Http.HttpResponseMessage> GetRetryPolicy()
    {
        return Policy
            .Handle<HttpIOException>()
            .Or<HttpRequestException>()
            .OrResult<System.Net.Http.HttpResponseMessage>(msg =>
                msg.StatusCode == System.Net.HttpStatusCode.BadGateway
                || msg.StatusCode == System.Net.HttpStatusCode.NotFound
                || msg.StatusCode == System.Net.HttpStatusCode.GatewayTimeout
                )
            .OrTransientHttpError()
            .OrTransientHttpStatusCode()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
