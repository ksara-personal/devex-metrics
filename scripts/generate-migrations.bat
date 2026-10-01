@echo off
setlocal enabledelayedexpansion

REM Script to generate EF Core migrations for different database providers and contexts
REM Usage: generate-migrations.bat <provider> <migration-name>
REM Example: generate-migrations.bat sqlite AddNewColumn

set PROVIDER=%~1
set MIGRATION_NAME=%~2
set SCRIPT_DIR=%~dp0
set SRC_DIR=%SCRIPT_DIR%..\src\Infrastructure

if "%PROVIDER%"=="" goto :usage
if "%MIGRATION_NAME%"=="" goto :usage
goto :dispatch

:usage
echo Usage: %~nx0 ^<provider^> ^<migration-name^>
echo.
echo Providers:
echo   github-sqlite   - Generate GitHub SQLite migration
echo   github-postgres - Generate GitHub PostgreSQL migration
echo   ado-sqlite      - Generate ADO SQLite migration
echo   ado-postgres    - Generate ADO PostgreSQL migration
echo   github          - Generate migrations for GitHub ^(both providers^)
echo   ado             - Generate migrations for ADO ^(both providers^)
echo   all             - Generate all migrations ^(GitHub + ADO, both providers^)
echo.
echo Example: %~nx0 github-sqlite AddNewColumn
echo Example: %~nx0 all AddNewColumn
exit /b 1

:dispatch
if /i "%PROVIDER%"=="github-sqlite" (
    call :generate_github_sqlite_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="github-postgres" (
    call :generate_github_postgres_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="github-postgresql" (
    call :generate_github_postgres_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="ado-sqlite" (
    call :generate_ado_sqlite_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="ado-postgres" (
    call :generate_ado_postgres_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="ado-postgresql" (
    call :generate_ado_postgres_migration "%MIGRATION_NAME%"
    goto :done
)
if /i "%PROVIDER%"=="github" (
    call :generate_github_sqlite_migration "%MIGRATION_NAME%"
    echo.
    call :generate_github_postgres_migration "%MIGRATION_NAME%"
    echo.
    echo 🎉 GitHub migrations generated for both providers!
    goto :done
)
if /i "%PROVIDER%"=="ado" (
    call :generate_ado_sqlite_migration "%MIGRATION_NAME%"
    echo.
    call :generate_ado_postgres_migration "%MIGRATION_NAME%"
    echo.
    echo 🎉 ADO migrations generated for both providers!
    goto :done
)
if /i "%PROVIDER%"=="all" goto :generate_all
if /i "%PROVIDER%"=="both" goto :generate_all

echo ❌ Error: Unknown provider '%PROVIDER%'
echo Valid providers: github-sqlite, github-postgres, ado-sqlite, ado-postgres, github, ado, all
exit /b 1

:generate_all
echo ════════════════════════════════════════════════════════
echo 🚀 Generating migrations for GitHub and ADO ^(all providers^)
echo ════════════════════════════════════════════════════════
echo.
call :generate_github_sqlite_migration "%MIGRATION_NAME%"
echo.
call :generate_github_postgres_migration "%MIGRATION_NAME%"
echo.
call :generate_ado_sqlite_migration "%MIGRATION_NAME%"
echo.
call :generate_ado_postgres_migration "%MIGRATION_NAME%"
echo.
echo ════════════════════════════════════════════════════════
echo 🎉 All migrations generated successfully!
echo ════════════════════════════════════════════════════════
goto :done

:done
echo.
echo 💡 To apply migrations:
echo    For GitHub: set DataStoreType=^<SQLite^|Postgres^> ^&^& dotnet ef database update --project src\Infrastructure\Metrics.Infrastructure
echo    For ADO:    set DataStoreType=^<SQLite^|Postgres^> ^&^& dotnet ef database update --project src\Infrastructure\Metrics.ADO
exit /b 0

REM ==========================================================
REM Functions
REM ==========================================================

:generate_github_sqlite_migration
echo 📦 Generating GitHub SQLite migration: %~1
cd /d "%SRC_DIR%\Migrations\Metrics.GitHub.Migrations.Sqlite" || exit /b 1
echo    Building project...
dotnet build --configuration Release >nul 2>&1
set DataStoreType=SQLite
dotnet ef migrations add "%~1"
if errorlevel 1 exit /b 1
echo ✅ GitHub SQLite migration generated
exit /b 0

:generate_ado_sqlite_migration
echo 📦 Generating ADO SQLite migration: %~1
cd /d "%SRC_DIR%\Migrations\Metrics.ADO.Migrations.Sqlite" || exit /b 1
echo    Building project...
dotnet build --configuration Release >nul 2>&1
set DataStoreType=SQLite
dotnet ef migrations add "%~1"
if errorlevel 1 exit /b 1
echo ✅ ADO SQLite migration generated
exit /b 0

:generate_github_postgres_migration
echo 🐘 Generating GitHub PostgreSQL migration: %~1
cd /d "%SRC_DIR%\Migrations\Metrics.GitHub.Migrations.Postgres" || exit /b 1
echo    Building project...
dotnet build --configuration Release >nul 2>&1
set DataStoreType=Postgres
dotnet ef migrations add "%~1"
if errorlevel 1 exit /b 1
echo ✅ GitHub PostgreSQL migration generated
exit /b 0

:generate_ado_postgres_migration
echo 🐘 Generating ADO PostgreSQL migration: %~1
cd /d "%SRC_DIR%\Migrations\Metrics.ADO.Migrations.Postgres" || exit /b 1
echo    Building project...
dotnet build --configuration Release >nul 2>&1
set DataStoreType=Postgres
dotnet ef migrations add "%~1"
if errorlevel 1 exit /b 1
echo ✅ ADO PostgreSQL migration generated
exit /b 0
