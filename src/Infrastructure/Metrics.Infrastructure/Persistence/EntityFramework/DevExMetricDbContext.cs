using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

namespace Metrics.Infrastructure;

/// <summary>
/// Base class for DevExMetricDbContext.
/// This context is used for storing and retrieving DevEx metrics in a database.
/// Provides DbSets for all metric-related entities and configures model relationships and properties.
/// Supports multiple database providers (SQLite, PostgreSQL, InMemory) configured through application settings.
/// </summary>
public class DevExMetricDbContext : MetricDbContext
{
    
    /// <summary>
    /// Constructor for <see cref="DevExMetricDbContext"/>.
    /// Initializes the DbContext with the provided options and configuration.
    /// Optionally accepts a multi-tenant context accessor for tenant isolation.
    /// </summary>
    /// <param name="options">The options for the DbContext.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="multiTenantContextAccessor">Optional multi-tenant context accessor for resolving the current tenant.</param>
    public DevExMetricDbContext(DbContextOptions<DevExMetricDbContext> options, IConfiguration configuration, IMultiTenantContextAccessor? multiTenantContextAccessor = null) 
        : base(options, configuration, multiTenantContextAccessor)
    {
    }
    
    /// <summary>
    /// DbSet for PRMetrics entity. Represents the PR metrics stored in the database.
    /// </summary>
    public DbSet<PRMetrics> PRMetrics { get; set; }
    /// <summary>
    /// DbSet for DevExMetricItem entity. Represents the individual items within DevEx metrics stored in the database.
    /// </summary>
    /// <value></value>
    public DbSet<DevExMetricItem> DevExMetricItems { get; set; }
    /// <summary>
    /// DbSet for RunStatus entity. Represents the status of metric runs, including the last run time.
    /// </summary>
    public DbSet<RunStatus> Runs { get; set; }
    /// <summary>
    /// DbSet for PRContributor entity. Represents the contributions made by developers to pull requests.
    /// </summary>
    public DbSet<PRContributor> Contributors { get; set; }
    /// <summary>
    /// DbSet for CopilotReview entity. Represents the metrics related to Copilot review for pull requests.
    /// </summary>
    public DbSet<CopilotReviewMetrics> CopilotReviewMetrics { get; set; }
    /// <summary>
    /// DbSet for reviewer daily metrics. Represents the daily metrics for pull request reviewers.
    /// </summary>
    /// <value></value>
    public DbSet<ReviewerDailyMetrics> ReviewerDailyMetrics { get; set; }
    /// <summary>
    /// DbSet for reviewer monthly metrics. Represents the monthly metrics for pull request reviewers.
    /// </summary>
    /// <value></value>
    public DbSet<ReviewerMonthlyMetrics> ReviewerMonthlyMetrics { get; set; }
    /// <summary>
    /// DbSet for reviewer sprint metrics. Represents the sprint-based metrics for pull request reviewers.
    /// </summary>
    /// <value></value>
    public DbSet<ReviewerSprintMetrics> ReviewerSprintMetrics { get; set; }
    /// <summary>
    /// DbSet for pull request reviewer metrics. Represents the metrics for pull request reviewers.
    /// </summary>
    /// <value></value>
    public DbSet<PRReviewerMetrics> PRReviewerMetrics { get; set; }

    /// <summary>
    /// DbSet for teams. Represents the teams in the database.
    /// </summary>
    /// <value></value>
    public DbSet<Team> Teams { get; set; }

