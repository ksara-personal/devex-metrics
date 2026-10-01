# DevMetrics Architecture

## Overview

DevMetrics is a multi-tenant, cross-platform .NET 10.0 solution designed to extract, analyze, and report software engineering metrics from multiple source control platforms (GitHub, Azure DevOps). The system provides flexible querying capabilities through OData endpoints, MCP (Model Context Protocol) integration, and supports multiple database backends.

## Architecture Principles

The solution is organised as a clean architecture. **Dependencies point inwards only**: hosts
depend on adapters, adapters depend on use cases, use cases depend on the domain, and the
domain depends on nothing. Anything the inner layers need from the outside world is expressed
as a *port* (an interface declared inwards) and satisfied by an *adapter* (an implementation
supplied outwards).

Alongside that:

- **Multi-Tenant**: isolated data and configuration per tenant, reached through the
  `ITenantSettingsProvider` port rather than a concrete tenant store
- **Provider Pattern**: GitHub and Azure DevOps plug in as runtime-loaded extensions
- **Database Agnostic**: SQLite and PostgreSQL with provider-specific migration assemblies
- **API First**: OData endpoints and MCP tools over the same application services
- **Background Processing**: scheduled collection driven by an infrastructure hosted service
- **Cloud Native**: OAuth 2.0, containerisation, cloud database support

### Layers

| Layer | Projects | Allowed dependencies |
|---|---|---|
| Domain | `src/Core/Metrics.Domain` | BCL only, plus the `[MultiTenant]` marker attribute |
| Application | `src/Core/Metrics.Application` | Domain and `Microsoft.Extensions.*.Abstractions` |
| Infrastructure | `src/Infrastructure/Metrics.Infrastructure`, `Metrics.GitHub`, `Metrics.ADO`, `Migrations/*` | Domain, Application, any package |
| Presentation | `src/Presentation/Metrics.MCP.StreamableHTTP`, `MetricsConsoleApp` | every layer |

Each project declares its layer through the `MetricsLayer` MSBuild property, and
`tests/Metrics.ArchitectureTests` fails the build when a project reference or package
reference breaks the rule above.

```mermaid
graph RL
    subgraph Presentation
        Host[Metrics.MCP.StreamableHTTP]
        Cli[MetricsConsoleApp]
    end
    subgraph Infrastructure
        Infra[Metrics.Infrastructure<br/>EF Core, stores, HTTP, tenancy, DI]
        GitHub[Metrics.GitHub]
        ADO[Metrics.ADO]
        Migrations[Migrations.*]
    end
    subgraph Core
        App[Metrics.Application<br/>use cases and ports]
        Domain[Metrics.Domain<br/>entities and contracts]
    end

    Host --> Infra
    Host --> GitHub
    Host --> ADO
    Cli --> Infra
    GitHub --> Infra
    ADO --> Infra
    Migrations --> Infra
    Infra --> App
    App --> Domain

    style Domain fill:#c8e6c9
    style App fill:#dcedc8
    style Infra fill:#e1f5ff
    style Host fill:#fff9c4
```

### Ports and adapters

| Port (Application) | Adapter (Infrastructure) | Purpose |
|---|---|---|
| `IMetricsPersistenceService` | `MetricsPersistenceService<TContext>`, `FilePersistenceService`, `DefaultPersistenceService` | storage-agnostic metric persistence |
| `ITenantSettingsProvider` | `TenantConfigurationService` | per-tenant configuration without knowing how tenants resolve |
| `IOrganizationApiClient` | `ApiClient` and its provider subclasses | source-control API calls used by team resolution |
| `IMetricsExtensionProvider`, `IServiceConfigurator`, `IODataConfigurator` | `GitHubExtensionProvider`, `ADOMetricsExtensionProvider` | provider registration and OData model contribution |
| `IMetricsScheduler` | `GitHubMetricsScheduler`, `ADOMetricsScheduler` | scheduled collection per provider |

Two contracts deliberately stay in the infrastructure layer because they expose Entity
Framework types: `IDbContextMetricsPersistenceService`, for callers that compose queries
directly against the EF model, and `IDataMigration<TContext>`, for data migrations.

### Accepted compromises

