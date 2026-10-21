using Metrics.GitActivity.Periods;

namespace Metrics.GitActivity.Tests;

public class ReportingPeriodTests
{
    static readonly DateTime Now = new(2026, 10, 17, 9, 30, 0, DateTimeKind.Utc);

    static DateTime D(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Defaults_to_current_month()
    {
        var w = ReportingPeriod.Resolve(null, null, null, null, null, Now);
        Assert.Equal(new DateWindow(D(2026, 10, 1), D(2026, 11, 1)), w);
    }

    [Fact]
    public void Quarter_contains_anchor()
    {
        var w = ReportingPeriod.Resolve(PeriodUnit.Quarter, null, D(2026, 5, 20), null, null, Now);
        Assert.Equal(new DateWindow(D(2026, 4, 1), D(2026, 7, 1)), w);
    }

    [Fact]
    public void Last_n_months_ends_with_anchor_month()
    {
        var w = ReportingPeriod.Resolve(PeriodUnit.Month, 3, D(2026, 9, 10), null, null, Now);
        Assert.Equal(new DateWindow(D(2026, 7, 1), D(2026, 10, 1)), w);
    }

    [Fact]
    public void Week_starts_on_monday()
    {
        // 2026-10-17 is a Saturday.
        var w = ReportingPeriod.Resolve(PeriodUnit.Week, null, null, null, null, Now);
        Assert.Equal(new DateWindow(D(2026, 10, 12), D(2026, 10, 19)), w);
    }

    [Fact]
    public void Explicit_dates_override_period()
    {
        var w = ReportingPeriod.Resolve(PeriodUnit.Year, 5, null, D(2026, 1, 15), D(2026, 2, 15), Now);
        Assert.Equal(new DateWindow(D(2026, 1, 15), D(2026, 2, 15)), w);
    }

    [Fact]
    public void Start_only_runs_until_now()
    {
        var w = ReportingPeriod.Resolve(null, null, null, D(2026, 10, 1), null, Now);
        Assert.Equal(Now, w.End);
    }

    [Fact]
    public void Rejects_end_before_start() =>
        Assert.Throws<ArgumentException>(() => ReportingPeriod.Resolve(null, null, null, D(2026, 2, 1), D(2026, 1, 1), Now));
}
