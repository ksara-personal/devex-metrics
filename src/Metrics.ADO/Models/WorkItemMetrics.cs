using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metrics.ADO.Models;

/// <summary>
/// Represents metrics related to a work item in Azure DevOps.
/// </summary>
/// <value></value>
[Table("workitem_metrics")]
public sealed record WorkItemMetrics
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int WorkItemId { get; set; }
    public int? TotalPrs { get; set; }
    public string? Team { get; set; }
    public string? ReleaseVersion { get; set; }
    public string? ShirtSize { get; set; }
    public int LOC { get; set; }
    public int Contributors { get; set; }
    public int Commits { get; set; }
    public int ReviewComments { get; set; }
    public int ChangesRequested { get; set; }
    public float MaturityPercentage { get; set; }
    public TimeSpan PRCycleTime { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ClosedDate { get; set; }
}

/// <summary>
/// Represents the synchronization status of a work item in Azure DevOps.
/// </summary>
/// <value></value>
[Table("workitem_sync_status")]
public record WorkItemSyncStatus
{
    /// <summary>
    /// Gets or sets the unique identifier for the work item.
    /// </summary>
    /// <value></value>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int? WorkItemId { get; set; }
    public DateTime? LastSyncDate { get; set; }
}
