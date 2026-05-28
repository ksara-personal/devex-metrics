using System.Text.Json;

namespace Metrics.Extensions;


/// <summary>
/// Extensions for working with HttpResponseMessage.
/// </summary>
static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Parses the rate limit information from the response.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public static async Task<RateLimitInfo> ParseRateLimitErrorAsync(this HttpResponseMessage response)
    {
        var info = new RateLimitInfo();

        if (response.Headers.TryGetValues("X-RateLimit-Limit", out var limitHeader))
            info.Limit = int.TryParse(limitHeader.FirstOrDefault(), out var limit) ? limit : null;

        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingHeader))
            info.Remaining = int.TryParse(remainingHeader.FirstOrDefault(), out var remaining) ? remaining : null;

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetHeader))
        {
            if (long.TryParse(resetHeader.FirstOrDefault(), out var resetEpoch))
                info.ResetAt = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);
        }

        var json = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("errors", out var errors))
                {
                    var firstError = errors[0].GetProperty("message").GetString();
                    info.Message = firstError;
                }

                if (root.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("rateLimit", out var rateLimit))
                {
                    if (rateLimit.TryGetProperty("limit", out var limitElem))
                        info.Limit ??= limitElem.GetInt32();

                    if (rateLimit.TryGetProperty("remaining", out var remElem))
                        info.Remaining ??= remElem.GetInt32();

                    if (rateLimit.TryGetProperty("cost", out var costElem))
                        info.Cost = costElem.GetInt32();

                    if (rateLimit.TryGetProperty("resetAt", out var resetElem))
                    {
                        var resetStr = resetElem.GetString();
                        if (DateTimeOffset.TryParse(resetStr, out var parsed))
                            info.ResetAt ??= parsed;
                    }
                }
            }
            catch (JsonException) { }
        }
        return info;
    }
}
