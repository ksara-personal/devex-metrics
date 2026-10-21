using System.CommandLine;
using System.CommandLine.Invocation;
using Metrics.GitHub;
using Metrics.Infrastructure;
using Metrics.GitActivity;
using Metrics.GitActivity.Collection;
using Metrics.GitActivity.Data;
using Metrics.GitActivity.Export;
using Metrics.GitActivity.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var repoOption = new Option<string?>("--repo", "Repository as owner/name");
repoOption.AddAlias("-r");
var prefixOption = new Option<string?>("--repo-prefix", "All repositories of an owner whose name starts with a prefix, as owner/prefix (e.g. acme/source.)");
prefixOption.AddAlias("-x");
var orgOption = new Option<string?>("--org", "All repositories of an organization or user (same as --repo-prefix owner/ with an empty prefix)");
var periodOption = new Option<PeriodUnit?>("--period", "Window unit: Day, Week, Month (default), Quarter, Year");
periodOption.AddAlias("-p");
var lastOption = new Option<int?>("--last", "Number of periods, ending with the one containing --anchor (default 1)");
lastOption.AddAlias("-n");
var anchorOption = new Option<DateTime?>("--anchor", "Any date inside the last period (default today)");
var startOption = new Option<DateTime?>("--start", "Explicit window start (UTC); overrides --period");
var endOption = new Option<DateTime?>("--end", "Explicit window end, exclusive (UTC); default now");
var dbOption = new Option<string?>("--db", "SQLite database path (default from appsettings)");

var branchOption = new Option<string?>("--branch", "Branch for commit history (default: repository default branch)");
var skipPrsOption = new Option<bool>("--skip-prs", "Do not collect pull requests");
var skipCommitsOption = new Option<bool>("--skip-commits", "Do not collect commits");
var restartOption = new Option<bool>("--restart", "Ignore the checkpoint of an unfinished run and collect the window from the start");

var includeArchivedOption = new Option<bool>("--include-archived", "With --repo-prefix, also collect archived repositories");
var refreshOption = new Option<bool>("--refresh", "With --repo-prefix, re-collect repositories that already completed this window");

var outOption = new Option<string?>("--out", "Output xlsx path (default <owner>_<repo or prefix>_<window>.xlsx)");
outOption.AddAlias("-o");
var allOption = new Option<bool>("--all", "Export everything stored for the repository, ignoring the window");

var collectCommand = new Command("collect", "Collect PRs and commits of one repository, or every repository matching a prefix, from GitHub into the SQLite database")
{
    repoOption, prefixOption, orgOption, periodOption, lastOption, anchorOption, startOption, endOption, dbOption, branchOption,
    skipPrsOption, skipCommitsOption, restartOption, includeArchivedOption, refreshOption
};
collectCommand.AddValidator(ValidateTarget);
collectCommand.SetHandler(async (InvocationContext ctx) =>
{
    var target = ParseTarget(ctx);
    var window = ResolveWindow(ctx);
    using var host = BuildHost(ctx.ParseResult.GetValueForOption(dbOption), requireToken: true);
    using var scope = host.Services.CreateScope();
    await MigrateAsync(scope.ServiceProvider);

    var settings = scope.ServiceProvider.GetRequiredService<GitActivitySettings>();
    var collector = scope.ServiceProvider.GetRequiredService<ActivityCollector>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var branch = ctx.ParseResult.GetValueForOption(branchOption) ?? settings.CommitBranch;
    var includePullRequests = !ctx.ParseResult.GetValueForOption(skipPrsOption);
    var includeCommits = !ctx.ParseResult.GetValueForOption(skipCommitsOption);
    var restart = ctx.ParseResult.GetValueForOption(restartOption);
    var cancellationToken = ctx.GetCancellationToken();

    if (target.Prefix is null)
    {
        logger.LogInformation("Collecting {Owner}/{Repo} for {Window}", target.Owner, target.Repo, window);
        var run = await collector.CollectAsync(target.Owner, target.Repo!, window, branch, includePullRequests, includeCommits, restart, cancellationToken);
        logger.LogInformation("Done{Resumed}: {PRs} PRs, {Commits} commits stored in {Db}",
            run.Resumed ? " (resumed)" : string.Empty, run.PullRequestCount, run.CommitCount, settings.DatabasePath);
        return;
    }

    var repos = await collector.FindRepositoriesAsync(target.Owner, target.Prefix,
        ctx.ParseResult.GetValueForOption(includeArchivedOption), cancellationToken);
    if (repos.Count == 0)
    {
        logger.LogWarning("No repositories of {Owner} match {Selection}", target.Owner, Describe(target));
        return;
    }
    logger.LogInformation("Collecting {Count} repositories of {Owner} ({Selection}) for {Window}: {Repos}",
        repos.Count, target.Owner, Describe(target), window, string.Join(", ", repos));

    var refresh = ctx.ParseResult.GetValueForOption(refreshOption);
    var failed = new List<string>();
    int prTotal = 0, commitTotal = 0, skipped = 0;
    for (var i = 0; i < repos.Count; i++)
    {
        var repository = $"{target.Owner}/{repos[i]}";
        if (!refresh && !restart && await collector.IsCompleteAsync(repository, window, includePullRequests, includeCommits, cancellationToken))
        {
            logger.LogInformation("[{Index}/{Count}] {Repository}: already collected for this window, skipping (use --refresh to re-collect)",
                i + 1, repos.Count, repository);
            skipped++;
            continue;
        }

        logger.LogInformation("[{Index}/{Count}] {Repository}", i + 1, repos.Count, repository);
        try
        {
            var run = await collector.CollectAsync(target.Owner, repos[i], window, branch, includePullRequests, includeCommits, restart, cancellationToken);
            prTotal += run.PullRequestCount;
            commitTotal += run.CommitCount;
        }
        catch (RateLimitExceededException)
        {
            // The remaining repositories would hit the same limit; the next run resumes here.
            failed.AddRange(repos.Skip(i).Select(r => $"{target.Owner}/{r}"));
            logger.LogError("Rate limit wait budget exhausted at {Repository}; stopping. Re-run the same command to resume.", repository);
            break;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failed.Add(repository);
            logger.LogError(ex, "{Repository} failed; continuing with the next repository", repository);
        }
    }

    logger.LogInformation("Done: {Collected} collected, {Skipped} skipped, {Failed} failed; {PRs} PRs and {Commits} commits stored in {Db}",
        repos.Count - skipped - failed.Count, skipped, failed.Count, prTotal, commitTotal, settings.DatabasePath);
    if (failed.Count > 0)
    {
        logger.LogError("Not completed (re-run to resume): {Repos}", string.Join(", ", failed));
        ctx.ExitCode = 1;
    }
});

