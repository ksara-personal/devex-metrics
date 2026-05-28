using Microsoft.AspNetCore.OData;
using Metrics.MCP.StreamableHTTP.Controllers;

namespace Metrics.MCP.StreamableHTTP;

/// <summary>
/// Extension methods for configuring OData services in the dependency injection container.
/// This class provides fluent configuration for OData endpoints that expose development metrics data.
/// </summary>
static class ServiceCollectionODataExtensions
{
    /// <summary>
    /// Configures OData services for the DevMetrics application.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection for method chaining</returns>
    public static IServiceCollection AddODataServices(this IServiceCollection services, IEnumerable<IMetricsExtensionProvider>? extensions = null, ILogger? logger = null)
    {
        services.AddScoped<DetailedLoggingActionFilter>();
        services.AddScoped<MetricsActionFilterAttribute>();

        var mvcBuilder = services.AddControllers(options => options.Filters.Add<DetailedLoggingActionFilter>());
        mvcBuilder
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
                //options.JsonSerializerOptions.TypeInfoResolver = ODataJsonSerializerContext.Default;
                options.JsonSerializerOptions.DefaultBufferSize = 16384;
                options.JsonSerializerOptions.MaxDepth = 32;
            })
            .AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Expand()
                .Count()
                .SetMaxTop(1000)
                .AddRouteComponents("{__tenant__}/odata", ODataEdmBuilderUtils.GetEdmModel(services)));

        // add extensions odata controllers.
        extensions = extensions ?? services.BuildServiceProvider().GetServices<IMetricsExtensionProvider>();
        foreach (var extension in extensions)
        {
            var asm = extension.GetType().Assembly;
            logger?.LogInformation("Registering OData controllers from extension: {ExtensionName}", asm.FullName);
            mvcBuilder.AddApplicationPart(asm);
        }
        return services;
    }
}
