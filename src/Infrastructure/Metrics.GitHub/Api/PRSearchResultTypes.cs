using System;

namespace Metrics.GitHub;

/// <summary>
/// Represents the data wrapper containing the search results.
/// </summary>
/// <param name="search">The search object containing the results.</param>
public record SearchData(Search search);

/// <summary>
/// Represents an edge in the search result graph, containing a search node.
/// </summary>
/// <param name="node">The search node object.</param>
public record Edge(SearchNode node);

/// <summary>
/// Represents a node in the search results, corresponding to a pull request or issue.
/// </summary>
/// <param name="title">The title of the pull request or issue.</param>
/// <param name="number">The number of the pull request or issue.</param>
/// <param name="url">The URL of the pull request or issue.</param>
/// <param name="baseRefName">The base reference name of the pull request.</param>
/// <param name="createdAt">The creation date.</param>
/// <param name="mergedAt">The merge date.</param>
/// <param name="mergedBy">The user who merged the pull request.</param>
/// <param name="repository">The repository containing the pull request or issue.</param>
/// <param name="author">The author of the pull request or issue.</param>
public record SearchNode(
   string title,
   int number,
   string url,
   string baseRefName,
   DateTime createdAt,
   DateTime? mergedAt,
   Author mergedBy,
   SearchRepository repository,
   Author author
    );

/// <summary>
/// Represents a repository in the search results.
/// </summary>
/// <param name="name">The name of the repository.</param>
public record SearchRepository(string name, SearchRepositoryOwner owner);

/// <summary>
/// Represents the owner of a repository in the search results.
/// </summary>
/// <param name="login"></param>
/// <summary>
public record SearchRepositoryOwner(string login);

/// <summary>
/// Represents the root object for a search API response, inheriting from PRSearchData.
/// </summary>
/// <param name="data">The data object containing search results.</param>
public record SearchRoot(SearchData data) : PRSearchData();

/// <summary>
/// Represents the search results, including issue count, pagination info, and edges.
/// </summary>
/// <param name="issueCount">The total number of issues found.</param>
/// <param name="pageInfo">The pagination information.</param>
/// <param name="edges">The list of edges (search nodes).</param>
public record Search(int issueCount, PageInfo pageInfo, IReadOnlyList<Edge> edges);

/// <summary>
/// Represents pagination information for search results.
/// </summary>
/// <param name="hasNextPage">Indicates if there is a next page of results.</param>
/// <param name="endCursor">The end cursor for pagination.</param>
public record PageInfo(bool hasNextPage, string endCursor);

public record PullRequestShort(int number, string state, DateTime? updatedAt);

public record SearchStateRoot(Dictionary<string, PullRequestContainer> data);

public record PullRequestContainer(PullRequestShort pullRequest);

