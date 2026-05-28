using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Metrics.Models;

namespace Metrics;

/// <summary>
/// Simple repo user service cache to manage current user, team members and the excellence squad reviewers.
/// </summary>
public abstract class OrgUserService<T> where T : Organization
{
    protected readonly ILogger<OrgUserService<T>> _logger;
    Lazy<HashSet<string>> _ceReviewers;
    protected ConcurrentDictionary<string, HashSet<string>> _teamMembers = new(StringComparer.OrdinalIgnoreCase);
    protected ApiClient _apiClient;
    protected T? _org;
    Task? _completionTask;
    protected ConcurrentDictionary<string, string> _usersDict = new(StringComparer.OrdinalIgnoreCase);
    protected ConcurrentDictionary<string, List<string>> _teamDict = new(StringComparer.OrdinalIgnoreCase);
    IComparer<string> _teamNameComparer;

    /// <summary>
    /// ctor
    /// </summary>
    protected OrgUserService(ApiClient apiClient, ILogger<OrgUserService<T>> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ceReviewers = new Lazy<HashSet<string>>(() => GetCEReviewers(), true);
    }

    /// <summary>
    /// Initializes the service with the organization.
    /// </summary>
    /// <param name="org"></param>
    public void Initialize(T org)
    {
        _org = org ?? throw new ArgumentNullException(nameof(org));
        var teamNamePatterns = _org.IncludeTeamsWithNamePattern;
        teamNamePatterns = teamNamePatterns is null ? new List<string> { "" } : teamNamePatterns;
        _teamNameComparer = new TeamNameComparer(teamNamePatterns.ToList());

        InvalidateUserCache();
    }

    /// <summary>
    /// Invalidates the org user service tasks.
    /// </summary>
    internal void InvalidateUserCache()
    {
        if (_completionTask is not null)
        {
            EnsureCompletion();
            _completionTask?.Dispose();
            _completionTask = null;
        }

        var teamNamePatterns = _org.IncludeTeamsWithNamePattern;
        // use empty string to include all teams when not specified.
        teamNamePatterns = teamNamePatterns is null ? new List<string> { "" } : teamNamePatterns;
        _logger.LogInformation("Initializing org service for {Org} with team name patterns {Patterns}", _org.Owner, string.Join(",", teamNamePatterns));
        _completionTask = Task.WhenAll(teamNamePatterns.Select(pattern => AddOrUpdateTeamMembersByPatternAsync(pattern)));
    }

    /// <summary>
    /// Ensures that the completion task is finished.
    /// </summary>
    void EnsureCompletion()
    {
        if (_completionTask is not null && !_completionTask.IsCompleted)
            _completionTask.Wait();

        if (!_completionTask.IsCompletedSuccessfully && _completionTask.Exception is not null)
        {
            _completionTask.Exception.Handle(ex =>
            {
                _logger.LogError(ex, "Error initializing org user service for {Org}", _org?.Owner);
                return true;
            });
            throw new InvalidOperationException($"Error initializing org user service for {_org?.Owner}", _completionTask.Exception);
        }
    }

    /// <summary>
    /// Gets the code excellence reviewers.
    /// </summary>
    /// <returns></returns>
    protected HashSet<string> GetCEReviewers()
    {
        var teams = _org.CodeExcellenceTeams ?? Enumerable.Empty<string>();
        EnsureCompletion();

        return teams.SelectMany(team => _teamMembers.TryGetValue(team, out var members) ? members : Enumerable.Empty<string>())
            .ToHashSet();
    }

    /// <summary>
    /// Resolves the user to a team
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public List<string> ResolveUser2Team(string user)
    {
        user = user ?? throw new ArgumentNullException(nameof(user));
        EnsureCompletion();

        _teamDict.TryGetValue(user, out var team);
        return team;
    }

    /// <summary>
    /// Resolves the given name or email to a login.
    /// </summary>
    /// <param name="nameOrEmail"></param>
    /// <returns></returns>
    public string Resolve2UserLogin(string nameOrEmail)
    {
        nameOrEmail = nameOrEmail ?? throw new ArgumentNullException(nameof(nameOrEmail));
        EnsureCompletion();

        _usersDict.TryGetValue(nameOrEmail, out var login);
        return login;
    }

