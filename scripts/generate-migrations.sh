#!/bin/bash

# Script to generate EF Core migrations for different database providers and contexts
# Usage: ./scripts/generate-migrations.sh <provider> <migration-name>
# Example: ./scripts/generate-migrations.sh sqlite AddNewColumn

set -e

PROVIDER=$1
MIGRATION_NAME=$2
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../src/Infrastructure"

if [ -z "$PROVIDER" ] || [ -z "$MIGRATION_NAME" ]; then
    echo "Usage: $0 <provider> <migration-name>"
    echo ""
    echo "Providers:"
    echo "  github-sqlite   - Generate GitHub SQLite migration"
    echo "  github-postgres - Generate GitHub PostgreSQL migration"
    echo "  ado-sqlite      - Generate ADO SQLite migration"
    echo "  ado-postgres    - Generate ADO PostgreSQL migration"
    echo "  github          - Generate migrations for GitHub (both providers)"
    echo "  ado             - Generate migrations for ADO (both providers)"
    echo "  all             - Generate all migrations (GitHub + ADO, both providers)"
    echo ""
    echo "Example: $0 github-sqlite AddNewColumn"
    echo "Example: $0 all AddNewColumn"
    exit 1
fi

# Navigate to Metrics project directory

generate_github_sqlite_migration() {
    echo "📦 Generating GitHub SQLite migration: $1"
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Sqlite"
    echo "   Building project..."
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef migrations add "$1"
    echo "✅ GitHub SQLite migration generated"
}

generate_ado_sqlite_migration() {
    echo "📦 Generating ADO SQLite migration: $1"
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Sqlite"
    echo "   Building project..."
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef migrations add "$1"
    echo "✅ ADO SQLite migration generated"
}

generate_github_postgres_migration() {
    echo "🐘 Generating GitHub PostgreSQL migration: $1"
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Postgres"
    echo "   Building project..."
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef migrations add "$1" 
    echo "✅ GitHub PostgreSQL migration generated"
}

generate_ado_postgres_migration() {
    echo "🐘 Generating ADO PostgreSQL migration: $1"
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Postgres"
    echo "   Building project..."
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef migrations add "$1"
    echo "✅ ADO PostgreSQL migration generated"
}

case "$PROVIDER" in
    github-sqlite)
        generate_github_sqlite_migration "$MIGRATION_NAME"
        ;;
    github-postgres|github-postgresql)
        generate_github_postgres_migration "$MIGRATION_NAME"
        ;;
    ado-sqlite)
        generate_ado_sqlite_migration "$MIGRATION_NAME"
        ;;
    ado-postgres|ado-postgresql)
        generate_ado_postgres_migration "$MIGRATION_NAME"
        ;;
    github)
        generate_github_sqlite_migration "$MIGRATION_NAME"
        echo ""
        generate_github_postgres_migration "$MIGRATION_NAME"
        echo ""
        echo "🎉 GitHub migrations generated for both providers!"
        ;;
    ado)
        generate_ado_sqlite_migration "$MIGRATION_NAME"
        echo ""
        generate_ado_postgres_migration "$MIGRATION_NAME"
        echo ""
        echo "🎉 ADO migrations generated for both providers!"
        ;;
    all|both)
        echo "════════════════════════════════════════════════════════"
        echo "🚀 Generating migrations for GitHub and ADO (all providers)"
        echo "════════════════════════════════════════════════════════"
        echo ""
        generate_github_sqlite_migration "$MIGRATION_NAME"
        echo ""
        generate_github_postgres_migration "$MIGRATION_NAME"
        echo ""
        generate_ado_sqlite_migration "$MIGRATION_NAME"
        echo ""
        generate_ado_postgres_migration "$MIGRATION_NAME"
        echo ""
        echo "════════════════════════════════════════════════════════"
        echo "🎉 All migrations generated successfully!"
        echo "════════════════════════════════════════════════════════"
        ;;
    *)
        echo "❌ Error: Unknown provider '$PROVIDER'"
        echo "Valid providers: github-sqlite, github-postgres, ado-sqlite, ado-postgres, github, ado, all"
        exit 1
        ;;
esac

echo ""
echo "💡 To apply migrations:"
echo "   For GitHub: export DataStoreType=<SQLite|Postgres> && dotnet ef database update --project src/Infrastructure/Metrics.Infrastructure"
echo "   For ADO:    export DataStoreType=<SQLite|Postgres> && dotnet ef database update --project src/Infrastructure/Metrics.ADO"
