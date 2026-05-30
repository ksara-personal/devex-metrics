# Automatic Tenant ID Injection in Logs

## Overview
Instead of manually adding `TenantID` to every log statement, we use **logging scopes** via middleware to automatically inject tenant information into all log messages within a request.

## How It Works

### 1. Middleware (`TenantLoggingScopeMiddleware.cs`)
The middleware creates a logging scope at the start of each request that contains tenant information:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["TenantId"] = tenantId,
    ["TenantName"] = tenantInfo?.Name ?? "Unknown"
}))
{
    await _next(context);
}
```

All logs within this scope automatically include the tenant information.

### 2. Middleware Registration (`Program.cs`)
The middleware is registered after `UseMultiTenant()` so tenant context is already resolved:

```csharp
app.UseMultiTenant();
app.UseMiddleware<TenantLoggingScopeMiddleware>();
app.UseAuthentication();
```

### 3. Logging Configuration (`appsettings.json`)
The configuration has `IncludeScopes: true` which enables scope values in log output:

```json
{
  "Logging": {
    "Console": {
      "FormatterOptions": {
        "IncludeScopes": true
      }
    }
  }
}
```

## Benefits

1. **No Code Changes Required**: Existing log statements work without modification
2. **Consistent**: All logs automatically include tenant information
3. **Maintainable**: Single place to manage tenant logging logic
4. **Flexible**: Easy to add more contextual information to the scope

## Log Output Examples

### Before (without scopes):
```
info: AuthorMetricsController[0]
      Error retrieving author metrics by sprint
```

### After (with scopes):
```
info: AuthorMetricsController[0]
      => TenantId: tenant-1, TenantName: Tenant 1
      Error retrieving author metrics by sprint
```

## Simplifying Existing Code

You can now **remove** the explicit `TenantID` parameters from existing log statements since the tenant information is automatically included via scopes:

### DetailedLoggingActionFilter.cs
**Before:**
```csharp
_logger.LogInformation(
    "Controller Action Starting: {Method} {FullUrl} | TenantID: {TenantID} | SourceIP: {SourceIP}",
    request.Method, fullUrl, tenantId, sourceIp);
```

**After (simplified):**
```csharp
_logger.LogInformation(
    "Controller Action Starting: {Method} {FullUrl} | SourceIP: {SourceIP}",
    request.Method, fullUrl, sourceIp);
// TenantID is automatically included via scope
```

### AuthorMetricsController.cs
**Before:**
```csharp
_logger.LogError(ex, "Error retrieving author metrics by sprint - TenantID: {TenantID}", GetTenantIdentifier());
```

**After (simplified):**
```csharp
_logger.LogError(ex, "Error retrieving author metrics by sprint");
// TenantID is automatically included via scope
```

You can remove the `GetTenantIdentifier()` helper methods since they're no longer needed.

## Advanced: Adding More Context

You can easily add more contextual information to all logs by updating the middleware:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["TenantId"] = tenantId,
    ["TenantName"] = tenantInfo?.Name ?? "Unknown",
    ["UserId"] = context.User.Identity?.Name ?? "Anonymous",
    ["RequestId"] = context.TraceIdentifier
}))
{
    await _next(context);
}
```

## Using with Structured Logging (Serilog)

If you're using Serilog, scopes work even better with enrichers:

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()  // This enables scope enrichment
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));
```

## Console App Usage

For the console app, you can set the logging scope when setting the tenant context:

```csharp
static void SetTenantContext(IServiceProvider serviceProvider, string tenantId)
{
    var contextSetter = serviceProvider.GetRequiredService<IMultiTenantContextSetter>();
    var tenant = new AppTenantInfo
    {
        Id = tenantId,
        Identifier = tenantId,
        Name = tenantId,
        ConfigurationFile = $"appsettings.{tenantId}.json"
    };
    var multiTenantContext = new MultiTenantContext<AppTenantInfo> { TenantInfo = tenant };
    contextSetter.MultiTenantContext = multiTenantContext;
    
    // Set logging scope for console app
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = tenantId }))
    {
        Console.WriteLine($"Tenant context set to: {tenantId}");
    }
}
```

## Testing

To verify scopes are working, check that log output includes the scope information:
```bash
# Should see tenant context in logs
curl -H "X-Tenant-Id: tenant-1" https://your-api.com/odata/Metrics
```

Look for output like:
```
info: MetricsController[0]
      => TenantId: tenant-1, TenantName: Tenant 1
      Controller Action Starting: GET /odata/Metrics
```
