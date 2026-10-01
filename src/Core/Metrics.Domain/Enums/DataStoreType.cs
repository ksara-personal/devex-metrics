namespace Metrics.Domain;

/// <summary>
/// Datastore types supported for metrics.
/// </summary>
[Flags]
public enum DataStoreType
{
    API,
    File,
    SQLite,
    Postgres,
    InMemory
}
