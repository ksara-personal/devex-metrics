using ClosedXML.Excel;
using Metrics.GitActivity.Data;
using Metrics.GitActivity.Periods;
using Microsoft.EntityFrameworkCore;

namespace Metrics.GitActivity.Export;

/// <summary>
/// One exported column: header and value selector.
/// </summary>
public record ExportColumn<T>(string Header, Func<T, object?> Value);

/// <summary>
/// Exports a repository's stored PRs and commits to an xlsx workbook with one sheet each.
/// Column order follows the "Git PR Example Fields" / "Git Commit Example Fields" layouts.
/// </summary>
public sealed class ActivityWorkbookExporter(ActivityDbContext db)
{
    const string DateFormat = "yyyy-mm-dd\\Thh:mm:ss\\Z";

    public static readonly IReadOnlyList<ExportColumn<PullRequestRecord>> PullRequestColumns =
    [
        new("PR ID / Number", p => p.Number),
        new("Repository", p => p.Repository),
        new("Author Email", p => p.AuthorEmail ?? p.AuthorLogin),
        new("Created Date", p => p.CreatedAt),
        new("Merged Date", p => p.MergedAt),
        new("Status", p => StatusText(p.State)),
        new("Source Branch", p => p.SourceBranch),
        new("Target Branch", p => p.TargetBranch),
        new("Reviewer(s)", p => Join(Reviewers(p))),
        new("Review Decision", p => Humanize(p.ReviewDecision)),
        new("Comments Count", p => p.CommentsCount),
        new("Lines Added", p => p.LinesAdded),
        new("Lines Deleted", p => p.LinesDeleted),
        new("Files Changed", p => p.FilesChanged),
        new("Commits Count", p => p.CommitsCount),
        new("Title", p => p.Title),
        new("Review Submitted Date", p => p.ReviewSubmittedAt),
        new("Comment Authors", p => Join(CommentAuthors(p))),
        new("First Review Date", p => p.FirstReviewAt),
        new("Labels / Tags", p => Join(p.Labels.Select(l => l.Name))),
        new("Review Rounds", p => p.ReviewRounds),
        new("Description", p => p.Description),
        new("Linked Issues", p => Join(p.LinkedIssues.Select(i => i.Reference))),
        new("Requested Reviewers", p => Join(p.ReviewRequests.Select(r => r.ReviewerEmail ?? r.Reviewer))),
        new("CI / Build Status", p => CiText(p.CiStatus)),
    ];

    public static readonly IReadOnlyList<ExportColumn<CommitRecord>> CommitColumns =
    [
        new("Commit Hash", c => c.Hash),
        new("Author Email", c => c.AuthorEmail),
        new("Author Date", c => c.AuthorDate),
        new("Subject Line", c => c.Subject),
        new("Parent Hashes", c => c.ParentHashes),
        new("Author Name", c => c.AuthorName),
        new("Committer Email", c => c.CommitterEmail),
        new("Committer Date", c => c.CommitterDate),
        new("Committer Name", c => c.CommitterName),
        new("Message Body", c => c.Body),
    ];

    /// <summary>
    /// Commit columns for multi-repository exports: the repository first, then the standard layout.
    /// </summary>
    public static readonly IReadOnlyList<ExportColumn<CommitRecord>> MultiRepositoryCommitColumns =
        [new("Repository", c => c.Repository), .. CommitColumns];

