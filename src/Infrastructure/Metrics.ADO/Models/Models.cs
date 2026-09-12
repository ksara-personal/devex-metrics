using System;
using System.Text.Json.Serialization;

namespace Metrics.ADO;

/// <summary>
/// ADO Settings for configuration
/// </summary>
/// <value></value>
public sealed record ADOSettings
{
    /// <summary>
    /// The URL of the organization.
    /// </summary>
    /// <value></value>
    public required string Organization { get; set; }

    /// <summary>
    /// The name of the project.
    /// </summary>
    /// <value></value>
    public required string Project { get; set; }

    /// <summary>
    /// The personal access token for authentication.
    /// </summary>
    /// <value></value>
    public required string PersonalAccessToken { get; set; }
    /// <summary>
    /// The list of fields to include.
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    public IReadOnlyList<string> Fields { get; set; } = new List<string>();
}

/// <summary>
/// Represents information about a work item, including its ID, fields, and related GitHub pull request info.
/// </summary>
[JsonConverter(typeof(WorkItemDtoConverter))]
public sealed record WorkItemDto
{
    /// <summary>
    /// The unique identifier of the work item.
    /// </summary>
    public int? Id { get; set; }
    /// <summary>
    /// Parent work item ID
    /// </summary>
    /// <value></value>
    public int? ParentId { get; set; }
    /// <summary>
    /// The dictionary of work item fields and their values.
    /// </summary>
    public Dictionary<string, object?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public record ODataRoot(IReadOnlyList<ODataWorkItemValue> value);

public record ODataWorkItemValue : WorkItemValue
{
    public string? Custom_MergedVersions { get; set; }
    public string? Custom_VerifiedVersions { get; set; }
    public int? ParentWorkItemId { get; set; }
}

public record WorkItemValue
{
    public int WorkItemId { get; set; }
    public string Title { get; set; }
    public string WorkItemType { get; set; }
    public string? State { get; set; }
    public string? AreaPath { get; set; }
    public string? AreaLevel3 { get; set; }
}

public record WorkItemWithGitHubLink : WorkItemValue
{
    public int? EpicWorkItemId { get; set; }
    public string? EpicReportingVersion { get; set; }
    public string? EpicTargetSprint { get; set; }
    public string? ReleaseVersion { get; set; }
    public string GitHubPullRequestUrl { get; set; }
    public string? MergedVersions { get; set; }
    public string? VerifiedVersions { get; set; }
}

/// <summary>
/// Represents a revision of a work item, including revision numbers, change metadata, and field values.
/// </summary>
public sealed class WorkItemRevisionDto
{
    /// <summary>
    /// The revision number of the work item (from).
    /// </summary>
    public int From { get; set; }
    /// <summary>
    /// The revision number of the work item (to).
    /// </summary>
    public int To { get; set; }
    /// <summary>
    /// The date and time when the revision was created.
    /// </summary>
    public string? ChangedDate { get; set; }
    /// <summary>
    /// The user who created the revision.
    /// </summary>
    public string? ChangedBy { get; set; }
    /// <summary>
    /// The fields and their values for this revision.
    /// </summary>
    public Dictionary<string, RevisionValue> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents the value of a field in a work item revision, including the new and old values.
/// </summary>
public sealed class RevisionValue
{
    /// <summary>
    /// The value of the field in the revision.
    /// </summary>
    public object? NewValue { get; set; }
    /// <summary>
    /// The original value as a string.
    /// </summary>
    public string? OldValue { get; set; }
}

public record DataProviderContext(Properties properties);

public record Properties
{
    public bool useIsoDateFormat { get; set; } = true; 
    public string wiql { get; set; }
    public bool isDirty { get; set; } = false;
    public string workItemIds { get; set; }
    public string fields { get; set; }
    public SourcePage sourcePage { get; set; }
}

public record HierarchyQueryRoot
{
    public IList<string> contributionIds { get; set; } = new List<string>()
    {
        "ms.vss-work-web.work-item-query-data-provider"
    };
    public DataProviderContext dataProviderContext { get; set; }
}

public record RouteValues
{
    public string project { get; set; } = "your-project";
    public string view { get; set; } = "query";
    public string id { get; set; } = "";
    public string controller { get; set; } = "ContributedPage";
    public string action { get; set; } = "Execute";
    public string serviceHost { get; set; } = "";
}

public record SourcePage
{
    public string url { get; set; } = "";
    public string routeId { get; set; } = "";
    public RouteValues routeValues { get; set; }
}

public record Clause(
    string logicalOperator,
    string fieldName,
    string @operator,
    object value,
    int index);

public record Column(
    string name,
    string text,
    int fieldId,
    bool canSortBy,
    int width,
    bool isIdentity,
    int fieldType);

public record Data(
    bool queryRan,
    string wiql,
    IReadOnlyList<Column> columns,
    IReadOnlyList<object> sortColumns,
    IReadOnlyList<int> targetIds,
    IReadOnlyList<string> pageColumns,
    Payload payload,
    EditInfo editInfo);
public record PaginationData(
 IReadOnlyList<string> columns,
 IReadOnlyList<List<object>> rows
    );
    
public record DataProviders(
    [property: JsonPropertyName("ms.vss-web.component-data")] MsVssWebComponentData msvsswebcomponentdata,
    [property: JsonPropertyName("ms.vss-web.shared-data")] object msvsswebshareddata,
    [property: JsonPropertyName("ms.vss-work-web.work-item-query-data-provider")] MsVssWorkWebWorkItemQueryDataProvider msvssworkwebworkitemquerydataprovider,
    [property: JsonPropertyName("ms.vss-work-web.page-work-items-data-provider")] MsVssWorkWebPageWorkItemsDataProvider msvssworkwebpageworkitemsdataprovider
);
public record EditInfo(SourceFilter sourceFilter,int mode,string teamProject);
public record MsVssWebComponentData();
public record MsVssWorkWebWorkItemQueryDataProvider(object errorMessage,Data data,object versionStamp);
public record MsVssWorkWebPageWorkItemsDataProvider(object errorMessage,PaginationData data,object versionStamp);

public record Payload(IReadOnlyList<string> columns, IReadOnlyList<List<object>> rows);
public record HierarchyQueryResultRoot(DataProviders dataProviders);
public record SourceFilter(IReadOnlyList<Clause> clauses, IReadOnlyList<object> groups, int maxGroupLevel);

public record WorkItemResult
{
    public int WorkItemId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ClosedDate { get; set; }
    public Dictionary<string, object?> Fields { get; set; }
}

public record WorkItemNode
{
    public int WorkItemId { get; set; }

    public List<WorkItemNode> Nodes { get; set; } = new();
}


