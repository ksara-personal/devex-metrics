using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MetricsConsoleApp")]
[assembly: InternalsVisibleTo("Metrics.Tests")]
[assembly: InternalsVisibleTo("Metrics.GitHub")]

namespace Metrics.Application;

/// <summary>
/// Represents a GraphQL post query payload.
/// </summary>
public class PostQuery
{
    /// <summary>
    /// The GraphQL query string to be posted.
    /// </summary>
    public required string query { get; set; }
}

public sealed class PostWithVariables<T> : PostQuery where T : PostWithEmptyVariables
{
    public required T variables { get; set; }
}

public record PostWithEmptyVariables();
public record PostVariablesWithOwner(string owner) : PostWithEmptyVariables();
public record PostWithQueryStringVariable(string queryString, string after) : PostWithEmptyVariables();
public record PostVariables(string owner, string repo) : PostVariablesWithOwner(owner);
public record PostTeamQueryVariables(string owner, string query, string after) : PostVariablesWithOwner(owner);
public record PostPRQueryVariables(string owner, string repo, int prNumber) : PostVariables(owner, repo);
public record PostPRQueryPaginationVariables(string owner, string repo, int prNumber,
    string afterReview, string afterCommit, string afterTimelines, string afterComment,
    int reviewsCount = 100, int commitsCount = 100, int timelinesCount = 100, int commentsCount = 100)
    : PostPRQueryVariables(owner, repo, prNumber);

/// <summary>
/// JSON serialization context for <see cref="PostQuery"/> using source generation.
/// </summary>
[JsonSerializable(typeof(PostQuery))]
[JsonSerializable(typeof(PostWithVariables<PostVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostTeamQueryVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostPRQueryVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostPRQueryPaginationVariables>))]
[JsonSerializable(typeof(PostWithVariables<PostWithQueryStringVariable>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class PostQueryContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for PR metrics and related types using source generation.
/// </summary>
[JsonSerializable(typeof(PRMetrics))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(IEnumerable<PRMetrics>))]
[JsonSerializable(typeof(HashSet<PRMetrics>))]
[JsonSerializable(typeof(ConcurrentDictionary<string, object>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class PRMetricsContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for settings types such as <see cref="GitHubSettings"/> and <see cref="AzureRepoSettings"/>.
/// </summary>
[JsonSerializable(typeof(SourceControlSettings<GitHubOrganization>))]
[JsonSerializable(typeof(DateTime?))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class SettingsJsonContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for enumerable string collections.
/// </summary>
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class EnumerableJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(RunStatus))]
[JsonSerializable(typeof(IReadOnlyList<RunStatus>))]
[JsonSerializable(typeof(IEnumerable<RunStatus>))]
[JsonSerializable(typeof(IList<RunStatus>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class RunStatusJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(ReviewerMetricsSummary))]
[JsonSerializable(typeof(IReadOnlyList<ReviewerMetricsSummary>))]
[JsonSerializable(typeof(IEnumerable<ReviewerMetricsSummary>))]
[JsonSerializable(typeof(IList<ReviewerMetricsSummary>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class ReviewerMetricsSummaryJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(CopilotReviewSummary))]
[JsonSerializable(typeof(IReadOnlyList<CopilotReviewSummary>))]
[JsonSerializable(typeof(IEnumerable<CopilotReviewSummary>))]
[JsonSerializable(typeof(IList<CopilotReviewSummary>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class CopilotReviewSummaryJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(object))]
public partial class PrimitiveJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(Sprint))]
[JsonSerializable(typeof(IEnumerable<Sprint>))]
public partial class SprintJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(AuthorMetrics))]
[JsonSerializable(typeof(IEnumerable<AuthorMetrics>))]
public partial class AuthorMetricsJsonContext : JsonSerializerContext
{
}