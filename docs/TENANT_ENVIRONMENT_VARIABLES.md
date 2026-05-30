# Tenant-Specific Environment Variables

## Overview

The multi-tenant configuration system supports environment variable overrides for tenant-specific settings using a hierarchical naming convention. This allows you to customize configuration per tenant without modifying configuration files, which is essential for container deployments, CI/CD pipelines, and local development.

## Configuration Resolution Order

Settings are resolved in the following order (later sources override earlier ones):

1. **Global configuration** (`appsettings.json`, `appsettings.{Environment}.json`)
2. **Tenant configuration file** (e.g., `appsettings.tenant-1.json`, `appsettings.tenant-2.json`)
3. **Inline tenant settings** (stored in `AppTenantInfo.Settings` dictionary)
4. **Tenant-specific environment variables** (`{tenantId}__{section}__{key}`)

## Naming Convention

### Format
```
{tenantId}__{section}__{key}={value}
```

### Components
- **`{tenantId}`**: The tenant identifier (e.g., `tenant-1`, `tenant-2`) - case insensitive
- **`__`**: Double underscore separator between each level
- **`{section}`**: Configuration section name (e.g., `GitHub`, `ADO`, `SprintCalendar`, `Authentication`)
- **`{key}`**: Configuration key, supports nested properties with double underscores

> **⚠️ Important**: The prefix is just the tenant ID followed by `__`, NOT `TENANT__`. The system automatically prepends the tenant ID when loading configurations.

### Examples

#### Authentication Settings
```bash
# Override API key for "tenant-1" tenant
tenant-1__Authentication__ApiKey=my-secret-api-key-123

# Enable/disable Okta authentication
tenant-1__Authentication__OktaEnabled=true
tenant-1__Authentication__OktaAuthority=https://your-domain.okta.com
tenant-1__Authentication__OktaAudience=https://api.your-domain.com

# Override for different tenant
tenant-2__Authentication__ApiKey=different-api-key-456
tenant-2__Authentication__OktaEnabled=false
```

#### GitHub Settings
```bash
# Override GitHub PAT for "tenant-1" tenant  
tenant-1__GitHub__PAT=ghp_xyz123abc456

# Override GitHub organization settings
tenant-1__GitHub__Organizations__0__Owner=my-github-org
tenant-1__GitHub__Organizations__0__DefaultTeam=Engineering
tenant-1__GitHub__Organizations__0__DefaultRegion=US-East

# Override repository list for first organization
tenant-1__GitHub__Organizations__0__Repositories__0=repo1
tenant-1__GitHub__Organizations__0__Repositories__1=repo2
tenant-1__GitHub__Organizations__0__Repositories__2=repo3

# Bot configuration
tenant-1__GitHub__Organizations__0__IgnoreReviewsFromBotName=copilot-pull-request-reviewer
tenant-1__GitHub__Organizations__0__IgnoreCommentsFromBotName=code-review-bot

# Team patterns
tenant-1__GitHub__Organizations__0__IncludeTeamsWithNamePattern__0=pd-team-
tenant-1__GitHub__Organizations__0__IncludeTeamsWithNamePattern__1=squad-

# Code excellence teams
tenant-1__GitHub__Organizations__0__CodeExcellenceTeams__0=code-excellence-team-1
```

#### Azure DevOps Settings
```bash
# Override ADO PAT for "tenant-2" tenant
tenant-2__ADO__PersonalAccessToken=abc123xyz789

# Override ADO organization and project
tenant-2__ADO__Organization=myorg
tenant-2__ADO__ProjectName=MyProject

# Override specific field names
tenant-2__ADO__Fields__0=System.Title
tenant-2__ADO__Fields__1=System.State
tenant-2__ADO__Fields__2=Custom.Priority
```

#### Database Connection Strings
```bash
# Override database connection for different environments
tenant-1__ConnectionStrings__Postgres=Host=prod-db.example.com;Port=5432;Database=tenant1_prod;Username=tenant1_user;Password=secure_password

tenant-2__ConnectionStrings__Postgres=Host=prod-db.example.com;Port=5432;Database=tenant2_prod;Username=tenant2_user;Password=secure_password

# Override data store type
tenant-1__DataStoreType=Postgres
tenant-2__DataStoreType=SQLite
```

