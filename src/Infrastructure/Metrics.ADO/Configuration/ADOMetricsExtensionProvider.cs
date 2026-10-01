namespace Metrics.ADO;

/// <summary>
/// ADO Metrics Extension for DevMetrics
/// </summary>
public sealed class ADOMetricsExtensionProvider : IMetricsExtensionProvider
{
    const string ExtensionName = "ADO";

    /// <summary>
    /// Name of the extension
    /// </summary> <summary>
    public string Name => ExtensionName;

    /// <summary>
    /// Description of the extension
    /// </summary>
    public string Description => "ADO Metrics Extension for DevMetrics";

    /// <summary>
    /// Service configurator for the extension
    /// </summary>
    /// <returns></returns>
    public IServiceConfigurator? ServiceConfigurator => new ADOServiceConfigurator();

    /// <summary>
    /// OData configuration support for the extension
    /// </summary>
    /// <returns></returns>
    public IODataConfigurator? ODataConfigurator => new ADOODataConfigurator();
}
