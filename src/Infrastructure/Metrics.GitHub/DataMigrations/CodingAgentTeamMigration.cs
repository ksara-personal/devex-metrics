using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.DataMigrations;

/// <summary>
/// Migration to update the team for PRs authored by CodingAgent
/// </summary>
public sealed class CodingAgentTeamMigration : GitHubDataMigration
{
    const string MigrationId = "RemoveMetricItem";
    public CodingAgentTeamMigration(GitHubPRMetricsSynchronizer dataSynchronizer,
        ILogger<CodingAgentTeamMigration> logger) : base(dataSynchronizer, logger)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public override string EFMigrationId => MigrationId;

    /// <summary>
    /// Runs the migration
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task RunAsync(DevExMetricDbContext dbContext)
    {
        Func<IQueryable<PRMetrics>> query = () => dbContext.PRMetrics
            .AsSingleQuery()
            .Include(p => p.Team)
            .Where(p => p.Author == GitHubAuthors.CodingAgent && (p.State == "MERGED" || p.State == "CLOSED"));

        int count = 0;
        await UpdateExistingMetrics(dbContext, query, (existing, newMetric) =>
        {
            _logger.LogInformation("Updating PRMetric {Id} Team from {OldTeam} to {NewTeam}", existing.Id, existing.Team, newMetric.Team);
            if (!string.Equals(existing.Team?.Name, newMetric.Team?.Name, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(existing.Team?.ValueStream, newMetric.Team?.ValueStream, StringComparison.OrdinalIgnoreCase))
            {
                existing.Author = newMetric.Author;
                var team = dbContext.Teams.Where(t => t.Name == newMetric.Team.Name && t.Region == newMetric.Team.Region && t.ValueStream == newMetric.Team.ValueStream)
                            .SingleOrDefault();

                count++;
                if (team is not null)
                {
                    existing.Team = team;
                    existing.TeamId = team.Id;
                    return true;
                }
                else
                {
                    // create a new one.
                    existing.Team = newMetric.Team;
                    dbContext.Teams.Add(existing.Team);
                }
            }
            return false;
        });
        _logger.LogInformation("Updated {Count} PRMetrics with CodingAgent author to new team", count);
    }
}
