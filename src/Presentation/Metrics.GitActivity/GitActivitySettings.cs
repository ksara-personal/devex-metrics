namespace Metrics.GitActivity;

/// <summary>
/// Binds the <c>ActivityCollector</c> configuration section.
/// </summary>
public class GitActivitySettings
{
    public const string SectionName = "GitActivity";

    /// <summary>GitHub PAT. The <c>GITHUB_TOKEN</c> environment variable wins when set.</summary>
    public string? PAT { get; set; }

    public string DatabasePath { get; set; } = "gitactivity.db";

    /// <summary>Branch to collect commits from; the default branch when empty.</summary>
    public string? CommitBranch { get; set; }

    /// <summary>Total minutes one run may wait on GitHub rate limits before stopping (resume by re-running).</summary>
    public int RateLimitMaxWaitMinutes { get; set; } = 60;

    /// <summary>Created-date span of one PR search query; narrowed automatically past 1000 results.</summary>
    public int SearchChunkDays { get; set; } = 30;

    /// <summary>GitHub login -> email, for users whose GitHub email is private.</summary>
    public Dictionary<string, string> UserEmails { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
