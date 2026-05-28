using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics.DataMigrations;


/// <summary>
/// Migration runner for handling database migrations.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public sealed class MigrationRunner<TContext> where TContext : MetricDbContext
{
    readonly IDbContextFactory<TContext> _contextFactory;
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<MigrationRunner<TContext>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunner{TContext}"/> class.
    /// </summary>
    /// <param name="contextFactory"></param>
    public MigrationRunner(IDbContextFactory<TContext> contextFactory,
        IServiceProvider serviceProvider,
        ILogger<MigrationRunner<TContext>> logger)
    {
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Runs all pending data migrations.
    /// </summary>
    /// <returns></returns>
    public async Task RunMigrationsAsync( )
    {
        using var context = _contextFactory.CreateDbContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        _logger.LogInformation("Applying migrations.. Already applied: {Applied}, Pending:{Pending}",
            string.Join(", ", appliedMigrations),
            string.Join(", ", pendingMigrations));

        var contextType = typeof(TContext);
        var migrations = _serviceProvider.GetKeyedServices<IDataMigration<TContext>>(contextType);
        try
        {
            var dataMigrations = await context.DataMigrations.ToListAsync();
            foreach (var dm in migrations)
            {
                if (!dataMigrations.Any(m => m.MigrationId == dm.EFMigrationId))
                {
                    _logger.LogInformation("Data Migration not applied: {MigrationId}, Type: {MigrationType}",
                        dm.EFMigrationId, dm.GetType().Name);

                    await ApplyMigrations(dm.EFMigrationId, context, migrations);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking applied data migrations.");
        }

        if (pendingMigrations.Any())
        {
            foreach (var pendingMigration in pendingMigrations)
            {
                _logger.LogInformation("Applying Data migration: {Migration}", pendingMigration);
                await context.Database.MigrateAsync(pendingMigration);
                await ApplyMigrations(pendingMigration, context, migrations);
            }
        }

        _logger.LogInformation("Migrations applied.");
    }

    /// <summary>
    /// Applies the specified migration. Used for integration tests
    /// </summary>
    /// <param name="migrationId"></param>
    /// <returns></returns>
    public async Task ApplyMigration(string migrationId)
    {
        using var context = _contextFactory.CreateDbContext();
        var migrations = _serviceProvider.GetServices<IDataMigration<TContext>>();
        await ApplyMigrations(migrationId, context, migrations);
    }

    /// <summary>
    /// Applies the specified migrations.
    /// </summary>
    /// <param name="currentMigration"></param>
    /// <param name="context"></param>
    /// <param name="migrations"></param>
    /// <returns></returns>
    async Task ApplyMigrations(string currentMigration, TContext context, IEnumerable<IDataMigration<TContext>> migrations)
    {
        var eligibleMigrations = migrations.Where(m => currentMigration.Contains(m.EFMigrationId, StringComparison.OrdinalIgnoreCase));
        if (eligibleMigrations.Any())
        {
            _logger.LogInformation("Current migration: {CurrentMigration}, Eligible new migrations are:{NewMigrations}",
                currentMigration,
                string.Join(", ", eligibleMigrations.Select(m => m.EFMigrationId)));

            foreach (var migration in eligibleMigrations)
            {
                var migrationType = migration.GetType().Name;
                migrationType = migrationType.Length > DataMigrationHistory.MigrationColumnLength ? migrationType[..DataMigrationHistory.MigrationColumnLength] : migrationType;

                var applied = await context.DataMigrations
                    .Where(x => x.MigrationId == migration.EFMigrationId && x.MigrationType == migrationType)
                    .SingleOrDefaultAsync();

                if (applied is null)
                {
                    _logger.LogInformation("Running data migration: {Migration}, {MigrationType}", migration.EFMigrationId, migrationType);
                    await migration.RunAsync(context);

                    // mark it as complete.
                    context.DataMigrations.Add(new DataMigrationHistory
                    {
                        MigrationId = migration.EFMigrationId,
                        MigrationType = migrationType,
                        AppliedOn = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("Data migration already applied: {Migration}, {MigrationType}", migration.EFMigrationId, migrationType);
                }
            }
        }
    }
}