using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Microsoft.AspNetCore.OData.Query;
using Finbuckle.MultiTenant;

namespace Metrics.MCP.StreamableHTTP.Controllers;

/// <summary>
/// Action filter that provides detailed logging for OData controller actions.
/// </summary>
public sealed class DetailedLoggingActionFilter : ActionFilterAttribute
{
    readonly ILogger<DetailedLoggingActionFilter> _logger;
    Stopwatch? _stopwatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailedLoggingActionFilter"/> class.
    /// </summary>
    /// <param name="logger"></param>
    public DetailedLoggingActionFilter(ILogger<DetailedLoggingActionFilter> logger) => _logger = logger;

    /// <summary>
    /// Gets the source IP address from the HTTP context, considering proxy headers.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The source IP address</returns>
    private static string GetSourceIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header (common in load balancers/proxies)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, the first is the original client
            return xForwardedFor.Split(',')[0].Trim();
        }

        // Check for X-Real-IP header (used by some proxies)
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp.Trim();
        }

        // Fall back to the remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Called before the action method executes.
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _stopwatch = Stopwatch.StartNew();

        var request = context.HttpContext.Request;
        var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        var sourceIp = GetSourceIpAddress(context.HttpContext);

        _logger.LogInformation(
            "Controller Action Starting: {Method} {FullUrl} | SourceIP: {SourceIP} | Controller: {Controller} | Action: {Action}",
            request.Method,
            fullUrl,
            sourceIp,
            context.ActionDescriptor.RouteValues["controller"],
            context.ActionDescriptor.RouteValues["action"]);

        // Log OData query options if present
        if (context.ActionArguments.ContainsKey("options") &&
            context.ActionArguments["options"] is ODataQueryOptions options)
        {
            _logger.LogInformation(
                "OData Query Options - Filter: {Filter} | OrderBy: {OrderBy} | Top: {Top} | Skip: {Skip} | Select: {Select}",
                options.Filter?.RawValue ?? "None",
                options.OrderBy?.RawValue ?? "None",
                options.Top?.Value.ToString() ?? "None",
                options.Skip?.Value.ToString() ?? "None",
                options.SelectExpand?.RawSelect ?? "None");
        }

        // Log all query parameters
        if (request.Query.Any())
        {
            var queryParams = string.Join(", ", request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
            _logger.LogInformation("Query Parameters - {QueryParams}", queryParams);
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Called after the action method executes.
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        _stopwatch?.Stop();

        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;
        var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        var sourceIp = GetSourceIpAddress(context.HttpContext);

        var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(logLevel,
            "Controller Action Completed: {Method} {FullUrl} | SourceIP: {SourceIP} | Status: {StatusCode} | Duration: {Duration}ms | Controller: {Controller} | Action: {Action}",
            request.Method,
            fullUrl,
            sourceIp,
            response.StatusCode,
            _stopwatch?.ElapsedMilliseconds ?? 0,
            context.ActionDescriptor.RouteValues["controller"],
            context.ActionDescriptor.RouteValues["action"]);

        if (context.Exception != null)
        {
            _logger.LogError(context.Exception,
                "Controller Action Exception: {Method} {FullUrl} | SourceIP: {SourceIP} | Controller: {Controller} | Action: {Action}",
                request.Method,
                fullUrl,
                sourceIp,
                context.ActionDescriptor.RouteValues["controller"],
                context.ActionDescriptor.RouteValues["action"]);
        }

        base.OnActionExecuted(context);
    }
}
