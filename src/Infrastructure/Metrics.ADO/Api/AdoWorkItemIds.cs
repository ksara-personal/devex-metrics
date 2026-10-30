namespace Metrics.ADO;

/// <summary>
/// Helpers to match ADO work item ids against PR work item ids, which may be stored
/// either as plain numbers ("123") or with the Azure Boards prefix ("AB#123").
/// </summary>
static class AdoWorkItemIds
{
    internal const string Prefix = "AB#";

    /// <summary>
    /// Expands ids to both the plain and prefixed forms, for use in database queries.
    /// </summary>
    internal static List<string> WithPrefixed(IEnumerable<string> ids) =>
        ids.SelectMany(id => new[] { id, Prefix + id }).Distinct().ToList();

    /// <summary>
    /// Whether the stored PR work item id refers to the given ADO id.
    /// </summary>
    internal static bool Matches(string? prWorkItemId, string? adoId) =>
        !string.IsNullOrEmpty(prWorkItemId) && !string.IsNullOrEmpty(adoId)
        && (prWorkItemId == adoId || string.Equals(prWorkItemId, Prefix + adoId, StringComparison.OrdinalIgnoreCase));
}