- Domain entities carry persistence annotations (`[Table]`, `[Column]`, `[MultiTenant]`)
  rather than separate `IEntityTypeConfiguration` classes. Moving the mapping out would risk
  silently changing the generated schema, so the annotations stay and the domain project
  references no database provider.
- `IODataConfigurator` lives in the application layer and pulls in
  `Microsoft.OData.ModelBuilder` (the EDM builder, not ASP.NET Core), because provider
  extensions contribute to the OData model and the host is the only thing that hosts it.

## Component Architecture

### 1. Core Components

#### Metrics.Application (use cases and ports)
- **Purpose**: Orchestrates the work the system does, and declares the ports it needs
- **Key Responsibilities**:
  - Data synchronization orchestration
  - Metrics calculation and analysis
  - Sprint calendar management
  - User membership and team resolution
  - Port definitions for persistence, tenant settings and source-control APIs

**Key Classes**:
- `DataSyncService`: Orchestrates data synchronization across providers
- `DataSynchronizer`: Base synchronization logic
- `MetricProvider`: Factory for creating metric providers
- `PRAnalyzer`: Pull request analysis and metrics calculation
- `ConfigService`: Configuration management
- `SprintCalendar`: Sprint/release date calculations

#### Metrics.Domain
- **Purpose**: Entities, settings records and domain contracts, free of infrastructure
- **Key Entities**:
  - `PRMetricsEx`: Extended PR metrics model
  - `DevExMetric`: Developer experience metrics
  - `Team`: Team information
  - `AuthorMetric`: Author productivity metrics
  - `ReviewerMetric`: Reviewer performance metrics
  - `CopilotReviewMetric`: GitHub Copilot review statistics
  - `Sprint`: Sprint/release information

```mermaid
classDiagram
    class PRMetricsEx {
        +string Id
        +int PrNumber
        +string Author
        +string Repository
        +DateTime CreatedAt
        +DateTime? MergedAt
        +string State
        +int TotalLines
        +double CycleTime
        +double LeadTime
        +Team Team
        +List~ReviewerMetric~ ReviewerMetrics
        +CopilotReviewMetric CopilotReviewMetrics
    }

    class Team {
        +string Name
        +string Region
        +string ValueStream
        +List~PRMetricsEx~ Metrics
    }

    class ReviewerMetric {
        +string Reviewer
        +int CommentCount
        +DateTime ReviewedAt
        +string State
    }

    class CopilotReviewMetric {
        +int FilesReviewed
        +int FilesChanged
        +int Comments
    }

    class AuthorMetric {
        +string Author
        +int PrsAuthored
        +int PrsClosed
        +double AvgPrSize
        +double AvgCycleTime
        +double AvgReviewComments
    }

    PRMetricsEx "1" --> "1" Team
    PRMetricsEx "1" --> "*" ReviewerMetric
    PRMetricsEx "1" --> "0..1" CopilotReviewMetric
```

#### Metrics.Infrastructure (adapters)
- **Purpose**: Implements the application ports and owns every framework dependency
- **Key Responsibilities**:
  - EF Core contexts and persistence services (`Persistence/`)
  - File and API backed stores (`Persistence/`)
  - HTTP client base and retry policies (`Http/`)
  - Tenant resolution and configuration layering (`MultiTenant/`)
  - Background scheduling (`Scheduling/`)
  - DI composition root, `AddDIServices` (`DependencyInjection/`)

### 2. Provider Pattern

The system uses an extensible provider pattern to support multiple source control systems.

