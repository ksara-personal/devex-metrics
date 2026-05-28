using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Metrics.Extensions;

namespace Metrics;

/// <summary>
/// Api client that wraps the api calls.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ApiClient
{
    protected readonly ILogger _logger;
    protected readonly HttpClient _httpClient;
    public ApiClient(ILogger<ApiClient> logger, HttpClient client) => (_logger, _httpClient) = (logger, client);

    /// <summary>
    /// Executes the async method with the callback and logs thr time taken for execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="apiCallPrefix"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    protected async Task<TResult> ExecuteAsync<TResult>(string apiCallPrefix, Func<Task<TResult>> func)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await func();
        }
        finally
        {
            sw.Stop();
            // let's optimize the string format for unncessary cpu cycles.
            _logger.LogDebug("{apiCallPrefix} took {Elapsed}, {ElapsedMS} ms", apiCallPrefix, sw.Elapsed, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Gets the teams by pattern.
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="organization"></param>
    /// <returns></returns>
    public abstract Task<TeamMembersData> GetTeamsByPatternAsync(string organization, string pattern);

    /// <summary>
    /// Gets the search results.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="repoFilter"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="endCursor"></param>
    /// <returns></returns>
    public abstract Task<PRSearchData> GetSearchResultsAsync(string repoFilter, DateTime start, DateTime end, string endCursor);

    /// <summary>
    /// Executes the post api with retries.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="query"></param>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    protected async Task<TResult?> ExecutePostWithRetriesAsync<TResult>(string url, string query, JsonTypeInfo<TResult> typeInfo, Func<TResult, bool> errorCallback = null)
    {
        var postQuery = new PostQuery
        {
            query = query
        };
        return await ExecutePostAsync(url, postQuery, PostQueryContext.Default.PostQuery, typeInfo, errorCallback);
    }

    /// <summary>
    /// Executes the post with retries with polly.
    /// </summary>
    /// <typeparam name="TBody"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="bodyTypeInfo"></param>
    /// <param name="resultTypeInfo"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<TResult?> ExecutePostAsync<TBody, TResult>(string url, TBody body, JsonTypeInfo<TBody> bodyTypeInfo, JsonTypeInfo<TResult> resultTypeInfo, Func<TResult, bool> errorCallback = null)
    {
        var response = await _httpClient.PostAsJsonAsync(url, body, bodyTypeInfo);
        if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.BadGateway)
        {
            // throw exception to retry.
            throw new HttpRequestException("Bad gateway error");
        }

        // Throw an exception if response is not successful to trigger retry
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TResult>(resultTypeInfo);
        var isError = errorCallback?.Invoke(result) ?? false;
        if (isError)
        {
            var error = await response.Content.ReadAsStringAsync();
            var rateLimit = await response.ParseRateLimitErrorAsync();
            if (rateLimit?.IsRateLimited == true)
            {
                //_logger.LogError("Rate limit exceeded. Limit: {Limit}, Remaining: {Remaining}, ResetsAt: {ResetsAt}", rateLimit.Limit, rateLimit.Remaining, rateLimit.ResetAt);
                throw new HttpRequestException($"{rateLimit.Message} - {rateLimit}");
            }

            throw new HttpRequestException($"Error in response data: {error}");
        }
        return result;
    }

    /// <summary>
    /// Executes the get request.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<TResult> ExecuteGetWithRetriesAsync<TResult>(string url, JsonTypeInfo<TResult> typeInfo) => await _httpClient.GetFromJsonAsync<TResult>(url, typeInfo);

    /// <summary>
    /// Executes the get request with a relative URL.
    /// </summary>
    /// <param name="relativeUrl"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public async Task<TResult> ExecuteGetWithRetriesAsync2<TResult>(Func<string, string> urlCallback)
    {
        var url = urlCallback(_httpClient.BaseAddress.AbsoluteUri);
        return await _httpClient.GetFromJsonAsync<TResult>(url);
    }
}