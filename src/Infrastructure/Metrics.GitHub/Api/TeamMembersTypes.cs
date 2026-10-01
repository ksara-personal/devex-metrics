using System;

namespace Metrics.GitHub;

/// <summary>
/// Represents the data wrapper containing the organization information.
/// </summary>
/// <param name="organization">The organization object containing team information.</param>
public record OrganizationData(OrganizationNode organization);

/// <summary>
/// Represents a GitHub organization with a single team.
/// </summary>
/// <param name="teams">The team object within the organization.</param>
public record OrganizationNode(Teams teams);

/// <summary>
/// Represents the root object for an organization API response, inheriting from TeamMembersData.
/// </summary>
/// <param name="data">The data object containing organization information.</param>
public record OrganizationRoot(OrganizationData data) : TeamMembersData();

/// <summary>
/// Represents a collection of team member nodes.
/// </summary>
/// <param name="nodes">The list of team member nodes.</param>
public record Members(IReadOnlyList<TeamsNode> nodes);

public record Teams(
 int totalCount,
 PageInfo pageInfo,
 List<Team> nodes
    );

/// <summary>
/// Represents a node containing information about a team member.
/// </summary>
/// <param name="login">The login name of the team member.</param>
/// <param name="name">The full name of the team member.</param>
/// <param name="email">The email address of the team member.</param>
public record TeamsNode(
    string login,
    string name,
    string email
    );

/// <summary>
/// Represents a GitHub team with its details and members.
/// </summary>
/// <param name="name">The name of the team.</param>
/// <param name="slug">The slug identifier for the team.</param>
/// <param name="members">The members of the team.</param>
public record Team(
    string name,
    string slug,
    Members members
    );

/// <summary>
/// Represents a team roster entry with user, team, role, and name information.
/// </summary>
/// <param name="GitHubUser">The GitHub username.</param>
/// <param name="Team">The team name.</param>
/// <param name="Role">The user's role in the team.</param>
/// <param name="Name">The full name of the user.</param>
public record TeamRoster(
    string GitHubUser,
    string Team,
    string Role,
    string Name
   );