```mermaid
graph TD
    subgraph "Provider Pattern"
        IProvider[IMetricsExtensionProvider<br/>Extension Interface]
        IService[IServiceConfigurator]
        IOData[IODataConfigurator]
        IScheduler[IMetricsScheduler]
        
        IProvider --> IService
        IProvider --> IOData
        
        GitHubProvider[GitHubExtensionProvider]
        ADOProvider[ADOMetricsExtensionProvider]
        
        GitHubProvider -.implements.-> IProvider
        ADOProvider -.implements.-> IProvider
        
        GitHubScheduler[GitHubMetricsScheduler]
        ADOScheduler[ADOMetricsScheduler]
        
        GitHubScheduler -.implements.-> IScheduler
        ADOScheduler -.implements.-> IScheduler
    end
    
    subgraph "GitHub Implementation"
        GitHubAPI[GitHubApiClient<br/>GraphQL Client]
        GitHubSync[GitHubPRMetricsSynchronizer]
        GitHubReviewerSync[GitHubReviewerMetricsSynchronizer]
        
        GitHubProvider --> GitHubScheduler
        GitHubProvider --> GitHubAPI
        GitHubScheduler --> GitHubSync
        GitHubSync --> GitHubReviewerSync
    end
    
    subgraph "ADO Implementation"
        ADOAPI[ADOApiClient<br/>REST Client]
        ADOSync[ADOMetricsSynchronizer]
        WorkItemClient[WorkItemClient]
        
        ADOProvider --> ADOScheduler
        ADOProvider --> ADOAPI
        ADOScheduler --> ADOSync
        ADOSync --> WorkItemClient
    end
    
    style IProvider fill:#ffeb3b
    style GitHubProvider fill:#81c784
    style ADOProvider fill:#81c784
```

#### Metrics.GitHub
- **Purpose**: GitHub-specific implementation
- **Features**:
  - GraphQL API integration
  - PR metrics synchronization
  - Reviewer metrics tracking
  - Copilot review analytics
  - Team membership resolution
  - Commit-level analysis

**Key Classes**:
- `GitHubExtensionProvider`: Provider registration and configuration
- `GitHubApiClient`: GraphQL API client
- `GitHubPRMetricsSynchronizer`: PR data synchronization
- `GitHubReviewerMetricsSynchronizer`: Reviewer data synchronization
- `GitHubMetricsScheduler`: Scheduled data collection

#### Metrics.ADO (Azure DevOps)
- **Purpose**: Azure DevOps-specific implementation
- **Features**:
  - REST API integration
  - Work item tracking
  - Pull request metrics
  - Team project management
  - Azure DevOps work item linking

**Key Classes**:
- `ADOMetricsExtensionProvider`: Provider registration
- `ADOApiClient`: REST API client
- `ADOMetricsSynchronizer`: Data synchronization
- `WorkItemClient`: Work item operations
- `ADOMetricsScheduler`: Scheduled collection

### 3. API Layer (Metrics.MCP.StreamableHTTP)

The API layer provides multiple access methods to metrics data.

```mermaid
graph LR
    subgraph "API Surface"
        MCP[MCP Server<br/>Model Context Protocol]
        OData[OData v4 Endpoints]
        Controllers[ASP.NET Controllers]
    end
    
    subgraph "Authentication"
        OAuth[OAuth 2.0<br/>Okta]
        ApiKey[API Key<br/>Header Auth]
        Basic[Basic Auth]
    end
    
    subgraph "MCP Tools"
        MetricsTool[Metrics Tool<br/>PR Queries]
        AuthorTool[Author Metrics Tool<br/>Author Stats]
        ReviewerTool[Reviewer Metrics Tool<br/>Reviewer Stats]
        SprintTool[Sprint Tool<br/>Sprint Info]
    end
    
    subgraph "OData Entities"
        PRMetrics[PRMetricsEx]
        Teams[Teams]
        Sprints[Sprints]
        Authors[Author Metrics]
        Reviewers[Reviewer Metrics]
        Copilot[Copilot Metrics]
    end
    
    MCP --> OAuth
    MCP --> ApiKey
    OData --> Basic
    
    OAuth --> MCP
    ApiKey --> MCP
    
    MCP --> MetricsTool
    MCP --> AuthorTool
    MCP --> ReviewerTool
    MCP --> SprintTool
    
    OData --> PRMetrics
    OData --> Teams
    OData --> Sprints
    OData --> Authors
    OData --> Reviewers
    OData --> Copilot
    
    Controllers --> OData
    
    style MCP fill:#e1f5ff
    style OData fill:#e8f5e9
    style OAuth fill:#fff9c4
```

**Key Features**:
- **MCP Tools**: VS Code Copilot integration
  - Query PR metrics by various criteria
  - Author productivity analysis
  - Reviewer performance tracking
  - Sprint information lookup

- **OData Endpoints**: `/{tenant}/odata/*`
  - Full OData query support ($filter, $select, $orderby, $expand)
  - Power BI and Excel integration
  - RESTful API access
  - Tenant-specific routing