#### Sprint Calendar Settings
```bash
# Override sprint calendar for "tenant-1" tenant
tenant-1__SprintCalendar__SprintStartDate=2026-01-05
tenant-1__SprintCalendar__SprintLengthInWeeks=2
tenant-1__SprintCalendar__SprintDelayInWeeks=1

# Different sprint configuration for another tenant
tenant-2__SprintCalendar__SprintStartDate=2026-01-12
tenant-2__SprintCalendar__SprintLengthInWeeks=3
```

## Nested Properties

For nested configuration objects, use double underscores (`__`) to separate levels:

```bash
# Configuration structure:
# GitHub:
#   Organizations:
#     - Owner: "myorg"
#       DefaultTeam: "Engineering"
#       Repositories: ["repo1", "repo2"]

tenant-1__GitHub__Organizations__0__Owner=myorg
tenant-1__GitHub__Organizations__0__DefaultTeam=Engineering
tenant-1__GitHub__Organizations__0__Repositories__0=repo1
tenant-1__GitHub__Organizations__0__Repositories__1=repo2
```

### Complex Nested Example
```bash
# Deeply nested authentication settings
tenant-1__Authentication__OktaEnabled=true
tenant-1__Authentication__OktaAuthority=https://auth.example.com
tenant-1__Authentication__OktaAudience=https://api.example.com

# Nested ADO field configuration
tenant-1__ADO__Organization=myorg
tenant-1__ADO__Fields__0=System.Title
tenant-1__ADO__Fields__1=System.State
tenant-1__ADO__Fields__2=Custom.Priority
```

## Array Properties

Arrays are represented using zero-based indices. Each element in an array needs its own environment variable:

```bash
# For repository arrays
tenant-1__GitHub__Organizations__0__Repositories__0=first-repo
tenant-1__GitHub__Organizations__0__Repositories__1=second-repo
tenant-1__GitHub__Organizations__0__Repositories__2=third-repo

# For team pattern arrays
tenant-1__GitHub__Organizations__0__IncludeTeamsWithNamePattern__0=pd-team-
tenant-1__GitHub__Organizations__0__IncludeTeamsWithNamePattern__1=code-excellence-squad-

# For ADO field arrays
tenant-1__ADO__Fields__0=System.Title
tenant-1__ADO__Fields__1=System.AssignedTo
tenant-1__ADO__Fields__2=Custom.Priority
```

## Common Use Cases

### 1. Local Development
```bash
# Override for local development with personal credentials
export tenant-1__GitHub__PAT=ghp_your_personal_token
export tenant-1__ADO__PersonalAccessToken=your_ado_token
export tenant-1__ConnectionStrings__Postgres="Host=localhost;Port=5431;Database=devmetrics_local;Username=dev;Password=dev123"
export tenant-1__Authentication__OktaEnabled=false
export tenant-1__Authentication__ApiKey=local-dev-key
```

### 2. Docker/Docker Compose
```yaml
# docker-compose.yml
services:
  devmetrics:
    image: devmetrics:latest
    environment:
      # Tenant 1 configuration
      - tenant-1__GitHub__PAT=${LEARN_GITHUB_PAT}
      - tenant-1__ADO__PersonalAccessToken=${LEARN_ADO_PAT}
      - tenant-1__ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=tenant1_metrics;Username=metrics_user;Password=${DB_PASSWORD}
      - tenant-1__Authentication__OktaEnabled=true
      - tenant-1__Authentication__OktaAuthority=https://auth.company.com
      
      # Tenant 2 configuration  
      - tenant-2__GitHub__PAT=${ILLUMINATE_GITHUB_PAT}
      - tenant-2__ADO__PersonalAccessToken=${ILLUMINATE_ADO_PAT}
      - tenant-2__ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=tenant2_metrics;Username=metrics_user;Password=${DB_PASSWORD}
```

