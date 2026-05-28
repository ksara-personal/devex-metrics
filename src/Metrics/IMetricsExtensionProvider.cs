using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;

namespace Metrics;

/// <summary>
/// Defines the contract for a metrics extension that can be integrated into the metrics system.
/// </summary>
public interface IMetricsExtensionProvider
{
    /// <summary>
    /// Gets the name of the metrics extension.
    /// </summary>
    /// <value></value>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the metrics extension.
    /// </summary>
    /// <value></value>
    public string Description { get; }

    /// <summary>
    /// Gets the service configurator for the metrics extension, if available.
    /// </summary>
    /// <value></value>
    public IServiceConfigurator? ServiceConfigurator { get; }

    /// <summary>
    /// Gets the OData configurator for the metrics extension, if available.
    /// </summary>
    /// <value></value>
    public IODataConfigurator? ODataConfigurator { get; }
}

/// <summary>
/// Defines the contract for configuration support in a metrics extension.
/// </summary>
public interface IServiceConfigurator
{
    /// <summary>
    /// Configures the services and configuration builder for the metrics extension.
    /// </summary>
    /// /// <param name="services"></param>
    void ConfigureServices(IServiceCollection services);
}

/// <summary>
/// Defines the contract for OData configuration support in a metrics extension.
/// </summary>
public interface IODataConfigurator
{
    /// <summary>
    /// Configures the OData model builder for the metrics extension.
    /// </summary>
    /// <param name="odataBuilder"></param>
    void ConfigureODataModel(ODataModelBuilder odataBuilder);
}

/// <summary>
/// Represents a metrics extension configuration.
/// </summary>
/// <value></value>
public sealed record MetricsExtension
{
    /// <summary>
    /// gets or sets the name of the metrics extension.
    /// </summary>
    /// <value></value>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the full name of the assembly containing the metrics extension.
    /// </summary>
    /// <value></value>
    public required string AssemblyFileName { get; set; }
}
