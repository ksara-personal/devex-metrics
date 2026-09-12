using System;
using System.Text;

namespace Metrics.GitHub;

static class QueryBuilder
{
    /// <summary>
    /// Builds a GraphQL query with the specified resource template and variables.
    /// </summary>
    /// <param name="resourceTemplate"></param>
    /// <param name="variablesValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PostWithVariables<T> BuildQuery<T>(string resourceTemplate, T variablesValue, string[] fragments = null) where T : PostWithEmptyVariables
    {
        var queryTemplate = ResourceCache.GetValue(resourceTemplate);
        if(fragments is not null && fragments.Length > 0)
        {
            var builder = new StringBuilder();
            builder.AppendLine(queryTemplate);
            foreach(var fragment in fragments)
            {
                var fragmentContent = ResourceCache.GetValue(fragment);
                builder.AppendLine(fragmentContent);
            }
            queryTemplate = builder.ToString();
        }
        return new PostWithVariables<T>
        {
            query = queryTemplate,
            variables = variablesValue
        };
    }

    /// <summary>
    /// Builds a search query for multiple pr numbers.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="repo"></param>
    /// <param name="prNumbers"></param>
    /// <returns></returns>
    public static PostWithVariables<PostVariables> BuildSearchQuery(string owner, string repo, IEnumerable<int> prNumbers, string resourceTemplate = "PRStateSearchQuery")
    {
        var queryTemplate = ResourceCache.GetValue(resourceTemplate);
        var builder = new StringBuilder("query($owner: String!, $repo: String!) {");
        foreach (var prNumber in prNumbers)
        {
            var query = string.Format(queryTemplate, prNumber);
            builder.AppendLine(query);
        }
        builder.AppendLine("}");
        return new PostWithVariables<PostVariables>
        {
            query = builder.ToString(),
            variables = new PostVariables(owner, repo)
        };
    }

    public static PostWithVariables<PostTeamQueryVariables> BuildTeamMembersQuery(string owner, string teamPattern, string endCursor = null)
        => BuildQuery<PostTeamQueryVariables>("TeamMembersQuery", new PostTeamQueryVariables(owner, teamPattern, endCursor));
}