    /// <summary>
    /// Adds or updates the team members in the dictionary.
    /// This method is called when the team members are fetched from the API.
    /// </summary>
    /// <param name="teamMembersData"></param>
    /// <returns></returns>
    protected abstract Task AddOrUpdateTeamMembersAsync(TeamMembersData teamMembersData);

    /// <summary>
    /// Gets the team members by pattern.
    /// This method is called to fetch team members based on a pattern.
    /// </summary>
    /// <param name="pattern"></param>
    /// <returns></returns>
    protected async Task AddOrUpdateTeamMembersByPatternAsync(string pattern)
    {
        var results = await _apiClient.GetTeamsByPatternAsync(_org.Owner, pattern);

        if (results is not null)
            await AddOrUpdateTeamMembersAsync(results);
    }

    /// <summary>
    /// Helper method to get the team members and add it to the dictionary.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="team"></param>
    /// <param name="typeInfo"></param>
    /// <param name="enumeratorSelector"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected Task<IEnumerable<string>> InternalAddOrUpdateTeamMembers<TItem>(string team,
        Func<IReadOnlyList<TItem>> enumeratorSelector,
        Func<TItem, (string login, string name, string email)> selector)
        where TItem : notnull
    {
        team = team ?? throw new ArgumentNullException(nameof(team));

        var teamMembers = enumeratorSelector();
        IEnumerable<string> members = null;
        if (teamMembers is not null)
        {
            List<string> membersList = new(teamMembers.Count);
            foreach (var member in teamMembers)
            {
                var (login, name, email) = selector(member);
                _usersDict.AddOrUpdate(login, _ => login, (_, existing) => login);
                _teamDict.AddOrUpdate(login, _ => new() { team }, (_, existing) => { existing.Add(team); existing.Sort(_teamNameComparer); return existing; });

                membersList.Add(login);

                if (!string.IsNullOrEmpty(name))
                {
                    _usersDict.TryAdd(name, login);
                }
                if (!string.IsNullOrEmpty(email))
                {
                    _usersDict.TryAdd(email, login);
                }
            }
            members = membersList;
        }
        members = members ?? Enumerable.Empty<string>();
        _teamMembers.AddOrUpdate(team, _ => members.ToHashSet(), (_, existing) =>
        {
            existing.UnionWith(members);
            return existing;
        });
        return Task.FromResult(members);
    }

    /// <summary>
    /// Checks if the given reviewer belongs to the team.
    /// </summary>
    /// <param name="team"></param>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool IsTeamReviewer(global::Metrics.Models.Team team, string reviewer)
    {
        bool value = false;
        Func<string, bool> isReviewerTeamMember = team => !string.IsNullOrEmpty(team) 
            && _teamMembers.TryGetValue(team, out var members)
            && members != null 
            && members.Contains(reviewer, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(reviewer) && team != null)
        {
            EnsureCompletion();
            if(!(value = isReviewerTeamMember(team.RemoteName)))
            {
                value = isReviewerTeamMember(team.Name);
            }
        }
        return value;
    }

    /// <summary>
    /// Gets the code excellence reviewers.
    /// </summary>
    public IReadOnlyList<string> CodeExcellenceReviewers => _ceReviewers.Value.ToList();

    /// <summary>
    /// Gets the organization for which this service is managing users.
    /// </summary>
    public T Organization => _org;

    /// <summary>
    /// Checks if the reviewer belongs to the cached list.
    /// </summary>
    /// <param name="reviewer"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool IsCEReviewer(string reviewer) => !string.IsNullOrEmpty(reviewer) ? _ceReviewers.Value.Contains(reviewer) : false;
}

sealed class TeamNameComparer : IComparer<string>
{
    readonly List<string> _prefixOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamNameComparer"/> class.
    /// </summary>
    /// <param name="prefixOrder"></param>
    public TeamNameComparer(List<string> prefixOrder) => _prefixOrder = prefixOrder;

    /// <summary>
    /// Gets the prefix rank of the specified string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    int GetPrefixRank(string s) => _prefixOrder.FindIndex(p => s.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Compares two team names.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) 
            return 0;

        var rx = GetPrefixRank(x);
        var ry = GetPrefixRank(y);

        if (rx != ry)
            return (rx == -1 ? int.MaxValue : rx).CompareTo(ry == -1 ? int.MaxValue : ry);

        return StringComparer.OrdinalIgnoreCase.Compare(x, y);
    }
}
