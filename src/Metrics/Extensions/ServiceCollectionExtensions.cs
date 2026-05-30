using System.Reflection;
using System.Text.Json;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Metrics.DataMigrations;
using Metrics.EF;
using Metrics.Models;
using Metrics.MultiTenant;
using Metrics.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics;

/// <summary>
/// Extension class to auto register implemntation types.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services for dependency injection.
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection AddDIServices(this IServiceCollection services, IConfigurationBuilder configurationBuilder, Action<IEnumerable<IMetricsExtensionProvider>, ILogger>? registerExtensions = null)
    {
        var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        configurationBuilder.AddJsonFile(Path.Combine(folder, "appsettings.json"), optional: false, reloadOnChange: true);

        var overrideKestrel = Environment.GetEnvironmentVariable("OVERRIDE_KESTREL");
        if (bool.TryParse(overrideKestrel, out var useKestrelOverride) && useKestrelOverride)
        {
            configurationBuilder.AddJsonFile(Path.Combine(folder, "appsettings.kestrel.json"), optional: true, reloadOnChange: true);
        }
        configurationBuilder.AddEnvironmentVariables();

        var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");

        // Build a temporary configuration to read the MetricsExtensions section
        var tempConfig = configurationBuilder.Build();
        var extensionsSettings = tempConfig.GetSection(ConfigSectionNames.MetricsExtensions).Get<MetricsExtensionsSettings>();

        // Register Finbuckle.MultiTenant with header-based strategy and configuration store
        
        services.AddMultiTenant<AppTenantInfo>()
            .WithHeaderStrategy("X-Tenant-Id")
            .WithBasePathStrategy(options => options.RebaseAspNetCorePathBase = true)
            .WithRouteStrategy("__tenant__",true)
            .WithConfigurationStore(tempConfig, ConfigSectionNames.TenantConfigurationStore);

        // Register tenant configuration provider (singleton) that loads and caches per-tenant config files
        var configBasePath = AppContext.BaseDirectory;
        services.AddSingleton(sp =>
            new TenantConfigurationProvider(configBasePath, sp.GetRequiredService<ILogger<TenantConfigurationProvider>>()));

        // Register tenant configuration service for accessing tenant-specific settings
        services.AddScoped<TenantConfigurationService>();

        var configRoot = configurationBuilder.Build();
        var extensions = LoadExtensions(extensionsSettings, services, logger);

        services
            .AddSingleton<ConfigService>()
            .AddScoped<SprintCalendar>()
            .AddScoped<DataClient>()
            .AddScoped<MetricsExportService>()
            .AddDbServices()
            .Configure<JsonSerializerOptions>(options =>
            {
                options.TypeInfoResolver = SettingsJsonContext.Default;
                options.WriteIndented = true;
            })
            .Configure<MetricsExtensionsSettings>(configRoot.GetSection(ConfigSectionNames.MetricsExtensions));

        services.RegisterExtensionServices(extensions, logger);

        registerExtensions?.Invoke(extensions, logger);
        return services;
    }

    /// <summary>
    /// Loads metrics extensions from configured assemblies.
    /// </summary>
    /// <param name="settings">The metrics extensions settings containing assembly information.</param>
    /// <returns>A collection of loaded metrics extensions.</returns>
    static IEnumerable<IMetricsExtensionProvider> LoadExtensions(MetricsExtensionsSettings? settings, IServiceCollection services, ILogger logger)
    {
        if (settings?.Extensions == null || !settings.Extensions.Any())
        {
            logger.LogInformation("No metrics extensions configured.");
            return Enumerable.Empty<IMetricsExtensionProvider>();
        }

        var extensions = new List<IMetricsExtensionProvider>();
        
        foreach (var extensionConfig in settings.Extensions)
        {
            logger.LogInformation($"Loading metrics extension from assembly: {extensionConfig.AssemblyFileName}");
            try
            {
                // Load the assembly
                var assembly = System.Reflection.Assembly.LoadFrom(extensionConfig.AssemblyFileName);
                
                // Find types that implement IMetricsExtension
                var extensionTypes = assembly.GetTypes()
                    .Where(t => typeof(IMetricsExtensionProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                
                foreach (var type in extensionTypes)
                {
                    services.AddTransient(typeof(IMetricsExtensionProvider), type);

                    // Create an instance of the extension
                    if (Activator.CreateInstance(type) is IMetricsExtensionProvider extension)
                    {
                        extensions.Add(extension);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the error - for now, just continue
                logger.LogError(ex, $"Failed to load metrics extension from assembly: {extensionConfig.AssemblyFileName}");
            }
        }

        return extensions;
    }

    /// <summary>
    /// Registers services from all metrics extensions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="extensions">The collection of metrics extensions.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The service collection for chaining.</returns>
    static IServiceCollection RegisterExtensionServices(this IServiceCollection services,
        IEnumerable<IMetricsExtensionProvider> extensions, ILogger logger) =>
        extensions.RegisterServices(services, logger);

    /// <summary>
    /// Registers the database context with provider configuration based on DataStoreType.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterMetricDbContext<TContext>(this IServiceCollection services)
        where TContext : MetricDbContext => services.AddDbContextFactory<TContext>((sp, options) => {}, ServiceLifetime.Transient);
    
    /// <summary>
    /// Adds db specific services.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    static IServiceCollection AddDbServices(this IServiceCollection services)
    {
        return services
            .RegisterMetricDbContext<DevExMetricDbContext>()
            .AddKeyedScoped<IMetricsPersistenceService, DefaultPersistenceService>(DataStoreType.API)
            .AddKeyedScoped<IMetricsPersistenceService, FilePersistenceService>(DataStoreType.File)
            .AddKeyedScoped<IMetricsPersistenceService, MetricsPersistenceService<DevExMetricDbContext>>(typeof(DevExMetricDbContext))
            .AddScoped<IMetricsPersistenceService>(sp =>
            {
                var configService = sp.GetRequiredService<ConfigService>();
                return configService.DataStoreType switch
                {
                    DataStoreType.API => sp.GetRequiredKeyedService<IMetricsPersistenceService>(DataStoreType.API),
                    DataStoreType.File => sp.GetRequiredKeyedService<IMetricsPersistenceService>(DataStoreType.File),
                    DataStoreType.SQLite or DataStoreType.Postgres or DataStoreType.InMemory => sp.GetRequiredKeyedService<IMetricsPersistenceService>(typeof(DevExMetricDbContext)),
                    _ => throw new NotSupportedException($"Data store type {configService.DataStoreType} is not supported for IMetricsPersistenceService.")
                }; 
            });
    }

    /// <summary>
    /// Adds migration runners for the specified DbContext type.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddMigrationRunners<TContext>(this IServiceCollection services)
        where TContext : MetricDbContext => services.AddScoped<MigrationRunner<TContext>>();
}
