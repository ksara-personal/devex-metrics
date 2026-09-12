#!/bin/bash

# Script to remove the last EF Core migration for different database providers and contexts
# Usage: ./scripts/remove-migrations.sh <provider>
# Example: ./scripts/remove-migrations.sh postgres

set -e

PROVIDER=$1
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../src/Infrastructure"

if [ -z "$PROVIDER" ]; then
    echo "Usage: $0 <provider>"
    echo ""
    echo "Providers:"
    echo "  github-sqlite   - Remove last GitHub SQLite migration"
    echo "  github-postgres - Remove last GitHub PostgreSQL migration"
    echo "  ado-sqlite      - Remove last ADO SQLite migration"
    echo "  ado-postgres    - Remove last ADO PostgreSQL migration"
    echo "  github          - Remove last GitHub migrations (both providers)"
    echo "  ado             - Remove last ADO migrations (both providers)"
    echo "  sqlite          - Remove last SQLite migrations (GitHub + ADO)"
    echo "  postgres        - Remove last PostgreSQL migrations (GitHub + ADO)"
    echo "  all             - Remove last migrations (GitHub + ADO, both providers)"
    echo ""
    echo "Example: $0 postgres"
    echo "Example: $0 all"
    exit 1
fi

remove_github_sqlite() {
    echo "📦 Removing last GitHub SQLite migration..."
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Sqlite"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef migrations remove --force
    echo "✅ GitHub SQLite migration removed"
}

remove_github_postgres() {
    echo "🐘 Removing last GitHub PostgreSQL migration..."
    cd "$SRC_DIR/Migrations/Metrics.GitHub.Migrations.Postgres"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef migrations remove --force
    echo "✅ GitHub PostgreSQL migration removed"
}

remove_ado_sqlite() {
    echo "📦 Removing last ADO SQLite migration..."
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Sqlite"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=SQLite
    dotnet ef migrations remove --force
    echo "✅ ADO SQLite migration removed"
}

remove_ado_postgres() {
    echo "🐘 Removing last ADO PostgreSQL migration..."
    cd "$SRC_DIR/Migrations/Metrics.ADO.Migrations.Postgres"
    dotnet build --configuration Release > /dev/null 2>&1 || true
    export DataStoreType=Postgres
    dotnet ef migrations remove --force
    echo "✅ ADO PostgreSQL migration removed"
}

case "$PROVIDER" in
    github-sqlite)
        remove_github_sqlite
        ;;
    github-postgres|github-postgresql)
        remove_github_postgres
        ;;
    ado-sqlite)
        remove_ado_sqlite
        ;;
    ado-postgres|ado-postgresql)
        remove_ado_postgres
        ;;
    github)
        remove_github_sqlite
        echo ""
        remove_github_postgres
        echo ""
        echo "🎉 GitHub migrations removed for both providers!"
        ;;
    ado)
        remove_ado_sqlite
        echo ""
        remove_ado_postgres
        echo ""
        echo "🎉 ADO migrations removed for both providers!"
        ;;
    sqlite)
        remove_github_sqlite
        echo ""
        remove_ado_sqlite
        echo ""
        echo "🎉 All SQLite migrations removed!"
        ;;
    postgres|postgresql)
        remove_github_postgres
        echo ""
        remove_ado_postgres
        echo ""
        echo "🎉 All PostgreSQL migrations removed!"
        ;;
    all|both)
        echo "════════════════════════════════════════════════════════"
        echo "🚀 Removing last migrations (GitHub + ADO, both providers)"
        echo "════════════════════════════════════════════════════════"
        echo ""
        remove_github_sqlite
        echo ""
        remove_github_postgres
        echo ""
        remove_ado_sqlite
        echo ""
        remove_ado_postgres
        echo ""
        echo "════════════════════════════════════════════════════════"
        echo "🎉 All migrations removed successfully!"
        echo "════════════════════════════════════════════════════════"
        ;;
    *)
        echo "❌ Error: Unknown provider '$PROVIDER'"
        echo "Valid providers: github-sqlite, github-postgres, ado-sqlite, ado-postgres, github, ado, sqlite, postgres, all"
        exit 1
        ;;
esac
