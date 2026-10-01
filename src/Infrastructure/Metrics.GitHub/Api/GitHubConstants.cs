namespace Metrics.GitHub;

public struct GitHubConstants
{
    public const string GitHubGraphQLEndpoint = "https://api.github.com/graphql";

    public const string GitHubCommitUrl = "https://api.github.com/repos/{0}/{1}/commits/{2}";
    public const string GitHubCommitCompareUrl = "https://api.github.com/repos/{0}/{1}/compare/{2}...{3}";
    public const string GitHubPullRequestUrl = "https://github.com/{0}/pull/{1}";
    public const int GRAPHQL_QUERY_SEARCH_PAGINATION_LIMIT = 1000;

}

/// <summary>
/// Represents the github states
/// </summary>
public struct GitHubStates
{
    public const string Open = "OPEN";
    public const string Approved = "APPROVED";
    public const string Commented = "COMMENTED";
    public const string Closed = "CLOSED";
    public const string Merged = "MERGED";
    public const string ChangesRequested = "CHANGES_REQUESTED";
}

public struct GitHubTimelineStates
{
    public const string ReviewRequestedEvent = "ReviewRequestedEvent";
    public const string ReviewRequestRemovedEvent = "ReviewRequestRemovedEvent";
    public const string ConvertToDraftEvent = "ConvertToDraftEvent";
}

public struct GitHubBranchRefPrefixes
{
    public const string Release = "release/";
    public const string Feature = "feature";
}

public struct GitHubRequestedReviewerTypes
{
    public const string User = "User";
    public const string Team = "Team";
}

public struct GitHubAuthors
{
    public const string CodingAgent = "copilot-swe-agent";

    public const string Dependabot = "dependabot";

    public const string CodeReviewBot = "code-review-bot-github-app";

    public const string Ghost = "ghost";

    public static bool IsBotOrAI(string author) => string.Equals(CodingAgent, author) || string.Equals(Dependabot, author) || string.Equals(CodeReviewBot, author);
}
