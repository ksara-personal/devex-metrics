using Metrics;
using Metrics.MCP;
using Metrics.MCP.StreamableHTTP;
using Metrics.MCP.StreamableHTTP.Authentication;
using AspNetCore.Authentication.ApiKey;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Runtime.Loader;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant;
using Metrics.MultiTenant;
using Metrics.Models;

// Add assembly resolver to find migrations assemblies
AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
{
    if (assemblyName.Name?.StartsWith("Metrics.ADO.Migrations") == true || 
        assemblyName.Name?.StartsWith("Metrics.GitHub.Migrations") == true)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyPath))
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
    }
    return null;
};

const string Realm = "Metrics";
const string Header_Auth_Scheme = "Header";
const string Header_Auth_Key = "x-api-key";
const string Basic_Auth_Scheme = "Basic";

var builder = WebApplication.CreateBuilder(args);
Environment.CurrentDirectory = AppContext.BaseDirectory;

var authSettings = builder.Configuration.GetSection(ConfigSectionNames.Authentication).Get<AuthenticationSettings>() ?? new AuthenticationSettings();

var serverUrl = authSettings.ServerUrl ?? "http://localhost:5000/";

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultBufferSize = 16384;
    options.SerializerOptions.MaxDepth = 32;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = null;
});

var mcpBuilder = builder.Services
    .Configure<AuthenticationSettings>(builder.Configuration.GetSection(ConfigSectionNames.Authentication))
    .AddScoped<IApiKeyRepository, InMemoryApiKeyRepository>()
    .AddHttpClient()
    .AddHostedService<MetricsSchedulerService>()
    .AddOpenApi()
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<MetricsTool>()
    .WithTools<AuthorMetricsTool>()
    .WithTools<SprintTool>()
    .WithTools<ReviewerMetricsTool>();

builder.Services.AddDIServices(builder.Configuration, (extensions, logger) => RegisterServicesAndTools(mcpBuilder, extensions, logger));


var defaultAuthenticateScheme = authSettings.OktaEnabled ? JwtBearerDefaults.AuthenticationScheme : ApiKeyDefaults.AuthenticationScheme;
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = defaultAuthenticateScheme;
})
.AddApiKeyInHeader(Header_Auth_Scheme, options => ConfigureApiKeyAuthentication(options, Header_Auth_Key))
.AddApiKeyInBasic(Basic_Auth_Scheme, options => ConfigureApiKeyAuthentication(options, Basic_Auth_Scheme));

if (authSettings.OktaEnabled && !string.IsNullOrWhiteSpace(authSettings.OktaAuthority))
{
    var authority = authSettings.OktaAuthority!;
    var audience = authSettings.OktaAudience ?? serverUrl;

    authenticationBuilder.AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = audience,
            ValidIssuer = authority,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context => Task.CompletedTask,
            OnTokenValidated = context => Task.CompletedTask,
            OnAuthenticationFailed = context => Task.CompletedTask,
            OnChallenge = context => Task.CompletedTask
        };
    });
}
/*
builder.Services.AddMultiTenant<AppTenantInfo>()
    .WithHeaderStrategy("X-Tenant-Id")
    .WithBasePathStrategy()
    .WithConfigurationStore(builder.Configuration, "TenantConfigurationStore")
    .WithPerTenantAuthentication();
*/

authenticationBuilder.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        Resource = new Uri(serverUrl),
        ResourceDocumentation = new Uri("https://github.com/your-org/devmetrics/blob/main/README.md"),
        ScopesSupported = [
            "openid",
        ],
    };

    if (!string.IsNullOrWhiteSpace(authSettings.OktaAuthority))
    {
        options.ResourceMetadata.AuthorizationServers.Add(new Uri(authSettings.OktaAuthority));
    }
});

// build authorization fallback policy including the configured schemes
var authSchemes = authSettings.OktaEnabled
     ? new[] { Header_Auth_Scheme, Basic_Auth_Scheme, JwtBearerDefaults.AuthenticationScheme, McpAuthenticationDefaults.AuthenticationScheme }
     : new[] { Header_Auth_Scheme, Basic_Auth_Scheme };

builder.Services.AddAuthorization(options =>
{
    var policy = new AuthorizationPolicyBuilder(authSchemes)
        .RequireAuthenticatedUser()
        .Build();
    
    options.DefaultPolicy = policy;
    options.FallbackPolicy = policy; // This makes ALL endpoints require authentication by default
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();
// adds forward headers for protocol and host, so that the response headers from https don't get converted to http; -JV
var cidr_block = Environment.GetEnvironmentVariable("CIDR_BLOCKS")?? "";
var parts = cidr_block.Split('/');
if (parts.Length == 2 && System.Net.IPAddress.TryParse(parts[0], out var ipAddress) && int.TryParse(parts[1], out var prefixLength))
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        KnownNetworks =
        {
            new IPNetwork(ipAddress, prefixLength),
        },
    });
}

// Add global exception handling first, before any other middleware
app.UseMiddleware<Metrics.MCP.StreamableHTTP.Middleware.GlobalExceptionHandlerMiddleware>();

// Add /ping endpoint for health check. No authorization, excluded from multi tenant resolution to ensure it always returns a response regardless of tenant context
app.MapGet("/ping", [AllowAnonymous]() => Results.Ok("hello")).ExcludeFromMultiTenantResolution();

app.UseRouting();

app.UseMultiTenant();

// Add tenant context to logging scope for all requests
app.UseMiddleware<Metrics.MCP.StreamableHTTP.Middleware.TenantLoggingScopeMiddleware>();

app.UseAuthentication()
    .UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers().RequireAuthorization();
    endpoints.MapMcp().RequireAuthorization();
});

await app.RunAsync();
return;

void ConfigureApiKeyAuthentication(ApiKeyOptions options, string keyName)
{
    options.Realm = Realm;
    options.KeyName = keyName;
    options.Events = new ApiKeyEvents
    {
        OnValidateKey = async context =>
        {
            var apiKeyRepository = context.HttpContext.RequestServices.GetRequiredService<IApiKeyRepository>();
            var apiKey = await apiKeyRepository.GetApiKeyAsync(context.ApiKey);
            var isValid = apiKey != null && apiKey.Key.Equals(context.ApiKey, StringComparison.OrdinalIgnoreCase);
            if (isValid)
            {
                context.Response.Headers.Append("ValidationCustomHeader", "From OnValidateKey");
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, apiKey.OwnerName, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(ClaimTypes.Name, apiKey.OwnerName, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();
            }
            else
            {
                context.NoResult();
            }
        }
    };
}

void RegisterServicesAndTools(IMcpServerBuilder mcpServerBuilder, IEnumerable<IMetricsExtensionProvider> extensions, ILogger? logger = null)
{
    mcpBuilder.Services.AddODataServices(extensions, logger);

    foreach (var extension in extensions)
    {
        var assembly = extension.GetType().Assembly;
        logger?.LogInformation($"Registering MCP tool assembly: {assembly.FullName}");
        mcpServerBuilder.WithToolsFromAssembly(assembly);
    }
}