    /// <summary>
    /// Configures the model and relationships for the context. Called when the model for a derived context is being created.
    /// </summary>
    /// <param name="modelBuilder">The model builder for configuring entities.</param>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PRMetrics entity
        modelBuilder.Entity<PRMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PrNumber).IsRequired();
            entity.Property(e => e.Repository).HasMaxLength(100).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.Author).HasMaxLength(100).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.MergedBy).HasMaxLength(100).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.BaseBranch).HasMaxLength(150).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.State).HasMaxLength(20).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.WorkItemId).HasMaxLength(20).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType)
                .IsRequired(false);
            entity.Property(e => e.WorkItemId2).HasMaxLength(20).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType)
                .IsRequired(false);

            entity.HasIndex(e => e.Repository);
            entity.HasIndex(e => e.PrNumber);
            entity.HasIndex(e => e.Author);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.WorkItemId);
            entity.HasIndex(e => e.WorkItemId2);
            entity.HasIndex(e => new { e.CreatedAt, e.Author });
            entity.HasIndex(e => new { e.CreatedAt, e.TeamId });

            entity.HasOne(e => e.CopilotReviewMetrics)
                .WithOne(e => e.PRMetric)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DevExMetricItem)
                .WithOne(e => e.PRMetric)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Contributors)
                .WithOne(e => e.PRMetric)
                .HasForeignKey(e => e.PRMetricId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ReviewerDailyMetrics)
                .WithOne(e => e.PRMetric)
                .HasForeignKey(e => e.PRMetricId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ReviewerMetrics)
                .WithOne(e => e.PRMetric)
                .HasForeignKey(e => e.PRMetricId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Team)
                .WithMany(e => e.Metrics)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RunStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.LastRunAt);
            entity.HasIndex(e => new { e.Repository, e.PRNumber });
        });

        modelBuilder.Entity<DataMigrationHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.TenantId, e.MigrationId, e.MigrationType }).IsUnique();
        });

        modelBuilder.Entity<PRContributor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PRMetricId).IsRequired();
            entity.Property(e => e.Contributor).IsRequired().HasMaxLength(100);

            // Index on foreign key for better performance
            entity.HasIndex(e => e.PRMetricId);
        });

        modelBuilder.Entity<CopilotReviewMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PRMetricId).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => e.PRMetricId).IsUnique();
        });

        modelBuilder.Entity<DevExMetricItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PRMetricId).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => e.PRMetricId).IsUnique();
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.Region).IsRequired(false).HasMaxLength(50).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);
            entity.Property(e => e.ValueStream).IsRequired(false).HasMaxLength(50).UseCollation(CaseInsensitiveCollation).HasColumnType(CaseInsensitiveColumnType);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => new { e.Name, e.ValueStream, e.Region }).IsUnique();
        });

        modelBuilder.Entity<ReviewerDailyMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PRMetricId).IsRequired();
            entity.Property(e => e.Reviewer).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => new { e.PRMetricId, e.Reviewer, e.Date }).IsUnique();
        });

        modelBuilder.Entity<PRReviewerMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PRMetricId).IsRequired();
            entity.Property(e => e.Reviewer).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => new { e.PRMetricId, e.Reviewer }).IsUnique();
        });

        modelBuilder.Entity<ReviewerMonthlyMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Reviewer).IsRequired();
            entity.Property(e => e.Repository).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => new { e.Reviewer, e.Repository, e.Month, e.Year }).IsUnique();
        });

        modelBuilder.Entity<ReviewerSprintMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Reviewer).IsRequired();
            entity.Property(e => e.Repository).IsRequired();

            // Index on foreign key for better performance
            entity.HasIndex(e => new { e.Reviewer, e.Repository, e.SprintNumber, e.Year }).IsUnique();
        });

        // Configure multi-tenant entities — these will get a shadow TenantId column and automatic query filters
        modelBuilder.Entity<PRMetrics>().IsMultiTenant();
        modelBuilder.Entity<Team>().IsMultiTenant();
        modelBuilder.Entity<RunStatus>().IsMultiTenant();
        modelBuilder.Entity<ReviewerMonthlyMetrics>().IsMultiTenant();
        modelBuilder.Entity<ReviewerSprintMetrics>().IsMultiTenant();
        modelBuilder.Entity<DataMigrationHistory>().IsMultiTenant();

        base.OnModelCreating(modelBuilder);
    }

    protected override void BuildPostgresMigrationsInfrastructure(NpgsqlDbContextOptionsBuilder action)
    {
        action.MigrationsAssembly(DatabaseMigrationProvider.Postgres.MigrationsAssembly);
    }

    protected override void BuildSqliteMigrationsInfrastructure(SqliteDbContextOptionsBuilder action)
    {
        action.MigrationsAssembly(DatabaseMigrationProvider.SQLite.MigrationsAssembly);
    }
}

record DatabaseMigrationProvider(string Name, string MigrationsAssembly)
{
    public static readonly DatabaseMigrationProvider SQLite = new("SQLite", "Metrics.GitHub.Migrations.Sqlite");
    public static readonly DatabaseMigrationProvider Postgres = new("Postgres", "Metrics.GitHub.Migrations.Postgres");
}
