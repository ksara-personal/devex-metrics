# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/Metrics.Models/*.csproj ./src/Metrics.Models/
COPY src/Metrics/*.csproj ./src/Metrics/
COPY src/Metrics.ADO/*.csproj ./src/Metrics.ADO/
COPY src/Metrics.GitHub/*.csproj ./src/Metrics.GitHub/
# copy migrations projects
COPY src/Migrations/Metrics.ADO.Migrations.Postgres/*.csproj ./src/Migrations/Metrics.ADO.Migrations.Postgres/
COPY src/Migrations/Metrics.ADO.Migrations.Sqlite/*.csproj ./src/Migrations/Metrics.ADO.Migrations.Sqlite/
COPY src/Migrations/Metrics.GitHub.Migrations.Postgres/*.csproj ./src/Migrations/Metrics.GitHub.Migrations.Postgres/
COPY src/Migrations/Metrics.GitHub.Migrations.Sqlite/*.csproj ./src/Migrations/Metrics.GitHub.Migrations.Sqlite/

COPY src/mcp/dotnet/Metrics.MCP.StreamableHTTP/*.csproj ./src/mcp/dotnet/Metrics.MCP.StreamableHTTP/
WORKDIR /app/src/mcp/dotnet/Metrics.MCP.StreamableHTTP
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY . .

WORKDIR /app/src/Metrics.Models
RUN dotnet publish -c Release -o out

# Publish Metrics.ADO project
WORKDIR /app/src/Metrics.ADO
RUN dotnet publish -c Release -o out

# Publish Metrics.GitHub project
WORKDIR /app/src/Metrics.GitHub
RUN dotnet publish -c Release -o out

# Publish Metrics Migrations projects
WORKDIR /app/src/Migrations/Metrics.GitHub.Migrations.Sqlite
RUN dotnet publish -c Release -o out

WORKDIR /app/src/Migrations/Metrics.GitHub.Migrations.Postgres
RUN dotnet publish -c Release -o out

WORKDIR /app/src/Migrations/Metrics.ADO.Migrations.Sqlite
RUN dotnet publish -c Release -o out

WORKDIR /app/src/Migrations/Metrics.ADO.Migrations.Postgres
RUN dotnet publish -c Release -o out

# Publish Metrics.MCP.StreamableHTTP project (the main entry point)
WORKDIR /app/src/mcp/dotnet/Metrics.MCP.StreamableHTTP
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the ADO extension and its dependencies first
COPY --from=build /app/src/Metrics.ADO/out/. ./

# Copy the GitHub extension and its dependencies next
COPY --from=build /app/src/Metrics.GitHub/out/. ./

COPY --from=build /app/src/Migrations/Metrics.GitHub.Migrations.Sqlite/out/. ./
COPY --from=build /app/src/Migrations/Metrics.GitHub.Migrations.Postgres/out/. ./
COPY --from=build /app/src/Migrations/Metrics.ADO.Migrations.Sqlite/out/. ./
COPY --from=build /app/src/Migrations/Metrics.ADO.Migrations.Postgres/out/. ./
# Copy the main application output last to ensure its JWT dependencies take precedence
COPY --from=build /app/src/mcp/dotnet/Metrics.MCP.StreamableHTTP/out ./

ENTRYPOINT ["dotnet", "Metrics.MCP.StreamableHTTP.dll"]
