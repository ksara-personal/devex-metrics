using Finbuckle.MultiTenant;
using Metrics.MultiTenant;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace Metrics.MCP.StreamableHTTP.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions including authentication failures.
/// Provides appropriate error responses for both OData and MCP endpoints.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    readonly RequestDelegate _next;
    readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions by logging them and writing an appropriate error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var tenantInfo = context.GetMultiTenantContext<AppTenantInfo>()?.TenantInfo;
        var tenantId = tenantInfo?.Identifier ?? "Unknown";

        _logger.LogError(exception,
            "Unhandled exception occurred. TenantID: {TenantId}, Path: {Path}, Method: {Method}",
            tenantId,
            context.Request.Path,
            context.Request.Method);

        context.Response.ContentType = "application/json";

        // Handle different exception types
        switch (exception)
        {
            case AuthenticationFailureException authEx:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteErrorResponseAsync(context, "AuthenticationFailed", authEx.Message);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteErrorResponseAsync(context, "Unauthorized", "Authentication is required to access this resource.");
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await WriteErrorResponseAsync(context, "InternalServerError",
                    $"An error occurred while processing your request. {exception.Message}");
                break;
        }
    }

    /// <summary>
    /// Writes an error response to the HTTP context in either OData or MCP JSON-RPC format.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task WriteErrorResponseAsync(HttpContext context, string code, string message)
    {
        var isODataRequest = context.Request.Path.StartsWithSegments("/odata");

        if (isODataRequest)
        {
            // OData error format
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = code,
                    message = message
                }
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        else
        {
            // MCP JSON-RPC error format
            await context.Response.WriteAsJsonAsync(new
            {
                jsonrpc = "2.0",
                id = (string?)null,
                error = new
                {
                    code = -32603, // Internal error
                    message = message,
                    data = new { errorCode = code }
                }
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