    /// <summary>
    /// Stored repositories (from PRs or commits) named <c>owner/prefix*</c>, case-insensitive, sorted.
    /// </summary>
    public async Task<IReadOnlyList<string>> FindStoredRepositoriesAsync(string owner, string prefix, CancellationToken cancellationToken = default)
    {
        var start = $"{owner}/{prefix}";
        var stored = await db.PullRequests.Select(p => p.Repository)
            .Union(db.Commits.Select(c => c.Repository))
            .ToListAsync(cancellationToken);
        return stored.Where(r => r.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Writes one repository's workbook. PRs are filtered by created date and commits by author date when a window is given.
    /// </summary>
    public Task<(int pullRequests, int commits)> ExportAsync(string repository, DateWindow? window, string outputPath, CancellationToken cancellationToken = default) =>
        ExportAsync([repository], window, outputPath, includeRepositoryColumn: false, cancellationToken);

    /// <summary>
    /// Writes one workbook for several repositories: a single PR sheet and a single commit sheet,
    /// ordered by repository and date. <paramref name="includeRepositoryColumn"/> prepends a Repository
    /// column to the commit sheet (the PR sheet always has one).
    /// </summary>
    public async Task<(int pullRequests, int commits)> ExportAsync(IReadOnlyCollection<string> repositories, DateWindow? window, string outputPath,
        bool includeRepositoryColumn, CancellationToken cancellationToken = default)
    {
        var names = repositories.ToList();
        var prQuery = db.PullRequests.AsNoTracking()
            .Include(p => p.Reviews).Include(p => p.Comments).Include(p => p.ReviewRequests)
            .Include(p => p.Labels).Include(p => p.LinkedIssues)
            .AsSplitQuery()
            .Where(p => names.Contains(p.Repository));
        var commitQuery = db.Commits.AsNoTracking().Where(c => names.Contains(c.Repository));

        if (window is { } w)
        {
            prQuery = prQuery.Where(p => p.CreatedAt >= w.Start && p.CreatedAt < w.End);
            commitQuery = commitQuery.Where(c => c.AuthorDate >= w.Start && c.AuthorDate < w.End);
        }

        var prs = (await prQuery.ToListAsync(cancellationToken))
            .OrderBy(p => p.Repository, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.CreatedAt).ToList();
        var commits = (await commitQuery.ToListAsync(cancellationToken))
            .OrderBy(c => c.Repository, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.AuthorDate).ToList();

        using var workbook = new XLWorkbook();
        WriteSheet(workbook, "Pull Requests", PullRequestColumns, prs);
        WriteSheet(workbook, "Commits", includeRepositoryColumn ? MultiRepositoryCommitColumns : CommitColumns, commits);

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        workbook.SaveAs(outputPath);
        return (prs.Count, commits.Count);
    }

    static void WriteSheet<T>(XLWorkbook workbook, string name, IReadOnlyList<ExportColumn<T>> columns, IReadOnlyList<T> rows)
    {
        var sheet = workbook.Worksheets.Add(name);
        for (var c = 0; c < columns.Count; c++)
            sheet.Cell(1, c + 1).Value = columns[c].Header;

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                switch (columns[c].Value(rows[r]))
                {
                    case null: break;
                    case DateTime d:
                        cell.Value = d;
                        cell.Style.NumberFormat.Format = DateFormat;
                        break;
                    case int i: cell.Value = i; break;
                    case string s: cell.Value = s.Length > 32_000 ? s[..32_000] : s; break; // Excel cell limit is 32,767
                    case var other: cell.Value = other.ToString(); break;
                }
            }
        }

        var header = sheet.Range(1, 1, 1, columns.Count);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.SheetView.FreezeRows(1);
        if (rows.Count > 0)
            sheet.Range(1, 1, rows.Count + 1, columns.Count).SetAutoFilter();
        sheet.Columns().AdjustToContents(1, Math.Min(rows.Count + 1, 200), 8, 60);
    }

    static string Identity(string? email, string? login) => email ?? login ?? string.Empty;

    static bool IsAuthor(PullRequestRecord pr, string? login) =>
        string.Equals(login, pr.AuthorLogin, StringComparison.OrdinalIgnoreCase);

    /// <summary>People who submitted a review, excluding the PR author.</summary>
    internal static IEnumerable<string> Reviewers(PullRequestRecord pr) => pr.Reviews
        .Where(r => r.ReviewerLogin is not null && !IsAuthor(pr, r.ReviewerLogin))
        .OrderBy(r => r.SubmittedAt)
        .Select(r => Identity(r.ReviewerEmail, r.ReviewerLogin));

    /// <summary>People who commented in the conversation or in review threads, excluding the PR author.</summary>
    internal static IEnumerable<string> CommentAuthors(PullRequestRecord pr) => pr.Comments
        .Where(c => c.AuthorLogin is not null && !IsAuthor(pr, c.AuthorLogin))
        .Select(c => (c.CreatedAt as DateTime?, Identity(c.AuthorEmail, c.AuthorLogin)))
        .Concat(pr.Reviews
            .Where(r => r.ReviewerLogin is not null && !IsAuthor(pr, r.ReviewerLogin) && (r.CommentsCount > 0 || r.State == "COMMENTED"))
            .Select(r => (r.SubmittedAt, Identity(r.ReviewerEmail, r.ReviewerLogin))))
        .OrderBy(x => x.Item1)
        .Select(x => x.Item2);

    static string? Join(IEnumerable<string> values)
    {
        var distinct = values.Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return distinct.Count == 0 ? null : string.Join(", ", distinct);
    }

    internal static string StatusText(string state) => state switch
    {
        "MERGED" => "Merged",
        "CLOSED" => "Closed",
        "OPEN" => "Open",
        _ => state
    };

    internal static string? CiText(string? state) => state switch
    {
        null => null,
        "SUCCESS" => "Passed",
        "FAILURE" or "ERROR" => "Failed",
        "PENDING" or "EXPECTED" => "Pending",
        _ => Humanize(state)
    };

    /// <summary>CHANGES_REQUESTED -> Changes Requested.</summary>
    internal static string? Humanize(string? value) => value is null
        ? null
        : string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
}
