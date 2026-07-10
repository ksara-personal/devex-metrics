@echo off
setlocal enabledelayedexpansion

REM Script to apply EF Core migrations for different database providers and contexts
REM Usage: apply-migrations.bat <provider>
REM Example: apply-migrations.bat postgres

set PROVIDER=%~1
set SCRIPT_DIR=%~dp0
set SRC_DIR=%SCRIPT_DIR%..\src

if "%PROVIDER%"=="" (
    echo Usage: %~nx0 ^<provider^>
    echo.
    echo Providers:
    echo   github-sqlite   - Apply GitHub SQLite migrations
    echo   github-postgres - Apply GitHub PostgreSQL migrations
    echo   ado-sqlite      - Apply ADO SQLite migrations
    echo   ado-postgres    - Apply ADO PostgreSQL migrations
    echo   github          - Apply GitHub migrations ^(both providers^)
    echo   ado             - Apply ADO migrations ^(both providers^)
    echo   sqlite          - Apply all SQLite migrations ^(GitHub + ADO^)
    echo   postgres        - Apply all PostgreSQL migrations ^(GitHub + ADO^)
    echo   all             - Apply all migrations ^(GitHub + ADO, both providers^)
    echo.
    echo Example: %~nx0 postgres
    echo Example: %~nx0 all
    exit /b 1
)

if /i "%PROVIDER%"=="github-sqlite" (
    call :apply_github_sqlite
    goto :done
)
if /i "%PROVIDER%"=="github-postgres" (
    call :apply_github_postgres
    goto :done
)
if /i "%PROVIDER%"=="github-postgresql" (
    call :apply_github_postgres
    goto :done
)
if /i "%PROVIDER%"=="ado-sqlite" (
    call :apply_ado_sqlite
    goto :done
)
if /i "%PROVIDER%"=="ado-postgres" (
    call :apply_ado_postgres
    goto :done
)
if /i "%PROVIDER%"=="ado-postgresql" (
    call :apply_ado_postgres
    goto :done
)
if /i "%PROVIDER%"=="github" (
    call :apply_github_sqlite
    echo.
    call :apply_github_postgres
    echo.
    echo 🎉 GitHub migrations applied for both providers!
    goto :done
)
if /i "%PROVIDER%"=="ado" (
    call :apply_ado_sqlite
    echo.
    call :apply_ado_postgres
    echo.
    echo 🎉 ADO migrations applied for both providers!
    goto :done
)
if /i "%PROVIDER%"=="sqlite" (
    call :apply_github_sqlite
    echo.
    call :apply_ado_sqlite
    echo.
    echo 🎉 All SQLite migrations applied!
    goto :done
)
if /i "%PROVIDER%"=="postgres" (
    call :apply_github_postgres
    echo.
    call :apply_ado_postgres
    echo.
    echo 🎉 All PostgreSQL migrations applied!
    goto :done
)
if /i "%PROVIDER%"=="postgresql" (
    call :apply_github_postgres
    echo.
    call :apply_ado_postgres
    echo.
    echo 🎉 All PostgreSQL migrations applied!
    goto :done
)
if /i "%PROVIDER%"=="all" (
    goto :apply_all
)
if /i "%PROVIDER%"=="both" (
    goto :apply_all
)

echo ❌ Error: Unknown provider '%PROVIDER%'
echo Valid providers: github-sqlite, github-postgres, ado-sqlite, ado-postgres, github, ado, sqlite, postgres, all
exit /b 1

:apply_all
echo ════════════════════════════════════════════════════════
echo 🚀 Applying all migrations ^(GitHub + ADO, both providers^)
echo ════════════════════════════════════════════════════════
echo.
call :apply_github_sqlite
echo.
call :apply_github_postgres
echo.
call :apply_ado_sqlite
echo.
call :apply_ado_postgres
echo.
echo ════════════════════════════════════════════════════════
echo 🎉 All migrations applied successfully!
echo ════════════════════════════════════════════════════════
goto :done

:done
echo.
echo 💡 To verify, check your database or run:
echo    dotnet ef migrations list --context DevExMetricDbContext
exit /b 0

REM ==========================================================
REM Functions
REM ==========================================================

:apply_github_sqlite
echo 📦 Applying GitHub SQLite migrations...
cd /d "%SRC_DIR%\Migrations\Metrics.GitHub.Migrations.Sqlite" || exit /b 1
dotnet build --configuration Release >nul 2>&1
set DataStoreType=SQLite
dotnet ef database update
if errorlevel 1 exit /b 1
echo ✅ GitHub SQLite migrations applied
exit /b 0

:apply_github_postgres
echo 🐘 Applying GitHub PostgreSQL migrations...
cd /d "%SRC_DIR%\Migrations\Metrics.GitHub.Migrations.Postgres" || exit /b 1
dotnet build --configuration Release >nul 2>&1
set DataStoreType=Postgres
dotnet ef database update
if errorlevel 1 exit /b 1
echo ✅ GitHub PostgreSQL migrations applied
exit /b 0

:apply_ado_sqlite
echo 📦 Applying ADO SQLite migrations...
cd /d "%SRC_DIR%\Migrations\Metrics.ADO.Migrations.Sqlite" || exit /b 1
dotnet build --configuration Release >nul 2>&1
set DataStoreType=SQLite
dotnet ef database update
if errorlevel 1 exit /b 1
echo ✅ ADO SQLite migrations applied
exit /b 0

:apply_ado_postgres
echo 🐘 Applying ADO PostgreSQL migrations...
cd /d "%SRC_DIR%\Migrations\Metrics.ADO.Migrations.Postgres" || exit /b 1
dotnet build --configuration Release >nul 2>&1
set DataStoreType=Postgres
dotnet ef database update
if errorlevel 1 exit /b 1
echo ✅ ADO PostgreSQL migrations applied
exit /b 0
