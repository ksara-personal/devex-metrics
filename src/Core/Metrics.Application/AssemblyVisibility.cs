using System.Runtime.CompilerServices;

// Mapping and analysis helpers stay internal to the application layer but are needed by the
// adapters that plug into it and by the test projects.
[assembly: InternalsVisibleTo("Metrics.Infrastructure")]
[assembly: InternalsVisibleTo("Metrics.GitHub")]
[assembly: InternalsVisibleTo("Metrics.ADO")]
[assembly: InternalsVisibleTo("Metrics.MCP.StreamableHTTP")]
[assembly: InternalsVisibleTo("MetricsConsoleApp")]
[assembly: InternalsVisibleTo("Metrics.Tests")]
[assembly: InternalsVisibleTo("Metrics.ADO.Tests")]
