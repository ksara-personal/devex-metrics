# Developer Efficiency Metrics

DevExMetrics is a cross-platform and multi tenant .NET 10.0 solution for extracting, analyzing, and reporting software engineering metrics from GitHub and Azure DevOps pull requests. It supports multi-tenant deployments, querying by team, author, value stream, sprint, and more, and can output results to JSON files, a database, or expose data through OData and MCP (Model Context Protocol) endpoints.

## Projects

The solution follows clean architecture: dependencies point inwards, from the hosts through
the adapters to the application use cases and finally the domain. See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the layer rules.

```
src/
  Core/
    Metrics.Domain              entities, value objects, domain contracts
    Metrics.Application         use cases and the ports they depend on
  Infrastructure/
    Metrics.Infrastructure      EF Core, stores, HTTP, multi-tenancy, DI composition
    Metrics.GitHub              GitHub adapter (runtime-loaded extension)
    Metrics.ADO                 Azure DevOps adapter (runtime-loaded extension)
    Migrations/                 EF Core migrations per provider and database
  Presentation/
    Metrics.MCP.StreamableHTTP  MCP server, OData endpoints, background scheduler
    MetricsConsoleApp           CLI for querying and syncing metrics
tests/
  Metrics.Tests                 application and persistence tests
  Metrics.ADO.Tests             Azure DevOps adapter tests
  Metrics.ArchitectureTests     enforces the layer dependency rules
```

| Layer | Project | May depend on |
|---|---|---|
| Domain | `Metrics.Domain` | nothing (BCL plus the `[MultiTenant]` marker attribute) |
| Application | `Metrics.Application` | Domain, `Microsoft.Extensions.*.Abstractions` |
| Infrastructure | `Metrics.Infrastructure`, `Metrics.GitHub`, `Metrics.ADO`, `Metrics.*.Migrations.*` | Domain, Application |
| Presentation | `Metrics.MCP.StreamableHTTP`, `MetricsConsoleApp` | every layer |

Package versions are managed centrally in `Directory.Packages.props`; shared build settings
live in `Directory.Build.props`.

## Supported Providers

- **GitHub** — Multi-org support, team-based metrics, Copilot review analytics, coding agent (bot) tracking, reviewer performance. Fully operational.
- **Azure DevOps** — Work item sync, epic metrics by team and release, feature flag work item queries. Operational for ADO-specific use cases.

## Metrics Tracked

### Pull Request Metrics
- PR size classification (small / medium / large), initial and subsequent lines changed
- Cycle time, lead time, coding time, review time, pickup time, merge time, approve time
- Maturity percentage, feature vs. non-feature classification
- Total comments, review comments, changes requested (by team / code excellence / others)
- Comments after final approval, average review comments per commit
- Commit breakdown by size (small / medium / large) with associated comment counts
- Work item linkage (WorkItemId), draft transitions, contributor list

### Reviewer Metrics
- Reviews requested and submitted, average response time, approval rate, changes requested count
- Aggregated by sprint, month, or individual reviewer

### Author Metrics
- PRs authored and closed, average PR size, average cycle time, average comments, PR maturity
- Aggregated by sprint or month, at both individual and team level

### Copilot Review Metrics
- Files reviewed by Copilot, comment counts, per-PR and cross-team summaries

### Azure DevOps Metrics
- Epic work item metrics by team, release version, or combined filters
- Feature flag work items per release version

## Database Support

| Mode | Use Case |
|---|---|
| **SQLite** | Local development (default) |
| **PostgreSQL** | Production deployments |
| **InMemory** | Testing |

`DataStoreType` in `appsettings.json` accepts: `SQLite`, `Postgres`, `InMemory` (case-insensitive).

## Multi-Tenant Architecture

DevMetrics is built on [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant). Each tenant has its own isolated configuration and database schema.

- The global `appsettings.json` defines the tenant registry under `TenantConfigurationStore.Tenants`.
- Each tenant points to its own config file (e.g., `appsettings.{tenantId}.json`) which overrides GitHub orgs, teams, repos, sprint calendar, ADO settings, and connection strings.
- The `x-tenant-id` HTTP header selects the tenant for all OData and MCP requests.
- All CLI commands require `--tenant-id` (alias `-tid`).

**Example tenant entry in `appsettings.json`:**
```json
{
  "TenantConfigurationStore": {
    "Tenants": [
      {
        "Id": "mytenant",
        "Identifier": "mytenant",
        "Name": "My Tenant",
        "ConfigurationFile": "appsettings.mytenant.json"
      }
    ]
  }
}
```

## Configuration

