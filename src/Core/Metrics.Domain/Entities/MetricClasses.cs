using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace Metrics.Domain;

/// <summary>
/// Represents pull request (PR) metrics, including metadata, contributors, and associated metrics for a PR.
/// </summary>
[Table("metrics")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class PRMetrics
{
    /// <summary>
    /// Unique identifier for the PR metrics (primary key).
    /// </summary>
    [JsonIgnore]
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Pull request number.
    /// </summary>
    [Required]
    public int PrNumber { get; set; }

    /// <summary>
    /// Formatter for pull request URLs.
    /// </summary>
    /// <value></value>
    [NotMapped, JsonIgnore]    
    public static IPRUrlFormatter UrlFormatter { get; set; }

    /// <summary>
    /// URL of the pull request.
    /// </summary>
    string _prUrl;
    [NotMapped]
    public string? PrUrl
    {
        get => _prUrl ?? (_prUrl = UrlFormatter?.Format(this.Repository, this.PrNumber));
        set => _prUrl = value;
    }

    /// <summary>
    /// Author of the pull request.
    /// </summary>
    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? Author { get; set; }

    /// <summary>
    /// User who merged the pull request.
    /// </summary>
    [StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? MergedBy { get; set; }

    /// <summary>
    /// Name of the repository.
    /// </summary>
    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? Repository { get; set; }

    /// <summary>
    /// Base branch of the pull request.
    /// </summary>
    [Required, StringLength(150), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? BaseBranch { get; set; }

    [JsonIgnore]
    public Team Team { get; set; }

    [JsonIgnore, ForeignKey("Team")]
    public int TeamId { get; set; }

    string _teamName;
    [NotMapped]
    public string TeamName
    {
        get => _teamName ?? (_teamName = this.Team?.Name);
        set => _teamName = value;
    }

    string _region;
    [NotMapped]
    public string TeamRegion
    {
        get => _region ?? (_region = this.Team?.Region);
        set => _region = value;
    }

    string _vs;
    [NotMapped]
    public string ValueStream
    {
        get => _vs ?? (_vs = this.Team?.ValueStream);
        set => _vs = value;
    }

    /// <summary>
    /// State of the PR.
    /// </summary>
    [StringLength(20), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string State { get; set; }

    /// <summary>
    /// Date and time when the pull request was merged.
    /// </summary>
    //[Required]
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Date and time when the pull request was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Total number of comments on the pull request.
    /// </summary>
    public int TotalComments { get; set; }

    /// <summary>
    /// Total number of lines changed in the pull request.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Total number of files changed in the pull request.
    /// </summary>
    /// <value></value>
    public int? ChangedFiles { get; set; }

    /// <summary>
    /// Unique identifier for the work item associated with the pull request.
    /// </summary>
    /// <value></value>
    [StringLength(20), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? WorkItemId { get; set; }

    /// <summary>
    /// Unique identifier for the second work item associated with the pull request.
    /// </summary>
    /// <value></value>
    [StringLength(20), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? WorkItemId2 { get; set; }

    /// <summary>
    /// Indicates if the pull request is a feature PR.
    /// </summary>
    public bool? IsFeature { get; set; }
    
    /// <summary>
    /// Number of times the pull request was marked as draft.
    /// </summary>
    public int DraftTransitions { get; set; }

    /// <summary>
    /// Total number of commits in the pull request.
    /// </summary>
    public int TotalCommits { get; set; }

    /// <summary>
    /// Collection of contributors to the pull request.
    /// </summary>
    public ICollection<PRContributor> Contributors { get; set; } = new List<PRContributor>();

    /// <summary>
    /// Collection of reviewer metrics to the pull request.
    /// </summary>
    public ICollection<ReviewerDailyMetrics> ReviewerDailyMetrics { get; set; } = new List<ReviewerDailyMetrics>();
    /// <summary>
    /// Collection of reviewer metrics to the pull request.
    /// </summary>
    /// <typeparam name="PRReviewerMetrics"></typeparam>
    /// <returns></returns>
    public ICollection<PRReviewerMetrics> ReviewerMetrics { get; set; } = new List<PRReviewerMetrics>();

    /// <summary>
    /// Copilot reviewer statistics for the pull request.
    /// </summary>
    public CopilotReviewMetrics CopilotReviewMetrics { get; set; }

    /// <summary>
    /// DevEx metrics associated with the pull request.
    /// </summary>
    /// <value></value>
    [JsonPropertyName("DevExMetrics")]
    public DevExMetricItem DevExMetricItem { get; set; }

    /// <summary>
    /// Returns a string representation of the PR metrics.
    /// </summary>
    public override string ToString() => $"{Repository}-{PrNumber}";

    /// <summary>
    /// Determines whether the specified object is equal to the current PRMetrics instance.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is PRMetrics metrics && this.PrNumber == metrics.PrNumber
        && string.Equals(this.Repository, metrics.Repository, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a hash code for the PR metrics.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(PrNumber, Repository);
}

/// <summary>
/// Represents daily metrics for a pull request reviewer.
/// </summary>
[Table("reviewer_daily_metrics")]
public sealed class ReviewerDailyMetrics : PRMetricRelationship
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

    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string Reviewer { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [NotMapped, JsonIgnore]
    internal List<TimeSpan> ResponseTimes { get; set; } = new(1);

    public ReviewerDailyMetrics() { }

    public ReviewerDailyMetrics(string reviewer) => Reviewer = reviewer;
}

/// <summary>
/// Represents a team involved in pull request reviews.
/// </summary>
[Table("team")]
public sealed class Team
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }

    [Required, StringLength(50), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string Name { get; set; }

    [NotMapped, JsonIgnore]
    public string RemoteName { get; set; }

    [StringLength(50), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string Region { get; set; }

    [StringLength(50), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string ValueStream { get; set; }

    [JsonIgnore]
    public ICollection<PRMetrics> Metrics { get; set; } = new List<PRMetrics>();

    public override string ToString() => string.Concat(Name, "|", ValueStream, "|", Region);
}

/// <summary>
/// Represents statistics related to Copilot reviewer activity for a pull request.
/// </summary>
[Table("copilotreviewmetrics")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class CopilotReviewMetrics : PRMetricRelationship
{
    /// <summary>
    /// Unique identifier for the Copilot reviewer stats (primary key).
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Number of files reviewed by Copilot.
    /// </summary>
    public int FilesReviewed { get; set; }

    /// <summary>
    /// Number of files changed by Copilot.
    /// </summary>
    public int FilesChanged { get; set; }

    /// <summary>
    /// Number of comments made by Copilot.
    /// </summary>
    public int Comments { get; set; }
}

/// <summary>
/// Represents a contributor's participation in a pull request, including lines of code and commits.
/// </summary>
[Table("contributor")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class PRContributor : PRMetricRelationship
{
    /// <summary>
    /// Unique identifier for the contribution (primary key).
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Name of the contributor.
    /// </summary>
    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string Contributor { get; set; }

    /// <summary>
    /// Lines of code contributed by the contributor.
    /// </summary>
    [Required]
    public int LOC { get; set; }

    /// <summary>
    /// Number of commits made by the contributor.
    /// </summary>
    [Required]
    public int Commits { get; set; }
}

/// <summary>
/// Represents a DevEx metric item associated with a pull request.
/// </summary>
[Table("devexmetricitem")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class DevExMetricItem : PRMetricRelationship
{
    /// <summary>
    /// Unique identifier for the metric item (primary key).
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }
    public TimeSpan? ApproveTime { get; set; }
    public int? TotalReviewChangesRequested { get; set; }
    public int? CodeExcellenceRequestedChanges { get; set; }
    public int? TeamRequestedChanges { get; set; }
    public int? OthersRequestedChanges { get; set; }
    public TimeSpan? CodingTime { get; set; }
    public int? TotalSmallCommits { get; set; }
    public int? TotalSmallCommitComments { get; set; }
    public int? TotalMediumCommits { get; set; }
    public int? TotalMediumCommitComments { get; set; }
    public int? TotalLargeCommits { get; set; }
    public int? TotalLargeCommitComments { get; set; }
    public TimeSpan? CycleTime { get; set; }
    public TimeSpan? LeadTime { get; set; }
    public float? MaturityPercentage { get; set; }
    public int? InitialLinesChanged { get; set; }
    public int? SubsequentLinesChanged { get; set; }
    public TimeSpan? MergeTime { get; set; }
    public TimeSpan? PickupTime { get; set; }
    public int? TotalReviewCommentsAfterFinalApproval { get; set; }
    public TimeSpan? ReviewTime { get; set; }
    public string? PRSize { get; set; }
    public float? AvgReviewCommentsPerCommit { get; set; }
    public int? TotalReviewComments { get; set; }
}

/// <summary>
/// Base class for all pull request metric relationships.
/// </summary>
public abstract class PRMetricRelationship
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

/// <summary>
/// Represents the status of a scheduled or background run, including the last run time and any error message.
/// </summary>
[Table("runstatus")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class RunStatus
{
    /// <summary>
    /// Unique identifier for the run status (primary key).
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The date and time when the last run occurred.
    /// </summary>
    public DateTime? LastRunAt { get; set; } = null;

    /// <summary>
    /// The error message from the last run, if any.
    /// </summary>
    public string? LastError { get; set; } = null;
    /// <summary>
    /// PR Number
    /// </summary>
    /// <value></value>
    public int? PRNumber { get; set; }
    /// <summary>
    /// Repository name
    /// </summary>
    /// <value></value>
    [StringLength(100)]
    public string? Repository { get; set; }
}

/// <summary>
/// Represents the history of data migrations.
/// </summary>
[Table("datamigrationhistory"), Finbuckle.MultiTenant.Abstractions.MultiTenant]
public class DataMigrationHistory
{
    public const int MigrationColumnLength = 150;
    /// <summary>
    /// Unique identifier for the data migration history record (primary key).
    /// </summary>
    /// <value></value>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// shadow property to hold tenant id for multi-tenancy support. Not mapped to database.
    /// </summary> <summary>
    [StringLength(64)]
    public string TenantId { get; set; }

    /// <summary>
    /// Unique identifier for the migration.
    /// </summary>
    /// <value></value>
    [Required, StringLength(MigrationColumnLength), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string MigrationId { get; set; } = null!;
    /// <summary>
    /// Unique identifier for the migration.
    /// </summary>
    /// <value></value>
    [Required, StringLength(MigrationColumnLength), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string MigrationType { get; set; } = null!;
    /// <summary>
    /// Date and time when the migration was applied.
    /// </summary>
    /// <value></value>
    [Required]
    public DateTime AppliedOn { get; set; }
}

/// <summary>
/// Represents a sprint in the project.
/// </summary>
[NotMapped]
public sealed class Sprint
{
    /// <summary>
    /// Unique identifier for the data migration history record (primary key).
    /// </summary>
    /// <value></value>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public int Id { get; set; }
    /// <summary>
    /// The release number associated with the sprint.
    /// </summary>
    /// <value></value>
    [Required, StringLength(100), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public string? ReleaseNumber { get; set; }
    /// <summary>
    /// The sprint number associated with the sprint.
    /// </summary>
    /// <value></value>
    [Required]
    public int SprintNumber { get; set; }
    [Required]
    public int Year { get; set; }
    /// <summary>
    /// The start date of the sprint.
    /// </summary>
    /// <value></value>
    [Required]
    public DateTime StartDate { get; set; }
    /// <summary>
    /// The end date of the sprint.
    /// </summary>
    /// <value></value>
    [Required]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// DTO for reviewer metrics summary results.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public record ReviewerMetricsSummary(
    string Login, 
    string Repository, 
    int TotalPRs, 
    int ChangesRequested, 
    int Approvals, 
    int ReviewComments,
    int TotalReviewsRequested,
    int TotalReviewsSubmitted,
    int TotalCommentCount,
    int PrsRequested,
    int PrsReviewed,
    int PrsForApprovalRate,
    int PrsReviewedWithComments,
    int PrsCommented,
    int CommentCountWhenReviewed,
    int ApprovalRatePercentage);

/// <summary>
/// DTO for Copilot review metrics summary results.
/// </summary>
/// <param name="Team"></param>
/// <param name="TotalPrs"></param>
/// <param name="TotalComments"></param>
/// <param name="TotalFilesChanged"></param>
/// <param name="TotalFilesReviwed"></param>
/// <returns></returns>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public record CopilotReviewSummary(string Team, int TotalPrs, int TotalComments, int TotalFilesChanged, int TotalFilesReviewed);
