# Database Migration Scripts

This directory contains scripts for managing Entity Framework Core migrations across different database providers and contexts.

## Available Scripts

### 1. generate-migrations.sh

Generates new EF Core migrations for different database providers and contexts.

**Usage:**
```bash
./scripts/generate-migrations.sh <provider> <migration-name>
```

**Supported Providers:**
- `github-sqlite` - Generate GitHub SQLite migration
- `github-postgres` - Generate GitHub PostgreSQL migration
- `ado-sqlite` - Generate ADO SQLite migration
- `ado-postgres` - Generate ADO PostgreSQL migration
- `github` - Generate migrations for GitHub (both SQLite and Postgres)
- `ado` - Generate migrations for ADO (both SQLite and Postgres)
- `all` - Generate all migrations (GitHub + ADO, both providers)

**Examples:**
```bash
# Generate a single migration for GitHub SQLite
./scripts/generate-migrations.sh github-sqlite AddNewColumn

# Generate migrations for all GitHub providers
./scripts/generate-migrations.sh github AddUserTable

# Generate migrations for all providers
./scripts/generate-migrations.sh all UpdateSchema
```

### 2. apply-migrations.sh

Applies EF Core migrations to the database for different providers and contexts.

**Usage:**
```bash
./scripts/apply-migrations.sh <provider>
```

**Supported Providers:**
- `github-sqlite` - Apply GitHub SQLite migrations
- `github-postgres` - Apply GitHub PostgreSQL migrations
- `ado-sqlite` - Apply ADO SQLite migrations
- `ado-postgres` - Apply ADO PostgreSQL migrations
- `github` - Apply GitHub migrations (both providers)
- `ado` - Apply ADO migrations (both providers)
- `sqlite` - Apply all SQLite migrations (GitHub + ADO)
- `postgres` - Apply all PostgreSQL migrations (GitHub + ADO)
- `all` - Apply all migrations (GitHub + ADO, both providers)

**Examples:**
```bash
# Apply PostgreSQL migrations for all contexts
./scripts/apply-migrations.sh postgres

# Apply only GitHub SQLite migrations
./scripts/apply-migrations.sh github-sqlite

# Apply all migrations
./scripts/apply-migrations.sh all
```

**Note:** Ensure your database connection strings are properly configured in `appsettings.json` or via environment variables before applying migrations.

### 3. remove-migrations.sh

Removes the last EF Core migration for different database providers and contexts. Useful for rolling back recent changes during development.

**Usage:**
```bash
./scripts/remove-migrations.sh <provider>
```

**Supported Providers:**
- `github-sqlite` - Remove last GitHub SQLite migration
- `github-postgres` - Remove last GitHub PostgreSQL migration
- `ado-sqlite` - Remove last ADO SQLite migration
- `ado-postgres` - Remove last ADO PostgreSQL migration
- `github` - Remove last GitHub migrations (both providers)
- `ado` - Remove last ADO migrations (both providers)
- `sqlite` - Remove last SQLite migrations (GitHub + ADO)
- `postgres` - Remove last PostgreSQL migrations (GitHub + ADO)
- `all` - Remove last migrations (GitHub + ADO, both providers)

**Examples:**
```bash
# Remove last PostgreSQL migration for all contexts
./scripts/remove-migrations.sh postgres

# Remove last GitHub migrations only
./scripts/remove-migrations.sh github

# Remove all last migrations
./scripts/remove-migrations.sh all
```

**Warning:** This operation is destructive and will remove migration files and update the model snapshot. Use with caution.

## Prerequisites

- .NET 10.0 SDK or later
- EF Core tools installed: `dotnet tool install --global dotnet-ef`
- Proper database connection strings configured
- For PostgreSQL: A running PostgreSQL instance (local or Docker)

## Environment Variables

The scripts use the following environment variables:
- `DataStoreType` - Set automatically by scripts to `SQLite` or `Postgres`
- Database connection strings from `appsettings.json` or environment:
  - `ConnectionStrings__SQLite`
  - `ConnectionStrings__Postgres`

## Common Workflows

### Creating a New Feature with Database Changes

1. Make model changes in your code
2. Generate migrations:
   ```bash
   ./scripts/generate-migrations.sh all AddFeatureXYZ
   ```
3. Review generated migration files
4. Apply migrations to your database:
   ```bash
   ./scripts/apply-migrations.sh all
   ```

### Rolling Back a Migration

```bash
# Remove the last migration
./scripts/remove-migrations.sh all

# Make corrections to your model
# Generate a new corrected migration
./scripts/generate-migrations.sh all FixedMigration
```

### Setting Up a New Database

```bash
# Apply all existing migrations to a fresh database
./scripts/apply-migrations.sh all
```

## Project Structure

The migration projects are organized as:
```
src/
  Migrations/
    Metrics.GitHub.Migrations.Sqlite/
    Metrics.GitHub.Migrations.Postgres/
    Metrics.ADO.Migrations.Sqlite/
    Metrics.ADO.Migrations.Postgres/
```

Each project contains:
- DbContext configuration for its specific provider
- Migration files (timestamped)
- Model snapshot

## Troubleshooting

### Script Permission Denied
```bash
chmod +x scripts/*.sh
```

### Build Errors
Ensure all projects build successfully:
```bash
dotnet build src/Metrics.sln
```

### Connection Issues
Verify your connection strings are correct:
```bash
# For PostgreSQL
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=devmetrics;Username=postgres;Password=yourpassword"

# For SQLite
export ConnectionStrings__SQLite="Data Source=/path/to/devmetrics.db"
```

### Migration Conflicts
If you encounter migration conflicts:
1. Remove the problematic migration: `./scripts/remove-migrations.sh <provider>`
2. Pull latest changes from your team
3. Regenerate the migration: `./scripts/generate-migrations.sh <provider> MigrationName`
