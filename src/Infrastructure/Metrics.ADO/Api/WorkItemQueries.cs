using System;

namespace Metrics.ADO;

/// <summary>
/// Work Item Queries
/// </summary> 
struct WorkItemQueries
{
    public const string EpicsClosedAfterDateWiql = @"SELECT
    [System.Id],
    [System.Title],
    [System.AreaLevel3],
    [System.CreatedDate],
    [Microsoft.VSTS.Common.ClosedDate],
    [Custom.ShirtSize],
    [Custom.ReportingVersion]
FROM workitems WHERE
    [Microsoft.VSTS.Common.ClosedDate] > '{0}'
    AND [System.WorkItemType] = 'Epic'
    AND [System.State] = 'Closed' ORDER BY [Microsoft.VSTS.Common.ClosedDate]";

    public const string EpicHierarchyWiql = @"SELECT [System.Id]
FROM workitemLinks WHERE ([Source].[System.Id] = {0})
    AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward')
    AND (
        [Target].[System.WorkItemType] IN ('Feature', 'User Story', 'Bug', 'Task')
        AND [Target].[System.State] NOT IN ('Cancelled')
    )
MODE (Recursive)";

    public const string FeatureOrEpicMeetingVersionQuery = @"
SELECT [System.Id],[System.Title],[System.WorkItemType],[System.State],[Custom.MergedVersions],[Custom.VerifiedVersions]
FROM workitemLinks
WHERE 
(
    [Source].[System.WorkItemType] IN ('Epic')
    AND [Source].[Target Sprint Committed] = True
    AND [Source].[Reporting Version] CONTAINS '{0}'
)
AND [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward'
MODE (Recursive)";
}
