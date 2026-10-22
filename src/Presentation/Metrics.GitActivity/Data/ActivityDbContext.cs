using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Metrics.GitActivity.Data;

/// <summary>
/// Shared SQLite store for every collected repository.
/// </summary>
public sealed class ActivityDbContext(DbContextOptions<ActivityDbContext> options) : DbContext(options)
{
    public DbSet<PullRequestRecord> PullRequests => Set<PullRequestRecord>();
    public DbSet<PullRequestReviewRecord> PullRequestReviews => Set<PullRequestReviewRecord>();
    public DbSet<PullRequestCommentRecord> PullRequestComments => Set<PullRequestCommentRecord>();
    public DbSet<PullRequestReviewRequestRecord> PullRequestReviewRequests => Set<PullRequestReviewRequestRecord>();
    public DbSet<PullRequestLabelRecord> PullRequestLabels => Set<PullRequestLabelRecord>();
    public DbSet<PullRequestLinkedIssueRecord> PullRequestLinkedIssues => Set<PullRequestLinkedIssueRecord>();
    public DbSet<CommitRecord> Commits => Set<CommitRecord>();
    public DbSet<CollectionRunRecord> CollectionRuns => Set<CollectionRunRecord>();
    public DbSet<CollectionCheckpoint> CollectionCheckpoints => Set<CollectionCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PullRequestRecord>(pr =>
        {
            pr.HasIndex(p => new { p.Repository, p.Number }).IsUnique();
            pr.HasIndex(p => new { p.Repository, p.CreatedAt });
            pr.HasMany(p => p.Reviews).WithOne().HasForeignKey(r => r.PullRequestId).OnDelete(DeleteBehavior.Cascade);
            pr.HasMany(p => p.Comments).WithOne().HasForeignKey(r => r.PullRequestId).OnDelete(DeleteBehavior.Cascade);
            pr.HasMany(p => p.ReviewRequests).WithOne().HasForeignKey(r => r.PullRequestId).OnDelete(DeleteBehavior.Cascade);
            pr.HasMany(p => p.Labels).WithOne().HasForeignKey(r => r.PullRequestId).OnDelete(DeleteBehavior.Cascade);
            pr.HasMany(p => p.LinkedIssues).WithOne().HasForeignKey(r => r.PullRequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommitRecord>(c =>
        {
            c.HasIndex(p => new { p.Repository, p.Hash }).IsUnique();
            c.HasIndex(p => new { p.Repository, p.AuthorDate });
        });

        modelBuilder.Entity<CollectionRunRecord>().HasIndex(r => r.Repository);

        modelBuilder.Entity<CollectionCheckpoint>(c =>
        {
            c.Property(p => p.Phase).HasConversion<string>();
            c.HasIndex(p => new { p.Repository, p.Phase, p.WindowStart, p.WindowEnd }).IsUnique();
        });
    }
}

/// <summary>
/// Used by <c>dotnet ef migrations</c>.
/// </summary>
public class ActivityDbContextFactory : IDesignTimeDbContextFactory<ActivityDbContext>
{
    public ActivityDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ActivityDbContext>().UseSqlite("Data Source=gitactivity.db").Options);
}
