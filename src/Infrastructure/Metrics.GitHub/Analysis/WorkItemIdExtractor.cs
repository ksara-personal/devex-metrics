using System.Text.RegularExpressions;

namespace Metrics.GitHub;

/// <summary>
/// Extracts work item ids from PR text using configurable regex patterns.
/// Each pattern must define a named group <c>WorkItemId</c>; an optional format shapes the stored id.
/// </summary>
public sealed class WorkItemIdExtractor
{
    /// <summary>
    /// Name of the regex group holding the work item id.
    /// </summary>
    public const string WorkItemIdGroup = "WorkItemId";

    /// <summary>
    /// Pattern used when none is configured: Azure Boards links such as AB#123, stored as "AB#123".
    /// </summary>
    public static readonly WorkItemIdPattern DefaultPattern = new() { Pattern = @"AB#(?<WorkItemId>\d+)", Format = "AB#${WorkItemId}" };

    static readonly TimeSpan _matchTimeout = TimeSpan.FromSeconds(1);
    readonly (Regex Regex, string? Format)[] _patterns;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="patterns">Patterns; falls back to <see cref="DefaultPattern"/> when null or empty.</param>
    public WorkItemIdExtractor(IEnumerable<WorkItemIdPattern>? patterns)
    {
        var list = patterns?.Where(p => !string.IsNullOrWhiteSpace(p?.Pattern)).ToList();
        if (list is null || list.Count == 0)
            list = [DefaultPattern];

        _patterns = list.Select(p =>
        {
            var regex = new Regex(p.Pattern, RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.IgnoreCase, _matchTimeout);
            if (!regex.GetGroupNames().Contains(WorkItemIdGroup))
                throw new ArgumentException($"Work item id pattern '{p.Pattern}' must define a named group '{WorkItemIdGroup}'.", nameof(patterns));
            return (regex, string.IsNullOrWhiteSpace(p.Format) ? null : p.Format);
        }).ToArray();
    }

    /// <summary>
    /// Returns the distinct, formatted work item ids found in the text, in order of appearance.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return _patterns
            .SelectMany(p => p.Regex.Matches(text).Select(m => (Match: m, p.Format)))
            .Where(x => x.Match.Groups[WorkItemIdGroup] is { Success: true, Length: > 0 })
            .OrderBy(x => x.Match.Index)
            .Select(x => x.Format is null ? x.Match.Groups[WorkItemIdGroup].Value : x.Match.Result(x.Format))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
