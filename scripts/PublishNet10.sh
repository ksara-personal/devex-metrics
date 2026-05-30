#!/bin/bash

# Exit on any error
set -e

# Build Console App
PROJECT_ROOT="/path/to/DevMetrics"
CONFIGURATION="Release"
OSX_RUNTIME="osx-arm64"
WIN_RUNTIME="win-x64"
OUTPUT_DIR="./publish"

# Display .NET SDK version
echo "Using .NET SDK version:"
dotnet --version

publish_project() {
  local RELATIVE_PATH=$1
  local RUNTIME=$2
  local PROJECT_PATH="$PROJECT_ROOT/$RELATIVE_PATH"
  local PROJECT_NAME=$(basename "$PROJECT_PATH" .csproj)
  local OUTPUT_DIR="$OUTPUT_DIR/$RUNTIME"
  mkdir -p "$OUTPUT_DIR"

  echo "Cleaning $PROJECT_NAME..."
  dotnet clean "$PROJECT_PATH" --configuration $CONFIGURATION

  echo "Publishing $PROJECT_NAME..."
  dotnet publish "$PROJECT_PATH" \
    --configuration $CONFIGURATION \
    --runtime $RUNTIME \
    --output "$OUTPUT_DIR" \
    --self-contained true \
    /p:PublishTrimmed=true \
    /p:PublishSingleFile=true

  echo "✅ Published $PROJECT_NAME to $OUTPUT_DIR"
}

publish_projects() {
  local RUNTIME=$1

  publish_project "MetricsConsoleApp/MetricsConsoleApp.csproj" $1
  publish_project "Metrics.MCP.Stdio/Metrics.MCP.Stdio.csproj" $1
  publish_project "Metrics.MCP.StreamableHTTP/Metrics.MCP.StreamableHTTP.csproj" $1
  publish_project "MetricsService/MetricsService.csproj" $1
}

publish_projects $OSX_RUNTIME
publish_projects $WIN_RUNTIME
