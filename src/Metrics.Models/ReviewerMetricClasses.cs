using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Metrics.Models;

/// <summary>
/// Represents the metrics for a pull request reviewer.
/// </summary>
public record ReviewerMetrics
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }

    public int? ReviewsRequested { get; set; }
    public int? ReviewsSubmitted { get; set; }
    public float? AverageResponseTimeHours { get; set; }
    public int? CommentCount { get; set; }
    public int? ChangesRequested { get; set; }
    public int? Approved { get; set; }
    public int? ReviewComments { get; set; }

    /// <summary>
    /// Gets or sets the name of the reviewer.
    /// </summary>
    /// </summary>
    /// <value></value>
    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string Reviewer { get; set; }
}

/// <summary>
/// Represents the aggregated metrics for a pull request reviewer.
/// </summary>
public abstract record AggregatedReviewerMetrics : ReviewerMetrics
{
    /// <summary>
    /// Gets or sets the name of the repository.
    /// </summary>
    /// <value></value>
    [Required, StringLength(100)]
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the total number of review days.
    /// </summary>
    /// <value></value>
    public int TotalReviewDays { get; set; }
    public int PrsRequested { get; set; }
    public int PrsReviewed { get; set; }
    public int PrsForApprovalRate { get; set; }
    public int PrsReviewedWithComments { get; set; }
    public int PrsCommented { get; set; }
    public int CommentCountWhenReviewed { get; set; }
    public int ApprovalRatePercentage { get; set; }
    public float AverageCommentCount { get; set; }
}

/// <summary>
/// Represents monthly reviewer metrics.
/// </summary>
[Table("reviewer_monthly_metrics")]
public sealed record ReviewerMonthlyMetrics : AggregatedReviewerMetrics
{
    /// <summary>
    /// Gets or sets the month for the metrics.
    /// </summary>
    /// <value></value>
    [Required]
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the year for the metrics.
    /// </summary>
    /// <value></value>
    [Required]
    public int Year { get; set; }
}

/// <summary>
/// Represents sprint-based reviewer metrics.
/// </summary>
[Table("reviewer_sprint_metrics")]
public sealed record ReviewerSprintMetrics : AggregatedReviewerMetrics
{
    [Required]
    public int SprintNumber { get; set; }

    [Required]
    public int Year { get; set; }
}

/// <summary>
/// Represents the pull request reviewer metrics.
/// </summary>
[Table("pr_reviewer_metrics")]
public sealed record PRReviewerMetrics : ReviewerMetrics
{
    /// <summary>
    /// Navigation property to the associated PRMetrics object.
    /// </summary>
    [JsonIgnore]
    public PRMetrics PRMetric { get; set; }

    /// <summary>
    /// Foreign key referencing the associated PRMetric.
    /// </summary>
    [JsonIgnore]
    [ForeignKey("PRMetric")]
    public int PRMetricId { get; set; }
}
