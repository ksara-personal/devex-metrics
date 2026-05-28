namespace Metrics;

/// <summary>
/// Represents the rate limit information returned by the API.
/// </summary>
internal sealed class RateLimitInfo
{
    public string? Message { get; set; }
    public int? Limit { get; set; }
    public int? Remaining { get; set; }
    public DateTimeOffset? ResetAt { get; set; }
    public int? Cost { get; set; }
    public bool IsNearLimit => Remaining.HasValue && Remaining.Value < 50;
    public bool IsRateLimited => Remaining.HasValue && Remaining.Value <= 0;
    public override string ToString() => $"Limit: {Limit}, Remaining: {Remaining}, ResetsAt: {ResetAt?.ToLocalTime()}, Cost: {Cost}";
}
