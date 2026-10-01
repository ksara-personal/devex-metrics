# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution-wide build configuration and every project file first, so that
# restore is cached independently of the source.
COPY Directory.Build.props Directory.Packages.props Metrics.slnx ./
COPY src/Core/Metrics.Domain/*.csproj                                       ./src/Core/Metrics.Domain/
COPY src/Core/Metrics.Application/*.csproj                                  ./src/Core/Metrics.Application/
COPY src/Infrastructure/Metrics.Infrastructure/*.csproj                     ./src/Infrastructure/Metrics.Infrastructure/
COPY src/Infrastructure/Metrics.GitHub/*.csproj                             ./src/Infrastructure/Metrics.GitHub/
COPY src/Infrastructure/Metrics.ADO/*.csproj                                ./src/Infrastructure/Metrics.ADO/
COPY src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Sqlite/*.csproj   ./src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Sqlite/
COPY src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Postgres/*.csproj ./src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Postgres/
COPY src/Infrastructure/Migrations/Metrics.ADO.Migrations.Sqlite/*.csproj      ./src/Infrastructure/Migrations/Metrics.ADO.Migrations.Sqlite/
COPY src/Infrastructure/Migrations/Metrics.ADO.Migrations.Postgres/*.csproj    ./src/Infrastructure/Migrations/Metrics.ADO.Migrations.Postgres/
COPY src/Presentation/Metrics.MCP.StreamableHTTP/*.csproj                   ./src/Presentation/Metrics.MCP.StreamableHTTP/
COPY src/Presentation/MetricsConsoleApp/*.csproj                            ./src/Presentation/MetricsConsoleApp/
COPY tests/Metrics.Tests/*.csproj                                           ./tests/Metrics.Tests/
COPY tests/Metrics.ADO.Tests/*.csproj                                       ./tests/Metrics.ADO.Tests/
RUN dotnet restore src/Presentation/Metrics.MCP.StreamableHTTP/Metrics.MCP.StreamableHTTP.csproj

# Copy everything else and publish.
COPY . .

# The extension assemblies and the migration assemblies are loaded by file name at
# runtime, so each one is published separately and the outputs are overlaid below.
RUN dotnet publish src/Infrastructure/Metrics.ADO/Metrics.ADO.csproj                                      -c Release -o /out/ado && \
    dotnet publish src/Infrastructure/Metrics.GitHub/Metrics.GitHub.csproj                                -c Release -o /out/github && \
    dotnet publish src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Sqlite/Metrics.GitHub.Migrations.Sqlite.csproj     -c Release -o /out/gh-sqlite && \
    dotnet publish src/Infrastructure/Migrations/Metrics.GitHub.Migrations.Postgres/Metrics.GitHub.Migrations.Postgres.csproj -c Release -o /out/gh-postgres && \
    dotnet publish src/Infrastructure/Migrations/Metrics.ADO.Migrations.Sqlite/Metrics.ADO.Migrations.Sqlite.csproj           -c Release -o /out/ado-sqlite && \
    dotnet publish src/Infrastructure/Migrations/Metrics.ADO.Migrations.Postgres/Metrics.ADO.Migrations.Postgres.csproj       -c Release -o /out/ado-postgres && \
    dotnet publish src/Presentation/Metrics.MCP.StreamableHTTP/Metrics.MCP.StreamableHTTP.csproj          -c Release -o /out/host

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Extensions and migration assemblies first...
COPY --from=build /out/ado/.          ./
COPY --from=build /out/github/.       ./
COPY --from=build /out/gh-sqlite/.    ./
COPY --from=build /out/gh-postgres/.  ./
COPY --from=build /out/ado-sqlite/.   ./
COPY --from=build /out/ado-postgres/. ./
# ...and the host last, so its JWT dependencies take precedence.
COPY --from=build /out/host/.         ./

ENTRYPOINT ["dotnet", "Metrics.MCP.StreamableHTTP.dll"]
