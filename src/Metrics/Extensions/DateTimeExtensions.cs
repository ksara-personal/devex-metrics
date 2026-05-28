using System;

namespace Metrics;

/// <summary>
/// Extensions class for datetime/timespan related functions.
/// </summary>
static class DateTimeExtensions
{
    /// <summary>
    /// Converts the given nullable DateTime to UTC if needed.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static DateTime ConvertToUtcIfNeeded(this DateTime? dateTime, DateTime defaultValue)
    {
        DateTime dt = defaultValue;
        if (dateTime.HasValue)
        {
            var value = dateTime.Value;
            dt = value.Kind == DateTimeKind.Utc ? value : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified));
        }
        return dt;
    }

    /// <summary>
    /// Gets the timespan considering the business days.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static TimeSpan GetWeekdayTimeSpan(this DateTime start, DateTime end)
    {
        var temp = end < start ? start : end;
        start = start > end ? end : start;
        end = temp;
        var total = TimeSpan.Zero;
        var current = start;

        while (current < end)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                DateTime next = current.Date.AddDays(1) < end ? current.Date.AddDays(1) : end;
                total += next - current;
                current = next;
            }
            else
            {
                current = current.Date.AddDays(1);
            }
        }
        return total;
    }

    /// <summary>
    /// Determines whether the specified dateTime is within the given interval.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static bool IsWithinRange(this DateTime dateTime, DateTime start, DateTime end) => dateTime >= start && dateTime <= end;
    
    /// <summary>
    /// Determines whether the specified date is within the given interval.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static bool IsWithinRange(this DateOnly date, DateTime start, DateTime end)
    {
        var dt = date.ToDateTime(new TimeOnly(0, 0, 0));
        return dt >= start && dt <= end;
    }
}
