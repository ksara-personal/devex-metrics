using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OData.ModelBuilder;

namespace Metrics;

/// <summary>
/// Provides extension methods for registering metrics extensions.
/// </summary>
public static class RegisterMetricsExtensions
{
    /// <summary>
    /// Registers the services for all metrics extensions.
    /// </summary>
    /// <param name="extensions">The collection of metrics extensions.</param>
    /// <param name="services">The service collection to register services with.</param>
    public static IServiceCollection RegisterServices(this IEnumerable<IMetricsExtensionProvider> extensions, IServiceCollection services, ILogger logger)
    {
        extensions.Register(extension => extension.ServiceConfigurator?.ConfigureServices(services), logger, "services");
        return services;
    }

    /// <summary>
    /// Registers the OData model for all metrics extensions.
    /// </summary>
    /// <param name="extensions"></param>
    /// <param name="modelBuilder"></param>
    public static void RegisterODataModel(this IEnumerable<IMetricsExtensionProvider> extensions, ODataModelBuilder modelBuilder) =>
        extensions.Register(extension => extension.ODataConfigurator?.ConfigureODataModel(modelBuilder));

    /// <summary>
    /// Registers each metrics extension using the specified action.
    /// </summary>
    /// <param name="extensions"></param>
    /// <param name="registerAction"></param>
    static void Register(this IEnumerable<IMetricsExtensionProvider> extensions, Action<IMetricsExtensionProvider> registerAction, ILogger logger = null, string action = null)
    {
        foreach (var extension in extensions)
        {
            try
            {
                logger?.LogInformation($"Registering metrics extension for {action}: {extension.GetType().FullName}");
                registerAction(extension);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error registering metrics extension for {action}: {extension.GetType().FullName}");
            }
        }
    }
}
