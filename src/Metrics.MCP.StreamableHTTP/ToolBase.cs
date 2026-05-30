using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace Metrics.MCP;

/// <summary>
/// Base class for all metrics tools.
/// </summary>
public abstract class ToolBase
{
    protected readonly DataClient _client;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="connector"></param>
    protected ToolBase([FromServices] DataClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Executes the given function and serializes the result to JSON.
    /// This is a generic method that can be used to execute any function that returns a Task<T>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async Task<string> GetMetricsAsync<T>(
        DateTime start, DateTime end, Func<DateTime, DateTime, Task<T>> func, JsonTypeInfo<T> typeInfo) =>
        JsonSerializer.Serialize(await func(start, end < start ? DateTime.Now : end), typeInfo);

    /// <summary>
    /// Builds a CallToolResult containing the given data serialized as CSV in an embedded resource.
    /// </summary>
    /// <param name="data">The data to serialize to CSV.</param>
    /// <param name="fileName">The file name to use for the embedded CSV resource.</param>
    /// <typeparam name="T">The type of the data elements.</typeparam>
    /// <returns>A CallToolResult containing the CSV data as an embedded resource.</returns>
    protected async Task<CallToolResult> BuildEmbeddedResourceResult<T>(IEnumerable<T> data, string fileName)
    {
        var csv = await data.WriteToCsvData<T>();
        var result = new CallToolResult();
        result.Content.Add(new EmbeddedResourceBlock()
        {
            Resource = new BlobResourceContents()
            {
                Uri = fileName,
                MimeType = "text/csv",
                Blob = System.Text.Encoding.UTF8.GetBytes(csv),
            }
        });
        return result;
    }
}
