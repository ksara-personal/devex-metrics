using Metrics.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Metrics.GitHub.Extensions;
using Metrics.Models;
using Metrics.MultiTenant;

namespace Metrics.GitHub;

/// <summary>
/// Provides metrics integration with GitHub repositories.
/// </summary>
public sealed class GitHubExtensionProvider : IMetricsExtensionProvider
{
    const string ExtensionName = "GitHub";
    /// <summary>
    /// Name of the extension
    /// </summary>
    public string Name => ExtensionName;

    /// <summary>
    /// Description of the extension
    /// </summary>
    public string Description => "Provides metrics integration with GitHub repositories.";

    /// <summary>
    /// Service configurator for the extension
    /// </summary>
    /// <returns></returns>
    public IServiceConfigurator? ServiceConfigurator => new GitHubServiceConfigurator();

    /// <summary>
    /// OData configurator for the extension
    /// </summary>
    public IODataConfigurator? ODataConfigurator => null;
}

/// <summary>
/// Builds and configures GitHub related settings.
/// </summary>
sealed class GitHubServiceConfigurator : IServiceConfigurator
{
    internal const string GitHubSectionName = "GitHub";
    
    /// <summary>
    /// Static constructor to initialize GitHub configuration builder.
    /// </summary>
    static GitHubServiceConfigurator()
    {
        // set this so that, Pr url can be formatted properly
        PRMetrics.UrlFormatter = new GitHubPRUrlFormatter();
    }

    /// <summary>
    /// Configures GitHub related services.
    /// Validation of tenant-specific settings occurs when services are resolved within tenant scope.
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services) => services.AddGitHubServices();
}