- **Authentication**:
  - OAuth 2.0 with Okta integration
  - API Key authentication
  - Basic authentication for tools
  - Multi-tenant header (`x-tenant-id`)

### 4. Multi-Tenant Architecture

```mermaid
graph TB
    subgraph "Request Flow"
        Request[HTTP Request<br/>x-tenant-id: tenant-1]
    end
    
    subgraph "Tenant Resolution"
        Middleware[Multi-Tenant Middleware]
        TenantStore[Tenant Configuration Store]
    end
    
    subgraph "Tenant Context"
        TenantInfo[Tenant Info<br/>Name, Id, Config]
        TenantConfig[Tenant Configuration<br/>GitHub Orgs, Repos, Teams]
    end
    
    subgraph "Data Isolation"
        TenantDB[Tenant-Specific DbContext]
        Schema[Schema/Table Isolation]
    end
    
    subgraph "Configuration"
        Tenant1Config[appsettings.tenant-1.json<br/>Tenant 1 Config]
        Tenant2Config[appsettings.tenant-2.json<br/>Tenant 2 Config]
        BaseConfig[appsettings.json<br/>Base Configuration]
    end
    
    Request --> Middleware
    Middleware --> TenantStore
    TenantStore --> TenantInfo
    TenantInfo --> TenantConfig
    TenantConfig --> TenantDB
    TenantDB --> Schema
    
    BaseConfig --> TenantStore
    Tenant1Config --> TenantStore
    Tenant2Config --> TenantStore
    
    style Request fill:#ffcdd2
    style TenantInfo fill:#c8e6c9
    style TenantDB fill:#b3e5fc
```

**Tenant Isolation**:
- Header-based tenant identification (`x-tenant-id`)
- Tenant-specific configuration (GitHub orgs, repos, teams)
- Isolated data storage per tenant
- Configurable via `appsettings.{tenant}.json`

### 5. Data Flow

```mermaid
sequenceDiagram
    participant Scheduler as Background Scheduler
    participant Sync as Data Synchronizer
    participant Provider as GitHub/ADO Provider
    participant API as External API
    participant Analyzer as PR Analyzer
    participant EF as Entity Framework
    participant DB as Database
    
    Scheduler->>Sync: Trigger Collection (Cron)
    Sync->>Provider: Get Latest PRs
    Provider->>API: Query PRs (since last run)
    API-->>Provider: PR Data
    Provider->>Analyzer: Analyze PR Metrics
    Analyzer-->>Provider: Calculated Metrics
    Provider-->>Sync: PR Metrics
    Sync->>EF: Save Metrics
    EF->>DB: Persist Data
    DB-->>EF: Success
    EF-->>Sync: Saved
    Sync->>Sync: Update Run Status
    
    Note over Scheduler,DB: Scheduled every 30 minutes (configurable)
```

**Data Collection Process**:
1. **Scheduler Trigger**: Cron-based background service triggers collection
2. **Provider Query**: Query external API for new/updated PRs since last run
3. **Analysis**: Calculate metrics (cycle time, lead time, maturity, etc.)
4. **Persistence**: Save to database via EF Core
5. **Status Tracking**: Record run status for incremental updates

### 6. Database Architecture

```mermaid
graph TB
    subgraph "Migration Projects"
        GitHubSQLite[Metrics.GitHub.Migrations.Sqlite]
        GitHubPostgres[Metrics.GitHub.Migrations.Postgres]
        ADOSQLite[Metrics.ADO.Migrations.Sqlite]
        ADOPostgres[Metrics.ADO.Migrations.Postgres]
    end
    
    subgraph "DbContext"
        GitHubContext[GitHub DbContext]
        ADOContext[ADO DbContext]
    end
    
    subgraph "Databases"
        SQLite[(SQLite<br/>Local Dev)]
        Postgres[(PostgreSQL<br/>Production)]
        CloudDB[(PostgreSQL<br/>Cloud Hosted)]
    end
    
    GitHubSQLite --> GitHubContext
    GitHubPostgres --> GitHubContext
    ADOSQLite --> ADOContext
    ADOPostgres --> ADOContext
    
    GitHubContext --> SQLite
    GitHubContext --> Postgres
    ADOContext --> SQLite
    ADOContext --> Postgres
    
    Postgres --> CloudDB
    
    style GitHubContext fill:#fff3e0
    style ADOContext fill:#fff3e0
    style SQLite fill:#e1f5ff
    style Postgres fill:#c5e1a5
    style CloudDB fill:#c5e1a5
```

