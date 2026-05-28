using Finbuckle.MultiTenant;
using Metrics.MultiTenant;

namespace Metrics.MCP.StreamableHTTP.Middleware;

/// <summary>
/// Middleware that adds tenant context to all log messages within the request scope.
/// This allows tenant ID to be automatically included in all logs without modifying individual log statements.
/// </summary>
public sealed class TenantLoggingScopeMiddleware
{
    readonly RequestDelegate _next;
    readonly ILogger<TenantLoggingScopeMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantLoggingScopeMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    public TenantLoggingScopeMiddleware(RequestDelegate next, ILogger<TenantLoggingScopeMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to set up logging scope with tenant context.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantInfo = context.GetMultiTenantContext<AppTenantInfo>()?.TenantInfo;
        var tenantId = tenantInfo?.Identifier ?? "Unknown";

        // Create a logging scope with tenant information that will be included in all logs
        using (_logger.BeginScope("TenantID: {TenantId}, TenantName: {TenantName};", tenantId, tenantInfo?.Name ?? "Unknown"))
        {
            await _next(context);
        }
    }
}
