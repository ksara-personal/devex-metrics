using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Metrics.Extensions;

namespace Metrics.ADO;

/// <summary>
/// Implements <see cref="IWorkItemMapper"/> to map Azure DevOps <see cref="WorkItem"/> objects
/// to <see cref="WorkItemDto"/> using field extraction and type conversion helpers.
/// </summary>
public sealed class WorkItemMapper
{
    readonly ADOSettings _settings;
    readonly Dictionary<string, string> _fieldsMap;
    static readonly string[] Fields = new[]
    {
        "System.Title",
        "System.State",
        "System.AssignedTo",
        "System.CreatedDate",
        "System.CreatedBy",
        "System.CommentCount",
        "System.WorkItemType",
        "Microsoft.VSTS.Common.Priority",
        "Microsoft.VSTS.Common.ClosedDate",
        "Microsoft.VSTS.Common.ClosedBy",
        "Microsoft.VSTS.Common.Severity",
        "Custom.ProductType",
        "Custom.AffectsVersions",
        "Custom.NoFixReason",
        "Custom.Regression",
        "Custom.NumberofClients",
        "Custom.IssueSource",
        "Custom.Environment",
        "Custom.MergedVersions",
        "Custom.VerifiedVersions",
        "System.Description",
        "System.Tags",
        "System.AreaPath",
        "System.IterationPath",
        "System.AreaLevel3",
        "System.Parent"
    };

    const string ReverseRelationType = "System.LinkTypes.Hierarchy-Reverse";

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItemMapper"/> class.
    /// </summary>
    /// <param name="tenantConfig">Tenant configuration service for resolving tenant-specific ADO settings.</param>
    public WorkItemMapper(TenantConfigurationService tenantConfig)
    {
        _settings = tenantConfig.GetSettings<ADOSettings>(ADOServiceConfigurator.ADOSectionName)
            ?? throw new InvalidOperationException("ADO settings not configured for tenant");
        var fields = _settings.Fields;
        if (fields is null || fields.Count == 0)
            fields = Fields;
        _fieldsMap = fields.ToDictionary(k => k, v => v.Split('.').Last());
    }

    /// <summary>
    /// Maps WorkItem to WorkItemDto
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public WorkItemDto? Map(WorkItem item, Dictionary<string,string> fieldOverrides = null)
    {
        if (item == null) return null;

        var dto = new WorkItemDto()
        {
            Id = item.Id,
        };
        var dtoFields = dto.Fields;
        var fields = item.Fields;
        if (fields.GetValue<string>("System.WorkItemType") != "Epic")
        {
            if (fields.TryGetValue("System.Parent", out var parentValue) && parentValue != null)
            {
                dto.ParentId = Convert.ToInt32(parentValue);
            }
            else // fallback to relations if Parent field is not set
            {
                var parent = item.Relations?.FirstOrDefault(r => r.Rel == ReverseRelationType && r.Attributes.TryGetValue("name", out var name) && "Parent".Equals(name));
                if (parent is not null && parent.Url is not null)
                {
                    var parentIdString = parent.Url.Split('/').Last();
                    if (int.TryParse(parentIdString, out var parentId))
                    {
                        dto.ParentId = parentId;
                    }
                }
            }
        }
        var fieldMap = fieldOverrides ?? _fieldsMap;
        foreach (var kv in fieldMap)
        {
            if (fields.TryGetValue(kv.Key, out var fieldValue))
            {
                dtoFields[kv.Value] = fieldValue switch
                {
                    IdentityRef identityRef => identityRef.DisplayName,
                    long longValue => Convert.ToInt32(longValue),
                    _ => fieldValue
                };
            }
            else
            {
                dtoFields[kv.Value] = null;
            }
        }
        return dto;
    }
}