Edit `configs/appsettings.json` and your tenant-specific config file (e.g., `configs/appsettings.mytenant.json`).

Key settings in the tenant config file:

```json
{
  "GitHub": {
    "Organizations": [
      {
        "Owner": "your-github-org",
        "DefaultTeam": "Engineering",
        "DefaultRegion": "Global",
        "IncludeTeamsWithNamePattern": ["team-"],
        "Repositories": ["repo-1", "repo-2"],
        "CodeExcellenceTeams": ["code-excellence-team-1"]
      }
    ],
    "Teams": [
      {
        "Region": "Global",
        "TimeZone": "UTC",
        "Teams": [
          { "Name": "Alpha", "RemoteName": "team-alpha", "ValueStream": "Core Platform" }
        ]
      }
    ],
    "UserTeamMappings": { "github-username": "team-alpha" },
    "ExcludeBranchPrefixes": ["release/"]
  },
  "ADO": {
    "Organization": "your-ado-org",
    "Project": "YourProject",
    "PersonalAccessToken": ""
  },
  "SprintCalendar": {
    "EarliestKnownSprintStartDate": "2023-01-01",
    "EarliestKnownSprintNumber": 1,
    "EarliestKnownReleaseName": "1.0.0",
    "GenerateSprintsUptoDate": "2030-12-31"
  }
}
```

### Environment Variables

All config keys can be overridden with environment variables using double-underscore (`__`) as the separator. The tenant ID is used as a prefix to scope overrides to a specific tenant.

```bash
# GitHub PAT for a specific tenant
export mytenant__GitHub__PAT="ghp_yourtoken"

# Connection strings
export mytenant__ConnectionStrings__SQLite="Data source=/path/to/devmetrics.db"
export mytenant__ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=devmetrics;Username=user;Password=pwd"

# Authentication
export mytenant__Authentication__ApiKey="your-api-key"
```

See [TENANT_ENVIRONMENT_VARIABLES.md](./docs/TENANT_ENVIRONMENT_VARIABLES.md) for the full reference.

## Setting up the Database

### SQLite (default)
No server required. The database file is created automatically on first run.

```bash
dotnet ef database update --context DevExMetricSqliteDbContext --project src/Infrastructure/Metrics.Infrastructure/Metrics.Infrastructure.csproj
```

### PostgreSQL
Spin up a local Postgres instance using Docker:

```bash
docker compose -f local-postgresql/docker-compose.yml up -d
```

Then apply migrations:

```bash
dotnet ef database update --context DevExMetricPostgresDbContext --project src/Infrastructure/Metrics.Infrastructure/Metrics.Infrastructure.csproj
```

Update your tenant config (or env variable) with the connection string, e.g.:
```
Host=localhost;Port=5431;Database=postgres;Username=postgres;Password=postgresql;
```

> **Note:** If you run the `Metrics.MCP.StreamableHTTP` project, database migrations are applied automatically on startup.

### Migration Scripts

The `scripts/` folder provides helpers for managing EF Core migrations:

```bash
# Generate a new migration
./scripts/generate-migrations.sh <provider> <MigrationName>

# Apply pending migrations
./scripts/apply-migrations.sh <provider>

# Remove the last migration
./scripts/remove-migrations.sh <provider>
```

Supported providers: `github-sqlite`, `github-postgres`, `ado-sqlite`, `ado-postgres`, `sqlite`, `postgres`, `github`, `ado`, `all`

## CLI Usage

Build and run from the `src/Presentation/MetricsConsoleApp` directory:

```sh
DOTNET_ENVIRONMENT=Release dotnet build
```

All commands require `--tenant-id` (alias `-tid`).

### Command Reference

| Command | Description |
|---|---|
| `writetofile` | Fetch from GitHub API and write metrics to a JSON file |
| `writetodb` | Fetch from GitHub API and write metrics to the configured database |
| `export` | Copy metrics between data stores (e.g., File → Postgres) |
| `export-team-metrics` | Export per-team metrics to individual JSON files |
| `getbyauthor` | Query stored metrics for a specific author |
| `getbyteam` | Query stored metrics for a specific team |
| `getbyid` | Query stored metrics for a specific PR |

### Examples

