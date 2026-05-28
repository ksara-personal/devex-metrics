#!/bin/bash

# Script to apply EF Core migrations for different database providers and contexts
# Usage: ./scripts/apply-migrations.sh <provider>
# Example: ./scripts/apply-migrations.sh postgres

set -e

PROVIDER=$1
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../src"

if [ -z "$PROVIDER" ]; then
    echo "Usage: $0 <provider>"
    echo ""
    echo "Providers:"
    echo "  github-sqlite   - Apply GitHub SQLite migrations"
    echo "  github-postgres - Apply GitHub PostgreSQL migrations"
    echo "  ado-sqlite      - Apply ADO SQLite migrations"
    echo "  ado-postgres    - Apply ADO PostgreSQL migrations"
    echo "  github          - Apply GitHub migrations (both providers)"
    echo "  ado             - Apply ADO migrations (both providers)"
    echo "  sqlite          - Apply all SQLite migrations (GitHub + ADO)"
    echo "  postgres        - Apply all PostgreSQL migrations (GitHub + ADO)"
    echo "  all             - Apply all migrations (GitHub + ADO, both providers)"
    echo ""
    echo "Example: $0 postgres"
    echo "Example: $0 all"
    exit 1
fi

apply_github_sqlite() {
    echo "📦 Applying GitHub SQLite migrations..."
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Sqlite"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef database update
    echo "✅ GitHub SQLite migrations applied"
}

apply_github_postgres() {
    echo "🐘 Applying GitHub PostgreSQL migrations..."
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Postgres"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef database update
    echo "✅ GitHub PostgreSQL migrations applied"
}

apply_ado_sqlite() {
    echo "📦 Applying ADO SQLite migrations..."
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Sqlite"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef database update
    echo "✅ ADO SQLite migrations applied"
}

apply_ado_postgres() {
    echo "🐘 Applying ADO PostgreSQL migrations..."
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Postgres"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef database update
    echo "✅ ADO PostgreSQL migrations applied"
}

case "$PROVIDER" in
    github-sqlite)
        apply_github_sqlite
        ;;
    github-postgres|github-postgresql)
        apply_github_postgres
        ;;
    ado-sqlite)
        apply_ado_sqlite
        ;;
    ado-postgres|ado-postgresql)
        apply_ado_postgres
        ;;
    github)
        apply_github_sqlite
        echo ""
        apply_github_postgres
        echo ""
        echo "🎉 GitHub migrations applied for both providers!"
        ;;
    ado)
        apply_ado_sqlite
        echo ""
        apply_ado_postgres
        echo ""
        echo "🎉 ADO migrations applied for both providers!"
        ;;
    sqlite)
        apply_github_sqlite
        echo ""
        apply_ado_sqlite
        echo ""
        echo "🎉 All SQLite migrations applied!"
        ;;
    postgres|postgresql)
        apply_github_postgres
        echo ""
        apply_ado_postgres
        echo ""
        echo "🎉 All PostgreSQL migrations applied!"
        ;;
    all|both)
        echo "════════════════════════════════════════════════════════"
        echo "🚀 Applying all migrations (GitHub + ADO, both providers)"
        echo "════════════════════════════════════════════════════════"
        echo ""
        apply_github_sqlite
        echo ""
        apply_github_postgres
        echo ""
        apply_ado_sqlite
        echo ""
        apply_ado_postgres
        echo ""
        echo "════════════════════════════════════════════════════════"
        echo "🎉 All migrations applied successfully!"
        echo "════════════════════════════════════════════════════════"
        ;;
    *)
        echo "❌ Error: Unknown provider '$PROVIDER'"
        echo "Valid providers: github-sqlite, github-postgres, ado-sqlite, ado-postgres, github, ado, sqlite, postgres, all"
        exit 1
        ;;
esac

echo ""
echo "💡 To verify, check your database or run:"
echo "   dotnet ef migrations list --context DevExMetricDbContext"
