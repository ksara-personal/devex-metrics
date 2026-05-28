using System.Globalization;
using System.Text.Json;
using Metrics.ADO.Utils;
using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Metrics.Extensions;

namespace Metrics.ADO;

public sealed class WorkItemClient
{
    readonly WorkItemTrackingHttpClient _client;
    readonly WorkItemMapper _mapper;
    readonly ADOApiClient _apiClient;
    readonly ADOSettings _settings;
    const string HierarchyQueryUrl = "https://dev.azure.com/{0}/_apis/Contribution/HierarchyQuery/project/{1}?api-version=5.0-preview.1";

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItemClient"/> class.
    /// </summary>
    /// <param name="client">The Azure DevOps work item tracking client.</param>
    /// <param name="mapper">The work item mapper.</param>
    /// <param name="apiClient">The ADO API client.</param>
    /// <param name="tenantConfig">Tenant configuration service for resolving tenant-specific ADO settings.</param>
    public WorkItemClient(WorkItemTrackingHttpClient client, WorkItemMapper mapper, ADOApiClient apiClient, TenantConfigurationService tenantConfig)
    {
        _client = client;
        _mapper = mapper;
        _apiClient = apiClient;
        _settings = tenantConfig.GetSettings<ADOSettings>(ADOServiceConfigurator.ADOSectionName)
            ?? throw new InvalidOperationException("ADO settings not configured for tenant");
    }

