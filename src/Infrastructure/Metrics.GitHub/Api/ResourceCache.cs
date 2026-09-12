using System;
using System.Reflection;
using System.Text.Json;

namespace Metrics.GitHub;

/// <summary>
/// GraphQL query cache.
/// </summary>
public static class ResourceCache
{
    /// <summary>
    /// Reads the embedded script.
    /// </summary>
    /// <param name="resourceName"></param>
    /// <returns></returns>
    static string ReadEmbeddedScript(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the value for the given key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal static string GetValue(string key) => _cachedQueries[key];

    /// <summary>
    /// Cached queries.
    /// </summary>
    static readonly Dictionary<string, string> _cachedQueries = new(StringComparer.OrdinalIgnoreCase)
    {
        [TeamMembersQuery] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.TeamMembersQuery.txt"),
        [SearchPRQuery] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.SearchPRQuery.txt"),
        [PRQuery] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.PRQuery.txt"),
        [PRQuery_Reviews] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.PRQuery_Reviews.txt"),
        [CurrentUser] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.ViewerQuery.txt"),
        [PRStateSearchQuery] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.StateSearchQuery.txt"),
        [Fragment_Comments] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.Fragment_Comments.txt"),
        [Fragment_Commits] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.Fragment_Commits.txt"),
        [Fragment_Reviews] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.Fragment_Reviews.txt"),
        [Fragment_TimelineItems] = ReadEmbeddedScript("Metrics.GitHub.Api.Resources.Fragment_TimelineItems.txt")
    };

    public const string PRQuery = "PRQuery";
    public const string PRQuery_Reviews = "PRQuery_Reviews";
    public const string TeamMembersQuery = "TeamMembersQuery";
    public const string SearchPRQuery = "SearchPRQuery";
    public const string CurrentUser = "CurrentUser";
    public const string PRStateSearchQuery = "PRStateSearchQuery";
    public const string Fragment_Comments = "Fragment_Comments";
    public const string Fragment_Commits = "Fragment_Commits";
    public const string Fragment_Reviews = "Fragment_Reviews";
    public const string Fragment_TimelineItems = "Fragment_TimelineItems";
}
