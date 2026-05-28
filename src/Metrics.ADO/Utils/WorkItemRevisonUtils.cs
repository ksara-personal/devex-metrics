using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Metrics.Extensions;

namespace Metrics.ADO.Utils;

/// <summary>
/// Utility class for comparing work item revisions.
/// </summary>
static class WorkItemRevisionUtils
{
    /// <summary>
    /// Compares a list of work item revisions and returns a list of revision differences.
    /// </summary>
    /// <param name="revisions">The list of work item revisions to compare.</param>
    /// <returns>A list of work item revision DTOs representing the differences between revisions.</returns>
    public static IReadOnlyList<WorkItemRevisionDto> GetRevisionComparisons(List<WorkItem> revisions)
    {
        List<WorkItemRevisionDto> revisionDtos = new();
        for (int i = 1; i < revisions.Count; i++)
        {

            var oldRev = revisions[i - 1];
            var newRev = revisions[i];

            var changedBy = newRev.Fields.GetValue<IdentityRef>("System.ChangedBy")?.DisplayName ?? "(unknown)";
            var changedDate = newRev.Fields.GetValue<string>("System.ChangedDate", "(unknown)");

            var revisionDto = new WorkItemRevisionDto
            {
                From = oldRev.Rev.Value,
                To = newRev.Rev.Value,
                ChangedBy = changedBy,
                ChangedDate = changedDate,
            };
            revisionDtos.Add(revisionDto);

            // Get all field keys from either revision
            var allKeys = new HashSet<string>(oldRev.Fields.Keys);
            allKeys.UnionWith(newRev.Fields.Keys);

            bool anyChanges = false;

            foreach (var field in allKeys.OrderBy(f => f))
            {
                oldRev.Fields.TryGetValue(field, out var oldValue);
                newRev.Fields.TryGetValue(field, out var newValue);

                if (!object.Equals(oldValue, newValue))
                {
                    anyChanges = true;

                    // Optional: Skip system fields you don't care about
                    if (field.StartsWith("System.Changed") || field == "System.Rev")
                        continue;
                        
                    string oldVal = FormatValue(oldValue), newVal = FormatValue(newValue);
                    if (!string.Equals(oldVal, newVal, StringComparison.OrdinalIgnoreCase))
                    {
                        revisionDto.Fields[field] = new RevisionValue
                        {
                            NewValue = FormatValue(newValue),
                            OldValue = FormatValue(oldValue)
                        };
                    }
                }
            }
            if (!anyChanges)
            {
                revisionDtos.Remove(revisionDto);
            }
        }
        return revisionDtos;
    }

    /// <summary>
    /// Formats the value for display, handling nulls and trimming whitespace.
    /// If the value is null, returns "null" as a string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    static string FormatValue(object value)
    {
        return value switch
        {
            null => "null",
            IdentityRef identityRef => identityRef.DisplayName,
            _ => value.ToString().Trim()
        };
    }
}