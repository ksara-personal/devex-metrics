using System;

namespace Metrics;

/// <summary>
/// Represents the github states
/// </summary>
public struct PullRequestStates
{
    public const string Open = "OPEN";
    public const string Approved = "APPROVED";
    public const string Commented = "COMMENTED";
    public const string Closed = "CLOSED";
    public const string Merged = "MERGED";
    public const string ChangesRequested = "CHANGES_REQUESTED";
}