**Database Strategy**:
- **Provider-Specific**: Separate DbContext per provider (GitHub, ADO)
- **Multi-Database**: SQLite for development, PostgreSQL for production
- **Separate Migrations**: Independent migration projects per provider and database
- **Cloud Integration**: Cloud-hosted PostgreSQL with remote Data API support

**Key Tables**:
- `PRMetrics`: Pull request metrics
- `Teams`: Team configuration
- `Contributors`: Contributor statistics
- `ReviewerMetrics`: Reviewer performance
- `Sprints`: Sprint calendar
- `CopilotReviewMetrics`: Copilot review statistics
- `RunStatus`: Synchronization tracking

### 7. Background Scheduler

```mermaid
graph LR
    subgraph "Scheduler Service"
        Hosted[IHostedService<br/>MetricsSchedulerService]
        Cron[Cron Expression<br/>Every 30 Minutes]
        Config[Configuration<br/>Schedule Settings]
    end
    
    subgraph "Execution"
        Trigger[Trigger Event]
        Provider[Provider Loop]
        Collect[Collect Metrics]
        Store[Store Data]
        Log[Log Status]
    end
    
    subgraph "Error Handling"
        Retry[Retry Logic]
        ErrorLog[Error Logging]
        Resilience[Resilient Execution]
    end
    
    Hosted --> Cron
    Config --> Hosted
    
    Cron --> Trigger
    Trigger --> Provider
    Provider --> Collect
    Collect --> Store
    Store --> Log
    
    Collect -.error.-> Retry
    Retry -.failed.-> ErrorLog
    Retry -.success.-> Store
    
    Hosted --> Resilience
    
    style Hosted fill:#fff9c4
    style Collect fill:#c8e6c9
    style ErrorLog fill:#ffcdd2
```

**Features**:
- ASP.NET Core hosted background service
- Configurable cron schedule (default: every 30 minutes)
- Incremental updates (only new/changed data)
- Error resilience and logging
- Run status tracking for auditing

## Technology Stack

### Core Framework
- **.NET 10.0**: Latest LTS version with AOT support
- **C# 13**: Modern language features
- **ASP.NET Core**: Web API and hosting

### Data Access
- **Entity Framework Core**: ORM with migrations
- **SQLite**: Local development database
- **PostgreSQL**: Production database
- **Npgsql**: PostgreSQL provider

### API & Integration
- **OData v4**: Flexible query capabilities
- **Model Context Protocol (MCP)**: VS Code Copilot integration
- **GraphQL**: GitHub API integration
- **REST**: Azure DevOps API integration

### Authentication & Security
- **OAuth 2.0**: OpenID Connect with Okta
- **API Key**: Header-based authentication
- **JWT**: Token validation
- **Multi-Tenant**: Finbuckle.MultiTenant

### Cloud & DevOps
- **Docker**: Containerization
- **Infrastructure as Code**: e.g., CDK, Terraform, or Pulumi
- **Container Orchestration**: e.g., Fargate, Kubernetes, or Azure Container Apps
- **Cloud Database**: PostgreSQL (hosted on your cloud provider of choice)
- **Secrets Management**: e.g., cloud-native secrets manager or Kubernetes Secrets

### Build & Deployment
- **GitHub Actions**: CI/CD pipelines
- **dotnet CLI**: Build and publish
- **EF Core Tools**: Migration management

## Deployment Architecture

