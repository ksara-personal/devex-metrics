using Metrics.Models;
using Metrics.MultiTenant;

namespace Metrics;

/// <summary>
/// Represents a sprint in the sprint calendar.
/// </summary>
public sealed class SprintCalendar
{
    readonly SprintCalendarSettings _settings;
    Dictionary<string, Sprint> _sprintByReleaseCache = new(StringComparer.OrdinalIgnoreCase);
    List<Sprint> _sprints;
    static readonly DateTime DefaultUpToDate = DateTime.Parse("2030-12-31");

    /// <summary>
    /// Initializes a new instance of the <see cref="SprintCalendar"/> class.
    /// </summary>
    /// <param name="tenantConfig">Tenant configuration service for resolving tenant-specific sprint calendar settings</param>
    public SprintCalendar(TenantConfigurationService tenantConfig) 
    {
        _settings = tenantConfig.GetSettings<SprintCalendarSettings>(ConfigSectionNames.SprintCalendar)
            ?? throw new InvalidOperationException("SprintCalendar settings not configured for tenant");
        _sprints = GenerateSprints(); // initialize sprints
    }

    /// <summary>
    /// Gets the boundaries for a specific sprint.
    /// </summary>
    /// <param name="year"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    public Sprint? GetSprintInfo(int year, int sprintNumber)
    {
        var sprint = _sprints.Find(s => s.SprintNumber == sprintNumber && s.Year == year);
        if (sprint == null)
            throw new ArgumentException($"Sprint {sprintNumber} for year {year} not found.");

        return sprint;
    }

    /// <summary>
    /// Gets a sprint by its release number.
    /// </summary>
    /// <param name="releaseNumber"></param>
    /// <returns></returns>
    public Sprint? GetSprintByRelease(string releaseNumber)
    {
        if (!_sprintByReleaseCache.TryGetValue(releaseNumber, out var sprint))
            throw new ArgumentException($"Sprint with release number {releaseNumber} not found.");

        return sprint;
    }
    
    /// <summary>
    /// Gets the list of sprints that end on or before a specific date.
    /// </summary>
    /// <param name="upToDate"></param>
    /// <returns></returns>
    public IEnumerable<Sprint> GetSprintsUpto(DateTime? upToDate = null)
    {
        var endDate = _settings.GenerateSprintsUptoDate ?? DefaultUpToDate;
        if (upToDate.HasValue && upToDate > endDate)
        {
            throw new ArgumentException($"Sprints are configured with an end date of {endDate}");
        }
        
        return upToDate.HasValue ? _sprints.FindAll(s => s.EndDate <= upToDate) : _sprints.AsReadOnly();
    }

    /// <summary>
    /// Gets the list of sprints that occur after a specific sprint.
    /// </summary>
    /// <param name="sprintNumber"></param>
    /// <param name="year"></param>
    /// <returns></returns>
    public IEnumerable<Sprint> GetSprintsAfter(int sprintNumber, int year) =>
        _sprints.FindAll(s => s.Year > year || (s.Year == year && s.SprintNumber > sprintNumber));

    /// <summary>
    /// Generates a list of sprints up to a specified date.
    /// </summary>
    /// <param name="upToDate"></param>
    /// <returns></returns>
    public List<Sprint> GenerateSprints()
    {
        var sprints = new List<Sprint>();
        var earliestSprintStart = _settings.EarliestKnownSprintStartDate;
        var sprintEndDate = earliestSprintStart.AddDays(13).AddHours(23).AddMinutes(59).AddSeconds(59);
        var sprintNumber = _settings.EarliestKnownSprintNumber;
        var currentReleaseName = _settings.EarliestKnownReleaseName;

        var endDate = _settings.GenerateSprintsUptoDate ?? DefaultUpToDate;

        do
        {
            sprintNumber = sprintEndDate.Year - 1 == earliestSprintStart.Year || earliestSprintStart.DayOfYear < 7 ? 1 : sprintNumber + 1;
            currentReleaseName = GetReleaseNumber(sprintNumber, sprintEndDate.Year, currentReleaseName);
            var sprint = new Sprint
            {
                SprintNumber = sprintNumber,
                Year = sprintEndDate.Year,
                StartDate = earliestSprintStart,
                EndDate = sprintEndDate,
                ReleaseNumber = currentReleaseName
            };

            if (!_sprintByReleaseCache.TryGetValue(sprint.ReleaseNumber, out var cachedSprint))
            {
                _sprintByReleaseCache.Add(sprint.ReleaseNumber, sprint);
            }
            
            sprints.Add(sprint);

            earliestSprintStart = sprintEndDate.Date.AddDays(1);
            sprintEndDate = earliestSprintStart.AddDays(13).AddHours(23).AddMinutes(59).AddSeconds(59);
            // special case for year end
            if (sprintEndDate.Month == 12)
            {
                // let's try to adjust the last sprint to end on Dec 31 if possible
                var end = sprintEndDate.AddDays(7);
                if ((new DateTime(end.Year, 12, 31) - end).TotalDays < 7)
                {
                    sprintEndDate = end;
                }
            }

        } while (earliestSprintStart <= endDate);
        return sprints;
    }

    /// <summary>
    /// Gets the release number for a given sprint.
    /// </summary>
    /// <param name="sprintNumber"></param>
    /// <param name="year"></param>
    /// <param name="previousReleaseName"></param>
    /// <returns></returns>
    string GetReleaseNumber(int sprintNumber, int year, string previousReleaseName)
    {
        foreach (var item in _settings.KnownSprintReleaseNames)
        {
            if (sprintNumber == item.SprintNumber && year == item.Year)
            {
                return item.ReleaseName;
            }
        }
        
        var arr = previousReleaseName.Split('.');
        if (arr.Length != 3 || !int.TryParse(arr[1], out int minor))
        {
            throw new InvalidOperationException($"Invalid release name format: {previousReleaseName}");
        }

        minor++;
        return $"{arr[0]}.{minor}.{arr[2]}";
    }
}
