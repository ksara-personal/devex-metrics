using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metrics.MCP.StreamableHTTP;

[JsonSerializable(typeof(PRMetricsEx))]
[JsonSerializable(typeof(Team))]
[JsonSerializable(typeof(PRContributor))]
[JsonSerializable(typeof(ReviewerDailyMetrics))]
[JsonSerializable(typeof(CopilotReviewMetrics))]
[JsonSerializable(typeof(CopilotReviewSummary))]
[JsonSerializable(typeof(IEnumerable<AuthorMetrics>))]
[JsonSerializable(typeof(AuthorMetrics))]
[JsonSerializable(typeof(Microsoft.OData.ODataError))]
[JsonSerializable(typeof(System.Linq.EnumerableQuery<Metrics.Domain.AuthorMetrics>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class ODataJsonSerializerContext : JsonSerializerContext
{
}
