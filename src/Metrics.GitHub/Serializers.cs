using System.Text.Json.Serialization;

namespace Metrics.GitHub;

/// <summary>
/// JSON serialization context for <see cref="GitHubPRRoot"/> using source generation.
/// </summary>
[JsonSerializable(typeof(GitHubPRRoot))]
[JsonSerializable(typeof(DateTime?))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class GitHubPRRootContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for <see cref="OrganizationRoot"/> using source generation.
/// </summary>
[JsonSerializable(typeof(OrganizationRoot))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class OrganizationRootContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for <see cref="ViewerRoot"/> using source generation.
/// </summary>
[JsonSerializable(typeof(ViewerRoot))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class ViewerRootContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for <see cref="SearchRoot"/> using source generation.
/// </summary>
[JsonSerializable(typeof(SearchRoot))]
[JsonSerializable(typeof(DateTime?))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class SearchRootContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(SearchStateRoot))]
[JsonSerializable(typeof(DateTime?))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class SearchStateRootContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for <see cref="TeamRoster"/> and related collection types using source generation.
/// </summary>
[JsonSerializable(typeof(TeamRoster))]
[JsonSerializable(typeof(IReadOnlyList<TeamRoster>))]
[JsonSerializable(typeof(IEnumerable<TeamRoster>))]
[JsonSerializable(typeof(HashSet<TeamRoster>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class TeamRosterContext : JsonSerializerContext
{
}