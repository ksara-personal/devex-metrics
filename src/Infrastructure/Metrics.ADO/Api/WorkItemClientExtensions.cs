using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Metrics.ADO;

/// <summary>
/// Extension methods for WorkItemClient to support feature flag work item operations.
/// </summary>
static class WorkItemClientExtensions
{
    internal static Regex _releaseVersionRegex = new(@"^\d+\.\d+\.\d+", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Field mapping override for feature flag work items.
    /// </summary>
    static readonly Dictionary<string, string> FeatureFlagFieldsMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "System.Title", "Title" },
        { "System.WorkItemType", "WorkItemType" },
        { "System.State", "State" },
        { "Custom.MergedVersions", "MergedVersions" },
        { "Custom.VerifiedVersions", "VerifiedVersions" },
        { "System.AreaPath", "AreaPath" },
        { "System.AreaLevel3", "AreaLevel3" },
        { "System.Parent", "Parent" },
        { "Custom.ReportingVersion", "ReportingVersion" },
        { "Custom.TargetSprint", "TargetSprint" },
    };

    /// <summary>
    /// Gets feature flag work items for a specific release version.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="dbContext"></param>
    /// <param name="releaseVersion"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<WorkItemWithGitHubLink>> GetFeatureFlagWorkItemsForReleaseVersionAsync(
        this WorkItemClient client,
        DevExMetricDbContext dbContext,
        string releaseVersion)
    {
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            throw new ArgumentException("Release version must not be null or empty", nameof(releaseVersion));
        }

        var featureOrEpicResult = await client.GetRawWorkItemsByWiql(string.Format(WorkItemQueries.FeatureOrEpicMeetingVersionQuery, releaseVersion));

        List<string> workItemIds = new(), parentIds = new();
        foreach (var relation in featureOrEpicResult.WorkItemRelations)
        {
            if (relation.Source != null)
            {
                workItemIds.Add(relation.Target.Id.ToString());
            }
            else
            {
                parentIds.Add(relation.Target.Id.ToString());
            }
        }
        
        var allIds = workItemIds.Concat(parentIds).Distinct().ToList();

        var prMetrics = await dbContext.PRMetrics
                        .AsNoTracking()
                        .Where(pr => allIds.Contains(pr.WorkItemId) || allIds.Contains(pr.WorkItemId2))
                        .ToListAsync();

        List<WorkItemWithGitHubLink> workItemsWithLinks = new();
        Dictionary<int, WorkItemDto> workItemsCache = new();

        foreach (var ids in allIds.Chunk(200))
        {
            var childItems = await client.GetWorkItemsAsync(ids.Select(i => int.Parse(i)), fieldMapOverride: FeatureFlagFieldsMap);
            foreach (var item in childItems)
            {
                workItemsCache.TryAdd(item.Id.Value, item);
            }
            //await client.FetchAndAddFeatureFlagWorkItemsAsync(ids.Select(i => int.Parse(i)), workItemsWithLinks, prMetrics);
        }
        var filtered = from item in workItemsCache.Values
                       let title = item.Fields["Title"].ToString()
                       where title.StartsWith("FF -", StringComparison.OrdinalIgnoreCase)
                       select item;

        foreach (var item in filtered)
        {
            WorkItemWithGitHubLink workItemWithGitHubLink = new()
            {
                WorkItemId = item.Id.Value,
                Title = Convert.ToString(item.Fields["Title"]),
                WorkItemType = Convert.ToString(item.Fields["WorkItemType"]),
                State = Convert.ToString(item.Fields["State"]),
                MergedVersions = Convert.ToString(item.Fields["MergedVersions"]),
                VerifiedVersions = Convert.ToString(item.Fields["VerifiedVersions"]),
                AreaPath = Convert.ToString(item.Fields["AreaPath"]),
                AreaLevel3 = Convert.ToString(item.Fields["AreaLevel3"]),
            };

            var parentIdValue = item.ParentId;
            int? grandParentId = null;
            if(parentIdValue.HasValue && workItemsCache.TryGetValue(parentIdValue.Value, out var parentItem) && parentItem is not null)
            {
                var workItemType = parentItem.Fields.GetValue<string>("WorkItemType");
                if ("Feature".Equals(workItemType))
                {
                    var parent = parentItem.Fields.GetValue<int>("Parent");
                    if (workItemsCache.TryGetValue(parent, out parentItem) && parentItem is not null)
                    {
                        parentIdValue = grandParentId = parent;
                    }
                }
                workItemWithGitHubLink.EpicWorkItemId = parentIdValue;
                var reportingVersion = parentItem.Fields.GetValue<string>("ReportingVersion");
                workItemWithGitHubLink.EpicReportingVersion = reportingVersion;
                workItemWithGitHubLink.EpicTargetSprint = parentItem.Fields.GetValue<string>("TargetSprint");
                var match = _releaseVersionRegex.Match(reportingVersion);
                if (match.Success)
                {
                    workItemWithGitHubLink.ReleaseVersion = match.Value;
                }
            }

            var idString = workItemWithGitHubLink.WorkItemId.ToString();
            var parentIdString = parentIdValue?.ToString();
            var grandParentIdString = grandParentId?.ToString();

            var prMetric = prMetrics.FirstOrDefault(pr => pr.WorkItemId == idString || pr.WorkItemId2 == idString
                    || (!string.IsNullOrEmpty(parentIdString) && (pr.WorkItemId == parentIdString || pr.WorkItemId2 == parentIdString))
                    || (!string.IsNullOrEmpty(grandParentIdString) && (pr.WorkItemId == grandParentIdString || pr.WorkItemId2 == grandParentIdString)));

            workItemWithGitHubLink.GitHubPullRequestUrl = prMetric?.PrUrl;
            workItemsWithLinks.Add(workItemWithGitHubLink);
        }

        workItemIds.Clear();
        prMetrics.Clear();
        allIds.Clear();
        parentIds.Clear();
        workItemsCache.Clear();

        return workItemsWithLinks;
    }
}