```sh
# Sync metrics from GitHub into the database
./MetricsConsoleApp writetodb --tenant-id mytenant

# Sync a specific date range
./MetricsConsoleApp writetodb --tenant-id mytenant --start 2025-01-01 --end 2025-06-30

# Write to JSON file
./MetricsConsoleApp writetofile --tenant-id mytenant --start 2025-01-01 --end 2025-06-30

# Export from file to Postgres
./MetricsConsoleApp export --tenant-id mytenant --source File --target Postgres

# Export per-team metrics to individual files
./MetricsConsoleApp export-team-metrics --tenant-id mytenant --source Postgres

# Query by author (login, email, or display name)
./MetricsConsoleApp getbyauthor --tenant-id mytenant -a github-username -s 2025-01-01 -e 2025-06-30

# Query by team
./MetricsConsoleApp getbyteam --tenant-id mytenant -t "Alpha" -s 2025-01-01 -e 2025-06-30

# Query by PR ID
./MetricsConsoleApp getbyid --tenant-id mytenant -r your-github-org/repo-1 -id 42
```

## MCP Server

The `Metrics.MCP.StreamableHTTP` project exposes a [Model Context Protocol](https://modelcontextprotocol.io/) server that allows AI agents (e.g., GitHub Copilot in VS Code) to query metrics directly in natural language.

### Running Locally

1. Build the solution.
2. Start a local Postgres instance (or use SQLite).
3. From the `configs/` directory (where `appsettings.json` lives), run:
   ```bash
   dotnet ../src/mcp/dotnet/Metrics.MCP.StreamableHTTP/bin/Debug/net10.0/Metrics.MCP.StreamableHTTP.dll
   ```
4. To debug, attach your debugger to the running `dotnet` process.

### Available MCP Tools

#### GitHub PR Metrics (`MetricsTool`)
- `GetMetricsByTeam` — PR metrics for a team within a date range
- `GetMetricsByAuthorLoginOrEmail` — PR metrics for an author (by login or email)
- `GetMetricsByAuthorName` — PR metrics for an author by display name
- `GetMetricsById` — PR metrics for a specific PR number in a repository
- `GetCopilotReviewMetricsByTeam` — Copilot review metrics for a team
- `GetCopilotReviewMetricsSummaryForAllTeams` — Cross-team Copilot review summary
- `GetTeamNames` — List all configured team names
- `GetCodeExcellenceReviewers` — List code excellence reviewer team names
- `GetProductTeams` — Full team/region/value-stream hierarchy

#### Author Metrics (`AuthorMetricsTool`)
- `GetAuthorMetricsSummaryBySprint` — Author-level metrics for a specific sprint
- `GetAuthorMetricsSummaryByMonth` — Author-level metrics for a specific month
- `GetTeamAuthorMetricsBySprint` — All authors in a team for a specific sprint
- `GetTeamAuthorMetricsByMonth` — All authors in a team for a specific month

#### Reviewer Metrics (`ReviewerMetricsTool`)
- `GetReviewerMetricsSummaryBySprint` — Reviewer metrics across all reviewers for a sprint
- `GetReviewerMetricsSummaryByMonth` — Reviewer metrics across all reviewers for a month
- `GetReviewerMetricsByReviewerForSprint` — Metrics for a specific reviewer by sprint
- `GetReviewerMetricsByReviewerForMonth` — Metrics for a specific reviewer by month
- `ExportReviewerMetricsBySprint` — Export reviewer data as CSV for a sprint
- `ExportReviewerMetricsByMonth` — Export reviewer data as CSV for a month

#### Sprint Tools (`SprintTool`)
- `GetSprintInfoByRelease` — Sprint details for a given release name
- `GetSprintInfo` — Sprint details for a given sprint number and year
- `GetAllSprintsForYear` — All sprints for a given year

#### Bot / Coding Agent Metrics (`BotMetricTool`)
- `GetMetricsAuthoredByCodingAgent` — PRs authored by an AI coding agent (e.g., GitHub Copilot coding agent)
- `GetMetricsAuthoredByDependabot` — PRs authored by Dependabot

#### Azure DevOps Tools
- `GetEpicMetricsByTeam` — Epic work item metrics for a team
- `GetEpicMetricsByReleaseVersion` — Epic metrics for a specific release version
- `GetEpicMetricsByTeamAndReleaseVersion` — Epic metrics filtered by both team and release
- `GetFeatureFlagWorkItemsForReleaseVersion` — Feature flag work items for a release

### Connecting to VS Code (GitHub Copilot)

#### OAuth authentication (default)

Add the following to your `mcp.json`:

```json
{
  "servers": {
    "devexmetrics": {
      "url": "https://your-devexmetrics-server",
      "type": "http",
      "headers": { "X-Tenant-Id": "mytenant" },
      "auth": {
        "type": "oauth",
        "clientId": "your-oauth-client-id",
        "scopes": ["openid"]
      }
    },
    "devexmetrics-local": {
      "url": "http://localhost:5000",
      "type": "http",
      "headers": { "X-Tenant-Id": "mytenant" },
      "auth": {
        "type": "oauth",
        "clientId": "your-oauth-client-id",
        "scopes": ["openid"]
      }
    }
  }
}
```

VS Code will prompt for OAuth login and handle token refresh automatically. Your identity provider must be configured in `Authentication.OktaAuthority` / `Authentication.OktaAudience` in the tenant config.

#### API key authentication

```json
{
  "servers": {
    "devexmetrics": {
      "url": "https://your-devexmetrics-server",
      "type": "http",
      "headers": {
        "x-api-key": "${input:x-api-key}",
        "X-Tenant-Id": "mytenant"
      }
    }
  },
  "inputs": [
    {
      "type": "promptString",
      "id": "x-api-key",
      "description": "DevEx Metrics API Key",
      "password": true
    }
  ]
}
```

### Background Scheduler

The `MetricsSchedulerService` is a hosted background service that automatically syncs metrics from GitHub on a configurable cron schedule (default: every 30 minutes). It is resilient to errors and logs all activity. The schedule can be customized in the app configuration.

## OData API

DevMetrics exposes OData v4 endpoints for integration with Power BI, PowerApps, Excel, and custom clients.

All endpoints require the `x-tenant-id` header (or use the tenant prefix in the URL path).

### Endpoints

| Endpoint | Description |
|---|---|
| `/{tenant}/odata/PRMetricsEx` | PR metrics with all computed fields and relationships |
| `/{tenant}/odata/Teams` | Configured teams and their statistics |
| `/{tenant}/odata/Sprints` | Sprint calendar with release names |
| `/{tenant}/odata/Contributors` | Per-PR contributor stats (LOC, commit count) |
| `/{tenant}/odata/CopilotReviewMetrics` | Per-PR Copilot review data |
| `/{tenant}/odata/CopilotReviewMetricsForAllTeams` | Cross-team Copilot summary over a date range |
| `/{tenant}/odata/ReviewerSprintMetrics` | Aggregated reviewer metrics per sprint |
| `/{tenant}/odata/ReviewerMonthlyMetrics` | Aggregated reviewer metrics per month |
| `/{tenant}/odata/AuthorMetrics/Sprint` | Author metrics for a sprint (`?author=&year=&sprintNumber=`) |
| `/{tenant}/odata/AuthorMetrics/Month` | Author metrics for a month (`?author=&year=&month=`) |
| `/{tenant}/odata/TeamAuthorMetrics/Sprint` | Team author metrics for a sprint (`?team=&year=&sprintNumber=`) |
| `/{tenant}/odata/TeamAuthorMetrics/Month` | Team author metrics for a month (`?team=&year=&month=`) |

Supports `$filter`, `$select`, `$orderby`, `$top`, `$skip`, `$expand`, `$count`.

**Example:**
```bash
curl -H "x-tenant-id: mytenant" \
  "https://your-devexmetrics-server/mytenant/odata/PRMetricsEx?\$filter=State eq 'merged'&\$expand=Team,Contributors&\$orderby=CreatedAt desc&\$top=20"
```

For detailed query examples and Power BI/Power Query setup, see [OData Documentation](./docs/OData.md).

## Authentication

Three authentication modes are supported, configured per tenant:

| Mode | Config Key | Description |
|---|---|---|
| **OAuth / OIDC** | `Authentication.OktaEnabled = true` | JWT bearer tokens from any OIDC-compatible provider (e.g., Okta) |
| **API Key** | `Authentication.ApiKey` | Header-based: `x-api-key: <key>` |
| **Basic Auth** | `Authentication.ApiKey` | HTTP Basic auth using the API key as password |

## Logging

Structured logging is configured in `appsettings.json` under the `Logging` section. The console formatter supports single-line output with timestamps and scopes. Tenant ID is injected into log scopes automatically via middleware.

See [TENANT_LOGGING_SCOPES.md](./docs/TENANT_LOGGING_SCOPES.md) for scope configuration details.

## Querying the Database Directly

You can query the PostgreSQL database directly using `psql` or any compatible SQL client:

```sh
psql "host=<your-db-host> port=5432 dbname=devmetrics user=<username>" \
  -c "SELECT * FROM information_schema.tables;"
```

If your cloud provider supports a remote data API, consult your provider's documentation for the appropriate CLI or SDK commands.

## Further Documentation

- [Architecture](./docs/ARCHITECTURE.md)
- [OData Query Reference](./docs/OData.md)
- [Tenant Environment Variables](./docs/TENANT_ENVIRONMENT_VARIABLES.md)
- [Tenant Logging Scopes](./docs/TENANT_LOGGING_SCOPES.md)
- [Database Migration Scripts](./docs/Database_Migration_Scripts.md)