```mermaid
graph TB
    subgraph "Cloud Hosting"
        subgraph "Container Cluster"
            Container1[Container Instance 1<br/>Metrics.MCP.StreamableHTTP]
            Container2[Container Instance 2<br/>Metrics.MCP.StreamableHTTP]
        end
        
        subgraph "Data Tier"
            DB[(PostgreSQL<br/>Multi-AZ)]
            Secrets[Secrets Manager<br/>API Keys, Credentials]
        end
        
        subgraph "Networking"
            ALB[Application Load Balancer]
            API_GW[API Gateway<br/>Optional]
        end
        
        subgraph "Monitoring"
            Logs[Centralized Logs]
            Metrics_Mon[Cloud Metrics]
        end
    end
    
    subgraph "External"
        GitHub_API[GitHub API]
        ADO_API[Azure DevOps API]
        IdP[Identity Provider<br/>OAuth]
    end
    
    ALB --> Container1
    ALB --> Container2
    
    Container1 --> DB
    Container2 --> DB
    
    Container1 --> Secrets
    Container2 --> Secrets
    
    Container1 --> GitHub_API
    Container2 --> GitHub_API
    Container1 --> ADO_API
    Container2 --> ADO_API
    
    Container1 --> IdP
    Container2 --> IdP
    
    Container1 --> Logs
    Container2 --> Logs
    Container1 --> Metrics_Mon
    Container2 --> Metrics_Mon
    
    style Container1 fill:#e1f5ff
    style Container2 fill:#e1f5ff
    style DB fill:#c5e1a5
    style ALB fill:#fff9c4
```

## Configuration Management

### Environment Variables

**GitHub Authentication**:
```bash
{tenant}__GitHub__PAT="ghp_token"
```

**Database Connection**:
```bash
ConnectionStrings__SQLite="Data source=/path/to/devmetrics.db"
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=devmetrics;Username=user;Password=pwd"
```

**Product Selection**:
```bash
PRODUCT="tenant-1"  # or "tenant-2"
```

### Configuration Files

- `appsettings.json`: Base configuration
- `appsettings.tenant-1.json`: Tenant 1 overrides
- `appsettings.tenant-2.json`: Tenant 2 overrides
- `appsettings.kestrel.json`: Kestrel web server configuration

## Extension Points

### Adding a New Provider

1. **Create Provider Project**: `src/Infrastructure/Metrics.NewProvider`
2. **Implement Interface**: `IMetricsExtensionProvider` (declared in `Metrics.Application`)
3. **Create API Client**: Provider-specific API integration
4. **Implement Synchronizer**: Data collection logic
5. **Add Scheduler**: Background collection service
6. **Register Services**: `IServiceConfigurator` implementation
7. **Configure OData**: `IODataConfigurator` implementation
8. **Create Migrations**: Provider-specific database projects

### Adding New Metrics

1. **Update Domain**: Add properties to `PRMetricsEx` in `Metrics.Domain`, or add a new entity
2. **Update Analyzer**: Add calculation logic in `PRAnalyzer`
3. **Update Migrations**: Generate and apply database migrations
4. **Update OData**: Configure EDM model if needed
5. **Update MCP Tools**: Add query capabilities if needed

## Security Considerations

### Authentication
- OAuth 2.0 with PKCE flow for VS Code integration
- API Key rotation via secrets management
- JWT token validation with your configured identity provider
- Header-based tenant validation

### Authorization
- Tenant-based data isolation
- Role-based access control (future)
- API rate limiting (future)

### Data Protection
- Encrypted connections (TLS)
- Secrets stored in a secrets manager (never in code or config files)
- Environment variable configuration
- No credentials in code or config files

## Performance Optimization

### Caching
- Resource caching in GitHub provider
- Team membership caching
- Sprint calendar caching

### Database
- Indexed queries on key fields
- Batch operations for bulk inserts
- Pagination in OData endpoints
- Connection pooling

### API Efficiency
- GraphQL for precise data fetching (GitHub)
- Incremental synchronization
- Parallel processing where applicable
- Efficient serialization

## Monitoring & Observability

### Logging
- Structured logging with Serilog
- Cloud logging integration (e.g., CloudWatch, Azure Monitor, Datadog)
- Log levels: Debug, Information, Warning, Error
- Correlation IDs for request tracking

### Metrics
- Cloud metrics integration
- Run status tracking
- API performance metrics
- Error rates and trends

### Health Checks
- Database connectivity
- External API availability
- Background service status

## Future Enhancements

### Planned Features
- Real-time webhooks for instant updates
- Advanced analytics and ML insights
- Custom dashboard builder
- Team comparison and benchmarking

## References

- [OData Documentation](./OData.md)
- [Migration Scripts](../scripts/README.md)
- [Main README](../README.md)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant)