var exportCommand = new Command("export", "Export PRs and commits of one repository, or every stored repository matching a prefix, to a single xlsx")
{
    repoOption, prefixOption, orgOption, periodOption, lastOption, anchorOption, startOption, endOption, dbOption, outOption, allOption
};
exportCommand.AddValidator(ValidateTarget);
exportCommand.SetHandler(async (InvocationContext ctx) =>
{
    var target = ParseTarget(ctx);
    DateWindow? window = ctx.ParseResult.GetValueForOption(allOption) ? null : ResolveWindow(ctx);
    var name = target.Prefix switch
    {
        null => target.Repo!,
        "" => "all-repos",
        var p => $"{p.TrimEnd('.', '-', '_')}-all"
    };
    var output = ctx.ParseResult.GetValueForOption(outOption)
        ?? $"{target.Owner}_{name}_{(window is { } w ? $"{w.Start:yyyyMMdd}-{w.End.AddTicks(-1):yyyyMMdd}" : "all")}.xlsx";

    using var host = BuildHost(ctx.ParseResult.GetValueForOption(dbOption), requireToken: false);
    using var scope = host.Services.CreateScope();
    await MigrateAsync(scope.ServiceProvider);
    var exporter = scope.ServiceProvider.GetRequiredService<ActivityWorkbookExporter>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int prs, commits;
    if (target.Prefix is null)
    {
        (prs, commits) = await exporter.ExportAsync($"{target.Owner}/{target.Repo}", window, output, ctx.GetCancellationToken());
    }
    else
    {
        var repositories = await exporter.FindStoredRepositoriesAsync(target.Owner, target.Prefix, ctx.GetCancellationToken());
        if (repositories.Count == 0)
        {
            logger.LogWarning("No stored repositories match {Owner}/{Prefix}*; run collect first", target.Owner, target.Prefix);
            ctx.ExitCode = 1;
            return;
        }
        logger.LogInformation("Exporting {Count} repositories: {Repos}", repositories.Count, string.Join(", ", repositories));
        (prs, commits) = await exporter.ExportAsync(repositories, window, output, includeRepositoryColumn: true, ctx.GetCancellationToken());
    }
    logger.LogInformation("Exported {PRs} PRs and {Commits} commits to {Output}", prs, commits, Path.GetFullPath(output));
});

var root = new RootCommand("Per-repository GitHub PR and commit collector") { collectCommand, exportCommand };
return await root.InvokeAsync(args);

DateWindow ResolveWindow(InvocationContext ctx) => ReportingPeriod.Resolve(
    ctx.ParseResult.GetValueForOption(periodOption),
    ctx.ParseResult.GetValueForOption(lastOption),
    ctx.ParseResult.GetValueForOption(anchorOption),
    ctx.ParseResult.GetValueForOption(startOption),
    ctx.ParseResult.GetValueForOption(endOption));

