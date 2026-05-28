namespace Metrics.GitHub;

public record FileCommin(
 string sha,
 string filename,
 string status,
 int additions,
 int deletions,
 int changes
    );

public record CommitCompareRoot(
string url,
string status,
int total_commits,
IReadOnlyList<FileCommin> files
);
