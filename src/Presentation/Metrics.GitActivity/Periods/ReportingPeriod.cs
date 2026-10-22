namespace Metrics.GitActivity.Periods;

/// <summary>
/// Unit a collection window is expressed in.
/// </summary>
public enum PeriodUnit
{
    Day,
    Week,
    Month,
    Quarter,
    Year
}

/// <summary>
/// A half-open UTC window <c>[Start, End)</c>.
/// </summary>
public readonly record struct DateWindow(DateTime Start, DateTime End)
{
    public bool Contains(DateTime value) => value >= Start && value < End;

    public override string ToString() => $"{Start:yyyy-MM-dd} .. {End.AddTicks(-1):yyyy-MM-dd}";
}

/// <summary>
/// Turns CLI duration options into a concrete window.
/// </summary>
public static class ReportingPeriod
{
    /// <summary>
    /// Resolves the collection window.
    /// </summary>
    /// <param name="unit">Period unit; month when omitted.</param>
    /// <param name="count">Number of periods, ending with the one that contains <paramref name="anchor"/>.</param>
    /// <param name="anchor">Any date inside the last period; today when omitted.</param>
    /// <param name="start">Explicit start; overrides the period when given.</param>
    /// <param name="end">Explicit end (exclusive); defaults to now when only start is given.</param>
    public static DateWindow Resolve(PeriodUnit? unit, int? count, DateTime? anchor, DateTime? start, DateTime? end, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (start is not null || end is not null)
        {
            if (start is null)
                throw new ArgumentException("--start is required when --end is given.");

            var s = AsUtc(start.Value);
            var e = end is null ? now : AsUtc(end.Value);
            if (e <= s)
                throw new ArgumentException("--end must be after --start.");
            return new DateWindow(s, e);
        }

        var n = count ?? 1;
        if (n < 1)
            throw new ArgumentException("--last must be at least 1.");

        var u = unit ?? PeriodUnit.Month;
        var periodStart = StartOf(u, AsUtc(anchor ?? now).Date);
        var windowEnd = Add(u, periodStart, 1);
        var windowStart = Add(u, periodStart, -(n - 1));
        return new DateWindow(windowStart, windowEnd);
    }

    static DateTime StartOf(PeriodUnit unit, DateTime date) => unit switch
    {
        PeriodUnit.Day => date,
        // ISO weeks start on Monday.
        PeriodUnit.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        PeriodUnit.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        PeriodUnit.Quarter => new DateTime(date.Year, (date.Month - 1) / 3 * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PeriodUnit.Year => new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    static DateTime Add(PeriodUnit unit, DateTime date, int amount) => unit switch
    {
        PeriodUnit.Day => date.AddDays(amount),
        PeriodUnit.Week => date.AddDays(7 * amount),
        PeriodUnit.Month => date.AddMonths(amount),
        PeriodUnit.Quarter => date.AddMonths(3 * amount),
        PeriodUnit.Year => date.AddYears(amount),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
