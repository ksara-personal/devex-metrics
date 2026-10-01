# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln ./
COPY MetricsConsoleApp/*.csproj ./MetricsConsoleApp/
COPY Metrics/*.csproj ./Metrics/
COPY ExcelCloner/*.csproj ./ExcelCloner/
COPY MetricsService/*.csproj ./MetricsService/
COPY Metrics.MCP.Stdio/*.csproj ./Metrics.MCP.Stdio/
COPY Metrics.MCP.StreamableHTTP/*.csproj ./Metrics.MCP.StreamableHTTP/
COPY Metrics.Tests/*.csproj ./Metrics.Tests/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /app/MetricsConsoleApp
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/MetricsConsoleApp/out ./



ENTRYPOINT ["dotnet", "MetricsConsoleApp.dll"]