### 3. Kubernetes Deployments
```yaml
# kubernetes-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: devmetrics
spec:
  template:
    spec:
      containers:
      - name: devmetrics
        image: devmetrics:latest
        env:
          # Tenant 1 - from secrets
          - name: tenant-1__GitHub__PAT
            valueFrom:
              secretKeyRef:
                name: github-tokens
                key: tenant-1-pat
          - name: tenant-1__ADO__PersonalAccessToken
            valueFrom:
              secretKeyRef:
                name: ado-tokens
                key: tenant-1-pat
          - name: tenant-1__ConnectionStrings__Postgres
            valueFrom:
              secretKeyRef:
                name: db-connection-strings
                key: tenant-1-postgres
          
          # Tenant 1 - from configmap
          - name: tenant-1__Authentication__OktaEnabled
            valueFrom:
              configMapKeyRef:
                name: devmetrics-config
                key: tenant-1-okta-enabled
          - name: tenant-1__Authentication__OktaAuthority
            valueFrom:
              configMapKeyRef:
                name: devmetrics-config
                key: tenant-1-okta-authority
          
          # Tenant 2 - from secrets
          - name: tenant-2__GitHub__PAT
            valueFrom:
              secretKeyRef:
                name: github-tokens
                key: tenant-2-pat
          - name: tenant-2__ADO__PersonalAccessToken
            valueFrom:
              secretKeyRef:
                name: ado-tokens
                key: tenant-2-pat
```

### 4. CI/CD Pipeline Overrides
```bash
# Azure Pipelines / GitHub Actions / Jenkins
# Override for testing/staging deployments

# Staging environment
export tenant-1__GitHub__Organizations__0__Owner=myorg-staging
export tenant-1__ADO__Organization=myorg-test
export tenant-1__ConnectionStrings__Postgres="Host=staging-db.internal;Port=5432;Database=metrics_staging;..."
export tenant-1__Authentication__OktaAuthority=https://staging-auth.company.com

# Production environment  
export tenant-1__GitHub__Organizations__0__Owner=myorg-production
export tenant-1__ADO__Organization=myorg-prod
export tenant-1__ConnectionStrings__Postgres="Host=prod-db.internal;Port=5432;Database=metrics_prod;..."
export tenant-1__Authentication__OktaAuthority=https://auth.company.com
```

### 5. Multi-Tenant SaaS Deployment
```bash
# Separate database per tenant
export tenant-1__ConnectionStrings__Postgres="Host=db.example.com;Port=5432;Database=tenant1_db;Username=tenant1_user;Password=${TENANT1_DB_PASSWORD}"
export tenant-1__DataStoreType=Postgres

export tenant-2__ConnectionStrings__Postgres="Host=db.example.com;Port=5432;Database=tenant2_db;Username=tenant2_user;Password=${TENANT2_DB_PASSWORD}"
export tenant-2__DataStoreType=Postgres

# Different GitHub organizations per tenant
export tenant-1__GitHub__Organizations__0__Owner=your-github-org
export tenant-2__GitHub__Organizations__0__Owner=another-github-org

# Separate authentication per tenant
export tenant-1__Authentication__OktaAuthority=https://your-okta.com
export tenant-1__Authentication__OktaAudience=https://api.your-domain.com

export tenant-2__Authentication__OktaAuthority=https://your-okta.com  
export tenant-2__Authentication__OktaAudience=https://api.another-domain.com
```

### 6. Feature Flags and Bot Configuration
```bash
# Enable/disable bot review filtering per tenant
export tenant-1__GitHub__Organizations__0__IgnoreReviewsFromBotName=copilot-pull-request-reviewer
export tenant-1__GitHub__Organizations__0__IgnoreCommentsFromBotName=code-review-bot-github-app

# Different bot configuration for another tenant (disable filtering)
export tenant-2__GitHub__Organizations__0__IgnoreReviewsFromBotName=""
export tenant-2__GitHub__Organizations__0__IgnoreCommentsFromBotName=""
```

## Implementation Details

### TenantConfigurationProvider
The `TenantConfigurationProvider` class loads tenant-specific configurations and applies environment variable overrides. The key implementation detail is that the environment variable prefix is just the tenant ID:

