using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol.Server;

namespace Metrics.ADO.MCP;

/// <summary>
/// MCP server tool for Azure DevOps work item operations.
/// Provides methods to get work item details, query by WIQL, create sub-tasks, and fetch multiple work items by IDs.
/// </summary>
[McpServerToolType]
public sealed class ADOWorkItemTool
{
    readonly WorkItemClient _wiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADOWorkItemTool"/> class.
    /// </summary>
    /// <param name="client">The work item client to use for operations.</param>
    public ADOWorkItemTool(WorkItemClient client) => _wiClient = client;

    /// <summary>
    /// Gets the work item details for the given id.
    /// </summary>
    /// <param name="workItemId">The work item ID.</param>
    /// <returns>Serialized work item details as JSON.</returns>
    //[McpServerTool, Description("Gets the work item details for the given id")]
    public async Task<string> GetWorkItem(int workItemId) =>
        await GetSerializedValue(() => _wiClient.GetWorkItemAsync(workItemId), WorkItemDtoJsonContext.Default.WorkItemDto);

    /// <summary>
    /// Gets work item details using a WIQL query.
    /// </summary>
    /// <param name="wiql">The WIQL query string.</param>
    /// <param name="top">Maximum number of results to return (default 100).</param>
    /// <returns>Serialized list of work items as JSON.</returns>
    [McpServerTool, Description("Gets the work item details by work item query language")]
    public async Task<string> GetWorkItemsByWiql(string wiql, int top = 100)
    {
        if (string.IsNullOrEmpty(wiql))
            throw new ArgumentNullException(nameof(wiql), "Work item query language query must not be empty");

        return await GetSerializedValue(() => _wiClient.GetWorkItemsByWiql(wiql, top), IntListJsonContext.Default.IEnumerableInt32);
    }

    /// <summary>
    /// Gets details for multiple work items by their IDs.
    /// </summary>
    /// <param name="workItemIds">Enumerable of work item IDs.</param>
    /// <returns>Serialized list of work items as JSON.</returns>
    //[McpServerTool, Description("Gets the work item details by work item query language")]
    public async Task<string> GetWorkItemsByIds(IEnumerable<int> workItemIds) =>
        await GetSerializedValue(() => _wiClient.GetWorkItemsAsync(workItemIds), WorkItemDtoJsonContext.Default.IEnumerableWorkItemDto);

    /// <summary>
    /// Gets the work item revisions or history details.
    /// </summary>
    /// <param name="workItemId"></param>
    /// <returns></returns>
    [McpServerTool, Description("Gets the work item revisions or history details")]
    public async Task<string> GetWorkItemRevisionsAsync(int workItemId) =>
        await GetSerializedValue(() => _wiClient.GetWorkItemRevisionsAsync(workItemId), WorkItemRevisionDtoJsonContext.Default.IReadOnlyListWorkItemRevisionDto);

    /// <summary>
    /// Helper method to serialize the result of an async operation.
    /// </summary>
    /// <typeparam name="T">The type of the result to serialize.</typeparam>
    /// <param name="func">The async function to execute.</param>
    /// <param name="typeInfo">The JSON type info for serialization.</param>
    /// <returns>Serialized result as JSON string.</returns>
    internal static async Task<string> GetSerializedValue<T>(Func<Task<T>> func, JsonTypeInfo<T> typeInfo) => JsonSerializer.Serialize(await func(), typeInfo);
}
