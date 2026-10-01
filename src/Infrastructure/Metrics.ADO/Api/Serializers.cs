using System.Text.Json.Serialization;
using Metrics.ADO.Models;

namespace Metrics.ADO;

[JsonSerializable(typeof(WorkItemWithGitHubLink))]
[JsonSerializable(typeof(IEnumerable<WorkItemWithGitHubLink>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class WorkItemWithGitHubLinkRootContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for <see cref="WorkItemDto"/> and related collections.
/// Used for source generation of serialization metadata.
/// </summary>
[JsonSerializable(typeof(WorkItemDto))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(IEnumerable<WorkItemDto>))]
[JsonSerializable(typeof(List<WorkItemDto>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class WorkItemDtoJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(WorkItemRevisionDto))]
[JsonSerializable(typeof(IEnumerable<WorkItemRevisionDto>))]
[JsonSerializable(typeof(IReadOnlyList<WorkItemRevisionDto>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class WorkItemRevisionDtoJsonContext : JsonSerializerContext
{
}

/// <summary>
/// JSON serialization context for integer list types such as <see cref="IList{int}"/>,
/// <see cref="IEnumerable{int}"/>, and <see cref="IReadOnlyList{int}"/>.
/// Used for source generation of serialization metadata.
/// </summary>
[JsonSerializable(typeof(IList<int>))]
[JsonSerializable(typeof(IEnumerable<int>))]
[JsonSerializable(typeof(IReadOnlyList<int>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class IntListJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(HierarchyQueryRoot))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class HierarchyQueryRootJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(HierarchyQueryResultRoot))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class HierarchyQueryResultRootJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(WorkItemMetrics))]
[JsonSerializable(typeof(IEnumerable<WorkItemMetrics>))]
[JsonSourceGenerationOptions(WriteIndented = true,GenerationMode = JsonSourceGenerationMode.Default)]
internal partial class WorkItemMetricsJsonContext : JsonSerializerContext
{
}
