using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>Pure aggregation and scheduling logic for the nightly rollup.</summary>
public static class AnalyticsRollup
{
    /// <summary>Raw PageView/AnalyticsEvent rows older than this are deleted;
    /// only the daily aggregates remain.</summary>
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    /// <summary>20 minutes past UTC midnight: slack for clock skew around the
    /// day boundary so "yesterday" is definitely complete.</summary>
    public static readonly TimeSpan DailyRunTime = TimeSpan.FromMinutes(20);

    public record RollupResult(
        DailySiteStat Site,
        IReadOnlyList<DailyRouteStat> Routes,
        IReadOnlyList<DailyReferrerStat> Referrers,
        IReadOnlyList<DailyEventStat> Events);

    public static RollupResult Aggregate(
        DateOnly day, IEnumerable<PageView> views, IEnumerable<AnalyticsEvent> events)
    {
        var viewList = views.ToList();

        var site = new DailySiteStat
        {
            Day = day,
            Views = viewList.Count,
            Visitors = viewList.Select(v => v.VisitorKey).Distinct().Count(),
        };

        var routes = viewList
            .GroupBy(v => v.Path)
            .Select(g => new DailyRouteStat
            {
                Day = day,
                Path = g.Key,
                Views = g.Count(),
                Visitors = g.Select(v => v.VisitorKey).Distinct().Count(),
            })
            .OrderBy(r => r.Path)
            .ToList();

        var referrers = viewList
            .Where(v => v.ReferrerHost is not null)
            .GroupBy(v => v.ReferrerHost!)
            .Select(g => new DailyReferrerStat { Day = day, ReferrerHost = g.Key, Views = g.Count() })
            .OrderBy(r => r.ReferrerHost)
            .ToList();

        var eventStats = events
            .GroupBy(e => (e.Name, e.Target))
            .Select(g => new DailyEventStat
            {
                Day = day,
                Name = g.Key.Name,
                Target = g.Key.Target,
                Count = g.Count(),
            })
            .OrderBy(e => e.Name).ThenBy(e => e.Target)
            .ToList();

        return new RollupResult(site, routes, referrers, eventStats);
    }

    /// <summary>Next daily run strictly after <paramref name="now"/>.</summary>
    public static DateTimeOffset NextRunUtc(DateTimeOffset now)
    {
        var todayRun = new DateTimeOffset(
            now.UtcDateTime.Date + DailyRunTime, TimeSpan.Zero);
        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }
}