void ValidateTarget(System.CommandLine.Parsing.CommandResult result)
{
    var hasRepo = !string.IsNullOrWhiteSpace(result.GetValueForOption(repoOption));
    var hasPrefix = !string.IsNullOrWhiteSpace(result.GetValueForOption(prefixOption));
    var hasOrg = !string.IsNullOrWhiteSpace(result.GetValueForOption(orgOption));
    if (new[] { hasRepo, hasPrefix, hasOrg }.Count(x => x) != 1)
        result.ErrorMessage = "Specify exactly one of --repo owner/name, --repo-prefix owner/prefix or --org owner.";
}

static string Describe(RepoSelection target) =>
    target.Prefix is { Length: > 0 } p ? $"names starting with '{p}'" : "all repositories";

RepoSelection ParseTarget(InvocationContext ctx)
{
    if (ctx.ParseResult.GetValueForOption(orgOption) is { Length: > 0 } org)
    {
        var orgName = org.Trim().Trim('/');
        if (orgName.Length == 0 || orgName.Contains('/'))
            throw new ArgumentException($"--org must be an organization or user name, got '{org}'.");
        return new RepoSelection(orgName, null, string.Empty);
    }
    if (ctx.ParseResult.GetValueForOption(prefixOption) is { Length: > 0 } prefix)
    {
        // owner/prefix: the prefix may itself end with separators such as '.' or '-'.
        var slash = prefix.IndexOf('/');
        if (slash <= 0 || slash == prefix.Length - 1)
            throw new ArgumentException($"--repo-prefix must be owner/prefix, got '{prefix}'.");
        return new RepoSelection(prefix[..slash].Trim(), null, prefix[(slash + 1)..].Trim());
    }
    var (owner, repo) = ParseRepo(ctx.ParseResult.GetValueForOption(repoOption)!);
    return new RepoSelection(owner, repo, null);
}

static (string owner, string repo) ParseRepo(string value)
{
    var parts = value.Trim().Trim('/').Split('/');
    if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
        throw new ArgumentException($"--repo must be owner/name, got '{value}'.");
    return (parts[0], parts[1]);
}

static IHost BuildHost(string? dbPath, bool requireToken)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Configuration.SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true)
        .AddEnvironmentVariables();

    var settings = builder.Configuration.GetSection(GitActivitySettings.SectionName).Get<GitActivitySettings>() ?? new();
    settings.UserEmails = new(settings.UserEmails, StringComparer.OrdinalIgnoreCase);
    settings.PAT = Environment.GetEnvironmentVariable("GITHUB_TOKEN") is { Length: > 0 } token ? token : settings.PAT;
    if (!string.IsNullOrWhiteSpace(dbPath))
        settings.DatabasePath = dbPath;
    if (requireToken && string.IsNullOrWhiteSpace(settings.PAT))
        throw new InvalidOperationException("Set GITHUB_TOKEN or GitActivity:PAT in appsettings.json.");

    builder.Services
        .AddSingleton(settings)
        .AddDbContext<ActivityDbContext>(o => o.UseSqlite($"Data Source={settings.DatabasePath}"))
        .AddSingleton(new GitHubActivityMapper(settings.UserEmails))
        .AddScoped<IActivitySource, GitHubActivitySource>()
        .AddScoped(sp => new RateLimitBackoff(sp.GetRequiredService<ILogger<RateLimitBackoff>>())
        {
            MaxTotalWait = TimeSpan.FromMinutes(settings.RateLimitMaxWaitMinutes)
        })
        .AddScoped(sp => new ActivityCollector(
            sp.GetRequiredService<IActivitySource>(),
            sp.GetRequiredService<ActivityDbContext>(),
            sp.GetRequiredService<GitHubActivityMapper>(),
            sp.GetRequiredService<RateLimitBackoff>(),
            sp.GetRequiredService<ILogger<ActivityCollector>>())
        {
            ChunkSize = TimeSpan.FromDays(Math.Max(1, settings.SearchChunkDays))
        })
        .AddScoped<ActivityWorkbookExporter>()
        .RegisterHttpClient<GitHubApiClient>(_ => (settings.PAT, GitHubConstants.GitHubGraphQLEndpoint), "GitActivity 1.0");

    return builder.Build();
}

static async Task MigrateAsync(IServiceProvider services) =>
    await services.GetRequiredService<ActivityDbContext>().Database.MigrateAsync();

public partial class Program;

/// <summary>
/// Either a single repository (<see cref="Repo"/>) or every repository starting with <see cref="Prefix"/>;
/// an empty prefix (<c>--org</c>) selects all of the owner's repositories.
/// </summary>
record RepoSelection(string Owner, string? Repo, string? Prefix);
