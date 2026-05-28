using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics.GitHub.Providers;

/// <summary>
/// Class for Copilot review metric
/// </summary>
public sealed class PRCopilotReviewMetricProvider : GitHubMetricProvider
{
    static readonly Regex _rx = new Regex(@".*Copilot\sreviewed\s(?<reviewed>[0-9]+)\sout\sof\s(?<files>[0-9]+)\schanged\sfiles\sin\sthis\spull\srequest\sand\sgenerated\s(?<comments>[a-z0-9]+)\scomment",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="logger"></param>
    public PRCopilotReviewMetricProvider(ILogger<PRCopilotReviewMetricProvider> logger) : base(logger)
    {
    }

    /// <summary>
    /// Calculate and adds the copilot specific metrics.
    /// </summary>
    /// <param name="pr"></param>
    /// <param name="metrics"></param>
    protected override void CalculateAndAddMetric(PullRequest pr, PRMetrics metrics)
    {
        var node = pr.CopilotReviewNode;
        if (node is not null)
        {
            if (!string.IsNullOrWhiteSpace(node.bodyText))
            {
                int filesReviewed = 0, filesChanged = 0, comments = 0;
                var match = _rx.Match(node.bodyText);
                if (match.Success)
                {
                    var reviewed = match.Groups["reviewed"].Value;
                    var files = match.Groups["files"].Value;
                    var commented = match.Groups["comments"].Value;

                    int.TryParse(reviewed, out filesReviewed);
                    int.TryParse(files, out filesChanged);
                    int.TryParse(commented, out comments);
                }
                metrics.CopilotReviewMetrics = new CopilotReviewMetrics
                {
                    FilesReviewed = filesReviewed,
                    FilesChanged = filesChanged,
                    Comments = comments
                };
            }
            else
            {
                Logger.LogWarning($"Copilot reviewer without body text, PR url:{pr.url}, Number:{pr.number}");
            }
        }
    }
}
