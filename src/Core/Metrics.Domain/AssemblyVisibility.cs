using System.Runtime.CompilerServices;

// The domain layer keeps a few helpers internal; the layers built on top of it need them.
[assembly: InternalsVisibleTo("Metrics.Application")]
[assembly: InternalsVisibleTo("Metrics.Infrastructure")]
[assembly: InternalsVisibleTo("Metrics.GitHub")]
[assembly: InternalsVisibleTo("Metrics.ADO")]
[assembly: InternalsVisibleTo("Metrics.MCP.StreamableHTTP")]
[assembly: InternalsVisibleTo("MetricsConsoleApp")]
[assembly: InternalsVisibleTo("Metrics.Tests")]
[assembly: InternalsVisibleTo("Metrics.ADO.Tests")]