```csharp
public IConfigurationRoot? GetConfiguration(string? configurationFile, string? tenantId = null)
{
    var cacheKey = string.IsNullOrWhiteSpace(tenantId) 
        ? configurationFile 
        : $"{configurationFile}|{tenantId}";

    return _cache.GetOrAdd(cacheKey, _ =>
    {
        var fullPath = Path.Combine(_basePath, configurationFile);
        var builder = new ConfigurationBuilder();
        
        // Load JSON config file
        builder.AddJsonFile(fullPath, optional: false, reloadOnChange: true);
        
        // Apply tenant-specific env vars (overrides file values)
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            // Prefix is just: {tenantId}__
            var prefix = $"{tenantId}__";
            builder.AddEnvironmentVariables(prefix);
        }
        
        return builder.Build();
    });
}
```

### Configuration Hierarchy
When a setting is requested for a tenant, the system:
1. Loads the global `appsettings.json`
2. Loads the tenant-specific config file (e.g., `appsettings.tenant-1.json`)
3. Applies environment variables with the tenant prefix (e.g., `tenant-1__`)
4. Returns the merged configuration

### Caching
- Configurations are cached per `(configurationFile, tenantId)` combination
- Cache key format: `"appsettings.tenant-1.json|tenant-1"`
- Environment variable changes require application restart to take effect
- Cache is thread-safe using `ConcurrentDictionary`

## Best Practices

