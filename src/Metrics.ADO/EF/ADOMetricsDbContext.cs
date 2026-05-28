using Metrics.Models;
using Metrics.ADO.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Metrics.ADO.EF;

/// <summary>
/// ADO Metrics Context
/// </summary>
public class ADOMetricsDbContext : MetricDbContext
{
    public const string ADOMigrationTable = "__EFMigrationsHistory_ADO";

    /// <summary>
    /// Initializes a new instance of the <see cref="ADOMetricsDbContext"/> class.
    /// Optionally accepts a multi-tenant context accessor for tenant isolation.
    /// </summary>
    /// <param name="options">The options for the DbContext.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="multiTenantContextAccessor">Optional multi-tenant context accessor for resolving the current tenant.</param>
    public ADOMetricsDbContext(DbContextOptions<ADOMetricsDbContext> options, IConfiguration configuration, IMultiTenantContextAccessor? multiTenantContextAccessor = null)
        : base(options, configuration, multiTenantContextAccessor)
    {
    }

    /// <summary>
    /// Gets or sets the work item metrics.
    /// </summary>
    /// <value></value>
    public DbSet<Models.WorkItemMetrics> WorkItemMetrics { get; set; }

    /// <summary>
    /// Gets or sets the work item sync status.
    /// </summary>
    /// <value></value>
    public DbSet<Models.WorkItemSyncStatus> WorkItemSyncStatus { get; set; }

    /// <summary>
    /// Configures the model and relationships for the context. Called when the model for a derived context is being created.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.WorkItemMetrics>(entity =>
        {
            entity.HasKey(e => e.WorkItemId);
            entity.Property(e => e.WorkItemId).ValueGeneratedNever();
            entity.Property(e => e.CreatedDate).IsRequired();
            //entity.Property(e => e.ClosedDate).IsRequired();

            entity.HasIndex(e => e.Team);
            entity.HasIndex(e => new { e.Team, e.CreatedDate, e.ClosedDate });
        });
        // avoid conflict with other EFMigrations in the same database
        modelBuilder.Entity<DataMigrationHistory>().ToTable("datamigrationhistory_ado");
        modelBuilder.Entity<WorkItemSyncStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.WorkItemId).IsUnique();
        });

        // Configure multi-tenant entities
        modelBuilder.Entity<Models.WorkItemMetrics>().IsMultiTenant();
        modelBuilder.Entity<DataMigrationHistory>().IsMultiTenant();
        modelBuilder.Entity<WorkItemSyncStatus>().IsMultiTenant();
    }

    protected override void BuildPostgresMigrationsInfrastructure(NpgsqlDbContextOptionsBuilder action)
    {
        action.MigrationsAssembly(DatabaseMigrationProvider.Postgres.MigrationsAssembly);
        action.MigrationsHistoryTable(ADOMetricsDbContext.ADOMigrationTable);
    }

    protected override void BuildSqliteMigrationsInfrastructure(SqliteDbContextOptionsBuilder action)
    {
        action.MigrationsAssembly(DatabaseMigrationProvider.SQLite.MigrationsAssembly);
        action.MigrationsHistoryTable(ADOMetricsDbContext.ADOMigrationTable);
    }
}

record DatabaseMigrationProvider(string Name, string MigrationsAssembly)
{
    public static readonly DatabaseMigrationProvider SQLite = new("SQLite", "Metrics.ADO.Migrations.Sqlite");
    public static readonly DatabaseMigrationProvider Postgres = new("Postgres", "Metrics.ADO.Migrations.Postgres");
}