    /// <summary>
    /// Gets a single work item by its ID and maps it to a DTO.
    /// </summary>
    /// <param name="workItemId">The work item ID.</param>
    /// <param name="expandGitHubLinks">Whether to expand GitHub pull request links.</param>
    /// <returns>The mapped <see cref="WorkItemDto"/>.</returns>
    public async Task<WorkItemDto> GetWorkItemAsync(int workItemId)
    {
        if (workItemId <= 0)
            throw new ArgumentException("Work item id must be a positive integer");

        var result = await _client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);
        return _mapper.Map(result);
    }

    /// <summary>
    /// Gets work items by a WIQL query and maps them to DTOs.
    /// </summary>
    /// <param name="wiql">The WIQL query string.</param>
    /// <param name="top">The maximum number of work items to return.</param>
    /// <returns>A collection of mapped <see cref="WorkItemDto"/> objects.</returns>
    public async Task<IEnumerable<int>> GetWorkItemsByWiql(string wiql, int? top = null)
    {
        wiql = wiql ?? throw new ArgumentNullException(nameof(wiql), "Work item query should not be empty");
        var query = new Wiql { Query = wiql };
        var result = await _client.QueryByWiqlAsync(query, top: top);
        var ids = result.WorkItems.Select(w => w.Id).ToList();
        return ids;
    }

    /// <summary>
    /// Queries work items by a hierarchy WIQL query.
    /// </summary>
    /// <param name="wiql">The WIQL query string.</param>
    /// <returns>The hierarchy query result root.</returns>
    public async IAsyncEnumerable<IEnumerable<WorkItemResult>> QueryWorkItemsByHierarchyWiql(string wiql)
    {
        wiql = wiql ?? throw new ArgumentNullException(nameof(wiql), "Work item query should not be empty");
        var query = new HierarchyQueryRoot
        {
            dataProviderContext = new DataProviderContext(new Properties
            {
                wiql = wiql,
                sourcePage = new SourcePage
                {
                    routeValues = new RouteValues { project = _settings.Project }
                }
            })
        };
        var url = string.Format(HierarchyQueryUrl, _settings.Organization, _settings.Project);
        var result = await _apiClient.ExecutePostAsync<HierarchyQueryRoot, HierarchyQueryResultRoot>(url, query,
            HierarchyQueryRootJsonContext.Default.HierarchyQueryRoot, HierarchyQueryResultRootJsonContext.Default.HierarchyQueryResultRoot);

        var data = result?.dataProviders?.msvssworkwebworkitemquerydataprovider?.data;
        var payload = data?.payload;
        if (data is null || payload is null)
            yield break;

        var colList = payload.columns.ToList();
        var targetIds = data.targetIds.ToList();

        bool firstBatch = true;
        foreach (var ids in targetIds.Chunk(200))
        {
            if (!firstBatch)
            {
                query = new HierarchyQueryRoot
                {
                    contributionIds = new List<string>()
                    {
                        "ms.vss-work-web.page-work-items-data-provider"
                    },
                    dataProviderContext = new DataProviderContext(new Properties
                    {
                        wiql = wiql,
                        workItemIds = string.Join(",", ids),
                        fields = string.Join(",", colList),
                        sourcePage = new SourcePage
                        {
                            routeValues = new RouteValues { project = _settings.Project }
                        }
                    })
                };
                result = await _apiClient.ExecutePostAsync<HierarchyQueryRoot, HierarchyQueryResultRoot>(url, query,
                    HierarchyQueryRootJsonContext.Default.HierarchyQueryRoot, HierarchyQueryResultRootJsonContext.Default.HierarchyQueryResultRoot);

                var paginationData = result?.dataProviders?.msvssworkwebpageworkitemsdataprovider?.data;
                if (paginationData is null)
                    yield break;

                yield return ConvertToWorkItemResults(paginationData.rows, colList);
            }
            else
            {
                yield return ConvertToWorkItemResults(payload.rows, colList);
            }
            firstBatch = false;
        }
    }

    static List<WorkItemResult> ConvertToWorkItemResults(IReadOnlyList<List<object>> rows, List<string> colList)
    {
        List<WorkItemResult> results = new();
        foreach (var row in rows)
        {
            var fields = colList.Select((col, index) => new { col, value = GetValue(row[index]) })
                               .ToDictionary(kv => kv.col, kv => kv.value, StringComparer.OrdinalIgnoreCase);
            var workItem = new WorkItemResult
            {
                Fields = fields,
                WorkItemId = fields.GetValue<int>("System.Id"),
                CreatedDate = DateTimeOffset.Parse(fields.GetValue<string>("System.CreatedDate"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                ClosedDate = DateTimeOffset.Parse(fields.GetValue<string>("Microsoft.VSTS.Common.ClosedDate"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            };
            results.Add(workItem);
        }
        return results;
    }

    static object? GetValue(object? obj)
    {
        if (obj is null)
            return null;
        
        var element = (JsonElement)obj;
        return element.ValueKind switch
        {
            JsonValueKind.String => Convert.ToString(element),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element
        };
    }
    /// <summary>
    /// Gets the raw result for the query.
    /// </summary>
    /// <param name="wiql"></param>
    /// <param name="top"></param>
    /// <returns></returns>
    public async Task<WorkItemQueryResult?> GetRawWorkItemsByWiql(string wiql, int? top = null)
    {
        wiql = wiql ?? throw new ArgumentNullException(nameof(wiql), "Work item query should not be empty");
        var query = new Wiql { Query = wiql };
        return await _client.QueryByWiqlAsync(query, top: top);
    }

    /// <summary>
    /// Gets work items by their IDs and maps them to DTOs.
    /// </summary>
    /// <param name="workItemIds">The collection of work item IDs.</param>
    /// <returns>A collection of mapped <see cref="WorkItemDto"/> objects.</returns>
    public async Task<IEnumerable<WorkItemDto>?> GetWorkItemsAsync(IEnumerable<int> workItemIds, bool expandAll = true, Dictionary<string,string> fieldMapOverride = null)
    {
        if (workItemIds is null || !workItemIds.Any())
            throw new ArgumentNullException(nameof(workItemIds), "At least one work item id must be provided");
        
        var fields = fieldMapOverride?.Keys.ToList();
        var results = await _client.GetWorkItemsAsync(workItemIds, fields:fields, expand: fields == null && expandAll ? WorkItemExpand.All : WorkItemExpand.None);

        return results.Select(result => _mapper.Map(result, fieldMapOverride));
    }

    /// <summary>
    /// Gets work items by their IDs and returns the raw work item objects.
    /// </summary>
    /// <param name="workItemIds"></param>
    /// <param name="expandAll"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception> <summary>
    public async Task<IEnumerable<WorkItem>?> GetWorkItemsRawAsync(IEnumerable<int> workItemIds, bool expandAll = true)
    {
        if (workItemIds is null || !workItemIds.Any())
            throw new ArgumentNullException(nameof(workItemIds), "At least one work item id must be provided");

        var results = await _client.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All);

        return results;
    }

    /// <summary>
    /// Gets the revisions of a work item by its ID.
    /// This method retrieves the historical changes made to a work item, including field updates and state transitions.
    /// It can be useful for auditing purposes or understanding the evolution of a work item over
    /// </summary>
    /// <param name="workItemId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<IReadOnlyList<WorkItemRevisionDto>> GetWorkItemRevisionsAsync(int workItemId)
    {
        if (workItemId <= 0)
            throw new ArgumentException("Work item id must be a positive integer");

        var revisions = await _client.GetRevisionsAsync(workItemId);
        var orderedRevisions = revisions.OrderBy(r => r.Rev).ToList();
        return WorkItemRevisionUtils.GetRevisionComparisons(orderedRevisions);
    }

    /// <summary>
    /// Executes an OData query against Azure DevOps Analytics API.
    /// </summary>
    /// <param name="odataQuery">The OData query string (e.g., "$filter=WorkItemType eq 'User Story'&$select=WorkItemId,Title").</param>
    /// <returns>The JSON response as a string.</returns>
    /// <remarks>
    /// Example: var result = await ExecuteODataQueryAsync("$filter=WorkItemType eq 'User Story' and startswith(Title, 'FF -')&$select=WorkItemId,Title,State&$expand=Parent($select=WorkItemId,Title,WorkItemType)");
    /// </remarks>
    public async Task<T> ExecuteODataQueryAsync<T>(string odataQuery)
    {
        if (string.IsNullOrWhiteSpace(odataQuery))
            throw new ArgumentNullException(nameof(odataQuery), "OData query cannot be empty");

        var result = await _apiClient.ExecuteGetWithRetriesAsync2<T>( url => url + "?" + odataQuery);
        return result;
    }
}