### 1. Security
- **Never commit secrets**: Use environment variables for all sensitive values (PATs, passwords, API keys)
- **Use secrets management**: Store sensitive values in a secrets manager (e.g., your cloud provider's native secret store or Kubernetes Secrets)
- **Rotate credentials**: Regularly update PATs and tokens, especially in production
- **Least privilege**: Grant minimum required permissions for GitHub/ADO tokens

```bash
# ❌ Bad: Hardcoded in config file
"Authentication": {
  "ApiKey": "my-secret-key-123"
}

# ✅ Good: Override with environment variable
export tenant-1__Authentication__ApiKey="${VAULT_API_KEY}"
```

### 2. Environment Separation
- Use different tenant IDs or configs per environment (dev, staging, prod)
- Maintain separate secrets per environment
- Test configuration changes in non-prod first

```bash
# Development
export tenant-1__ConnectionStrings__Postgres="Host=dev-db;..."
export tenant-1__Authentication__OktaAuthority=https://dev-auth.company.com

# Production
export tenant-1__ConnectionStrings__Postgres="Host=prod-db;..."
export tenant-1__Authentication__OktaAuthority=https://auth.company.com
```

### 3. Documentation
- Document required environment variables for each tenant
- Include examples in README or deployment docs
- Use `.env.example` files for local development templates
- Document which settings can be safely overridden

### 4. Consistency
- Use the same tenant ID across all environment variables for a tenant
- Follow consistent naming patterns across tenants
- Use lowercase for tenant IDs to avoid case-sensitivity issues

```bash
# ✅ Good: Consistent tenant ID
export tenant-1__GitHub__PAT=...
export tenant-1__ADO__PersonalAccessToken=...
export tenant-1__ConnectionStrings__Postgres=...

# ❌ Bad: Inconsistent casing
export Tenant-1__GitHub__PAT=...
export LEARN__ADO__PersonalAccessToken=...
export tenant-1__ConnectionStrings__Postgres=...
```

### 5. Validation
- Test tenant resolution with `X-Tenant-Id` header
- Verify configuration values are applied correctly
- Use health checks to validate connectivity to external services
- Log configuration sources (file vs. env var) for debugging

### 6. Array Management
- When overriding arrays, set all elements explicitly
- Start with index 0 and increment sequentially
- Missing indices may cause unexpected behavior

```bash
# ✅ Good: Sequential indices
export tenant-1__GitHub__Organizations__0__Repositories__0=repo1
export tenant-1__GitHub__Organizations__0__Repositories__1=repo2
export tenant-1__GitHub__Organizations__0__Repositories__2=repo3

# ❌ Bad: Skipped indices
export tenant-1__GitHub__Organizations__0__Repositories__0=repo1
export tenant-1__GitHub__Organizations__0__Repositories__3=repo2  # Skipped 1 and 2
```

## Troubleshooting

### Environment Variable Not Working

#### 1. Check the Prefix Format
**Issue**: Using incorrect prefix format
```bash
# ❌ Wrong: TENANT__ prefix
export TENANT__tenant-1__GitHub__PAT=token123

# ✅ Correct: Just tenant ID
export tenant-1__GitHub__PAT=token123
```

#### 2. Verify Tenant ID
- Ensure tenant ID matches exactly (case-insensitive but be consistent)
- Check `X-Tenant-Id` header value in requests
- Verify tenant is configured in `TenantConfigurationStore`

```bash
# Check registered tenants in appsettings.json
"TenantConfigurationStore": {
  "Tenants": [
    {
      "Id": "tenant-1",           # ← This is your tenant ID
      "Identifier": "tenant-1",
      "ConfigurationFile": "appsettings.tenant-1.json"
    }
  ]
}
```

#### 3. Check Double Underscores
**Issue**: Missing or incorrect separators
```bash
# ❌ Wrong: Single underscore
export tenant-1_GitHub_PAT=token123

# ❌ Wrong: Three underscores
export tenant-1___GitHub___PAT=token123

# ✅ Correct: Double underscore
export tenant-1__GitHub__PAT=token123
```

#### 4. Restart Required
- Configuration is cached at application startup
- Environment variable changes require app restart
- Clear any config caches if implemented

```bash
# After setting environment variables, restart the application
docker-compose restart devmetrics
# or
kubectl rollout restart deployment/devmetrics
```

#### 5. Check Logs
The `TenantConfigurationProvider` logs when loading configurations:
```
[12:34:56] Loading tenant configuration from /app/configs/appsettings.tenant-1.json
[12:34:56] Adding environment variables with prefix: tenant-1__
```

Enable debug logging to see configuration resolution:
```bash
export tenant-1__Logging__LogLevel__Metrics.MultiTenant=Debug
```

### Common Errors and Solutions

#### Error: Configuration Value Not Applied
**Symptoms**: Setting environment variable but config still uses file value

**Diagnosis**:
```bash
# Print environment variables to verify they're set
env | grep tenant-1__

# Check if the variable is visible to the application
docker exec -it devmetrics env | grep tenant-1__
```

**Solutions**:
- Verify environment variable is set in the correct scope (container, pod, etc.)
- Check for typos in variable name
- Ensure variable is exported (`export` in bash)
- Restart application after setting variables

#### Error: Tenant Not Found
**Symptoms**: HTTP 404 or "Tenant not found" errors

**Diagnosis**:
```bash
# Check tenant configuration in appsettings.json
cat configs/appsettings.json | grep -A 10 "TenantConfigurationStore"

# Verify X-Tenant-Id header in request
curl -H "X-Tenant-Id: tenant-1" http://localhost:5000/api/metrics
```

**Solutions**:
- Ensure tenant is registered in `TenantConfigurationStore.Tenants`
- Check `X-Tenant-Id` header matches tenant `Identifier` field
- Verify tenant configuration file exists

#### Error: Database Connection Failed
**Symptoms**: Cannot connect to database after setting connection string

**Diagnosis**:
```bash
# Check connection string format
echo $tenant-1__ConnectionStrings__Postgres

# Test database connectivity
psql "$tenant-1__ConnectionStrings__Postgres"
```

**Solutions**:
- Verify connection string format is correct
- Check special characters are properly escaped
- Ensure database is accessible from application
- Verify credentials are correct

#### Error: Array Configuration Not Working
**Symptoms**: Only first element of array is used, or array is empty

**Diagnosis**:
```bash
# Check array environment variables
env | grep "tenant-1__GitHub__Organizations__0__Repositories"
```

**Solutions**:
- Use zero-based indices: `__0`, `__1`, `__2`, etc.
- Set all elements sequentially without gaps
- Don't skip indices

```bash
# ✅ Correct
export tenant-1__GitHub__Organizations__0__Repositories__0=repo1
export tenant-1__GitHub__Organizations__0__Repositories__1=repo2
export tenant-1__GitHub__Organizations__0__Repositories__2=repo3
```

### Verifying Configuration

#### Option 1: Check in Code
```csharp
// In a controller or service
var tenantConfig = _tenantConfigService.GetTenantConfiguration();
var githubPat = tenantConfig?["GitHub:PAT"];
var adoOrg = tenantConfig?["ADO:Organization"];

_logger.LogInformation("GitHub PAT: {PAT}", githubPat?.Substring(0, 10) + "...");
_logger.LogInformation("ADO Org: {Org}", adoOrg);
```

#### Option 2: Diagnostic Endpoint
Add a diagnostic endpoint to view resolved configuration (sanitize secrets!):
```csharp
[HttpGet("debug/config")]
public IActionResult GetConfig()
{
    var config = _tenantConfigService.GetTenantConfiguration();
    // Sanitize sensitive values before returning
    var sanitized = new
    {
        GitHubOrg = config["GitHub:Organizations:0:Owner"],
        AdoOrg = config["ADO:Organization"],
        Database = config["DataStoreType"],
        HasGitHubPat = !string.IsNullOrEmpty(config["GitHub:PAT"]),
        HasAdoPat = !string.IsNullOrEmpty(config["ADO:PersonalAccessToken"])
    };
    return Ok(sanitized);
}
```

#### Option 3: Logging at Startup
```csharp
// In Program.cs or Startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var config = app.Services.GetRequiredService<IConfiguration>();

logger.LogInformation("Data Store Type: {Type}", config["DataStoreType"]);
logger.LogInformation("ADO Organization: {Org}", config["ADO:Organization"]);
// Don't log sensitive values like PATs
```

### Getting Help

If you're still experiencing issues:
1. Enable debug logging: `export tenant-1__Logging__LogLevel__Default=Debug`
2. Collect application logs
3. Verify tenant configuration in `appsettings.json`
4. Check environment variables are visible: `env | grep <tenantId>__`
5. Review the [TenantConfigurationProvider implementation](../src/Metrics/MultiTenant/TenantConfigurationProvider.cs)

## Configuration Reference

### Available Configuration Sections

#### Authentication
```bash
<tenantId>__Authentication__ApiKey=<api-key>
<tenantId>__Authentication__OktaEnabled=<true|false>
<tenantId>__Authentication__OktaAuthority=<okta-domain-url>
<tenantId>__Authentication__OktaAudience=<api-audience-url>
<tenantId>__Authentication__ServerUrl=<server-url>
```

#### GitHub
```bash
<tenantId>__GitHub__PAT=<personal-access-token>
<tenantId>__GitHub__Organizations__<index>__Owner=<org-name>
<tenantId>__GitHub__Organizations__<index>__DefaultTeam=<team-name>
<tenantId>__GitHub__Organizations__<index>__DefaultRegion=<region>
<tenantId>__GitHub__Organizations__<index>__Repositories__<index>=<repo-name>
<tenantId>__GitHub__Organizations__<index>__IgnoreReviewsFromBotName=<bot-name>
<tenantId>__GitHub__Organizations__<index>__IgnoreCommentsFromBotName=<bot-name>
<tenantId>__GitHub__Organizations__<index>__IncludeTeamsWithNamePattern__<index>=<pattern>
<tenantId>__GitHub__Organizations__<index>__CodeExcellenceTeams__<index>=<team-name>
```

#### Azure DevOps (ADO)
```bash
<tenantId>__ADO__PersonalAccessToken=<pat-token>
<tenantId>__ADO__Organization=<org-name>
<tenantId>__ADO__ProjectName=<project-name>
<tenantId>__ADO__Fields__<index>=<field-name>
```

#### Database
```bash
<tenantId>__ConnectionStrings__Postgres=<connection-string>
<tenantId>__ConnectionStrings__SQLite=<connection-string>
<tenantId>__ConnectionStrings__File=<file-path>
<tenantId>__DataStoreType=<Postgres|SQLite|File>
```

#### Sprint Calendar
```bash
<tenantId>__SprintCalendar__SprintStartDate=<yyyy-MM-dd>
<tenantId>__SprintCalendar__SprintLengthInWeeks=<number>
<tenantId>__SprintCalendar__SprintDelayInWeeks=<number>
```

#### Logging
```bash
<tenantId>__Logging__LogLevel__Default=<level>
<tenantId>__Logging__LogLevel__Microsoft=<level>
<tenantId>__Logging__LogLevel__Metrics=<level>
```

Log levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`

## Quick Reference Commands

### View All Tenant Variables
```bash
# Linux/macOS
env | grep "^tenant-1__"
env | grep "^tenant-2__"

# Windows PowerShell
Get-ChildItem Env: | Where-Object { $_.Name -like "tenant-1__*" }
Get-ChildItem Env: | Where-Object { $_.Name -like "tenant-2__*" }
```

### Set Multiple Variables from File
```bash
# Create .env file
cat > .env.tenant-1 << 'EOF'
tenant-1__GitHub__PAT=ghp_token123
tenant-1__ADO__PersonalAccessToken=ado_token456
tenant-1__ConnectionStrings__Postgres=Host=localhost;Port=5432;...
EOF

# Load variables
export $(cat .env.tenant-1 | xargs)

# Or use with docker-compose
docker-compose --env-file .env.tenant-1 up
```

### Unset All Tenant Variables
```bash
# Linux/macOS
unset $(env | grep "^tenant-1__" | cut -d= -f1)

# Or more safely
for var in $(env | grep "^tenant-1__" | cut -d= -f1); do unset $var; done
```

### Validate Environment Variables
```bash
# Check critical variables are set
required_vars=(
  "tenant-1__GitHub__PAT"
  "tenant-1__ADO__PersonalAccessToken"
  "tenant-1__ConnectionStrings__Postgres"
)

for var in "${required_vars[@]}"; do
  if [ -z "${!var}" ]; then
    echo "ERROR: $var is not set"
  else
    echo "✓ $var is set"
  fi
done
```

## Related Files
- [TenantConfigurationProvider.cs](../src/Metrics/MultiTenant/TenantConfigurationProvider.cs) - Tenant configuration loading with env var support
- [TenantConfigurationService.cs](../src/Metrics/MultiTenant/TenantConfigurationService.cs) - Tenant configuration resolution service
- [ServiceCollectionExtensions.cs](../src/Metrics/Extensions/ServiceCollectionExtensions.cs) - Dependency injection setup
- [appsettings.json](../configs/appsettings.json) - Global configuration
- [appsettings.tenant-1.json](../configs/appsettings.tenant-1.json) - Tenant 1 configuration
- [appsettings.tenant-2.json](../configs/appsettings.tenant-2.json) - Tenant 2 configuration
- [Program.cs (StreamableHTTP)](../src/mcp/dotnet/Metrics.MCP.StreamableHTTP/Program.cs) - MCP server configuration

## Additional Resources

### Environment Variable Templates

#### .env.template (Local Development)
```bash
# Copy this file to .env.tenant-1 and fill in values
tenant-1__Authentication__ApiKey=
tenant-1__Authentication__OktaEnabled=false

tenant-1__GitHub__PAT=
tenant-1__GitHub__Organizations__0__Owner=

tenant-1__ADO__PersonalAccessToken=
tenant-1__ADO__Organization=

tenant-1__ConnectionStrings__Postgres=Host=localhost;Port=5431;Database=devmetrics;Username=dev;Password=
tenant-1__DataStoreType=Postgres
```

#### docker-compose.override.yml
```yaml
# Local development overrides
version: '3.8'
services:
  devmetrics:
    environment:
      - tenant-1__Authentication__ApiKey=${LEARN_API_KEY}
      - tenant-1__GitHub__PAT=${LEARN_GITHUB_PAT}
      - tenant-1__ADO__PersonalAccessToken=${LEARN_ADO_PAT}
      - tenant-1__ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=tenant1_metrics;Username=metrics;Password=${DB_PASSWORD}
```

### Migration Guide

#### From Old Format (TENANT__ prefix)
If you were using the old `TENANT__` prefix format, update your variables:

```bash
# Old format (❌ no longer works)
export TENANT__tenant-1__GitHub__PAT=token123

# New format (✅ correct)
export tenant-1__GitHub__PAT=token123
```

To convert all variables:
```bash
# Linux/macOS
for var in $(env | grep "^TENANT__" | cut -d= -f1); do
  new_var=$(echo $var | sed 's/^TENANT__//')
  export $new_var="${!var}"
  unset $var
  echo "Converted $var to $new_var"
done
```
