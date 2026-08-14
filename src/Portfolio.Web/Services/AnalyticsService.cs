using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>Totals for a period. Visitors are per-UTC-day uniques summed over
/// the period — keys rotate daily by design, so cross-day uniques don't exist.</summary>
public record StatsSummary(
    int Views, int DailyVisitors, int ContactSubmits, int ProjectClicks, int ResumeDownloads);

public record RouteStatRow(string Path, int Views, int Visitors);

public record ReferrerStatRow(string ReferrerHost, int Views);

public record EventStatRow(string Name, string? Target, int Count);

/// <summary>
/// Records anonymous page views and named events. Recording is best-effort:
/// failures are logged, never thrown into a request.
/// </summary>
public class AnalyticsService(
    IDbContextFactory<AppDbContext> dbFactory,
    TimeProvider timeProvider,
    ILogger<AnalyticsService> logger)
{
    private byte[]? _secret;

    /// <summary>Loads (or creates, on first ever use) the per-install secret.</summary>
    public async Task<byte[]> GetSecretAsync()
    {
        if (_secret is not null)
        {
            return _secret;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.AnalyticsStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == AnalyticsState.SingletonId);
        if (state is null)
        {
            state = new AnalyticsState
            {
                Id = AnalyticsState.SingletonId,
                Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            };
            db.AnalyticsStates.Add(state);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Lost a create race with a concurrent request — take theirs.
                state = await db.AnalyticsStates.AsNoTracking()
                    .FirstAsync(s => s.Id == AnalyticsState.SingletonId);
            }
        }

        return _secret = Convert.FromBase64String(state.Secret);
    }

    public async Task<string> ComputeVisitorKeyAsync(HttpContext context)
    {
        var secret = await GetSecretAsync();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var day = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return VisitorKey.Compute(secret, day, ip, userAgent);
    }

    public async Task RecordPageViewAsync(string path, string? referrerHost, string visitorKey)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.PageViews.Add(new PageView
            {
                Path = AnalyticsRules.Truncate(path, AnalyticsRules.PathMaxLength),
                ReferrerHost = referrerHost is null
                    ? null
                    : AnalyticsRules.Truncate(referrerHost, AnalyticsRules.ReferrerMaxLength),
                VisitorKey = visitorKey,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record page view for {Path}.", path);
        }
    }

    public async Task RecordEventAsync(string name, string? target, string visitorKey)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                Name = AnalyticsRules.Truncate(name, AnalyticsRules.EventNameMaxLength),
                Target = target is null
                    ? null
                    : AnalyticsRules.Truncate(target, AnalyticsRules.EventTargetMaxLength),
                VisitorKey = visitorKey,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record {Event} event.", name);
        }
    }

    /// <summary>Convenience for endpoint/page call sites: applies the standard
    /// bot and opt-out exclusions, then records.</summary>
    public async Task TryRecordEventAsync(HttpContext context, string name, string? target)
    {
        if (AnalyticsRules.IsBot(context.Request.Headers.UserAgent)
            || AnalyticsRules.OptedOut(context.Request.Headers))
        {
            return;
        }

        var key = await ComputeVisitorKeyAsync(context);
        await RecordEventAsync(name, target, key);
    }

    // -- Queries -------------------------------------------------------------
    // Completed days come from the Daily*Stat aggregates; today (not yet
    // rolled up) is computed live from the raw tables and merged in.

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    /// <summary>Earliest day with any recorded data; today when there is none.</summary>
    public async Task<DateOnly> FirstDayAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var first = await db.DailySiteStats
            .OrderBy(s => s.Day)
            .Select(s => (DateOnly?)s.Day)
            .FirstOrDefaultAsync();
        return first ?? Today;
    }

    public async Task<StatsSummary> GetSummaryAsync(DateOnly from, DateOnly to)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var today = Today;

        var views = 0;
        var visitors = 0;
        var site = await db.DailySiteStats.AsNoTracking()
            .Where(s => s.Day >= from && s.Day <= to)
            .GroupBy(_ => 1)
            .Select(g => new { Views = g.Sum(s => s.Views), Visitors = g.Sum(s => s.Visitors) })
            .FirstOrDefaultAsync();
        views += site?.Views ?? 0;
        visitors += site?.Visitors ?? 0;

        var events = await db.DailyEventStats.AsNoTracking()
            .Where(s => s.Day >= from && s.Day <= to)
            .GroupBy(s => s.Name)
            .Select(g => new { Name = g.Key, Count = g.Sum(s => s.Count) })
            .ToDictionaryAsync(g => g.Name, g => g.Count);

        if (to >= today && from <= today)
        {
            var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            views += await db.PageViews.CountAsync(v => v.OccurredAt >= start);
            visitors += await db.PageViews
                .Where(v => v.OccurredAt >= start)
                .Select(v => v.VisitorKey)
                .Distinct()
                .CountAsync();
            var todayEvents = await db.AnalyticsEvents
                .Where(e => e.OccurredAt >= start)
                .GroupBy(e => e.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var group in todayEvents)
            {
                events[group.Name] = events.GetValueOrDefault(group.Name) + group.Count;
            }
        }

        return new StatsSummary(
            views, visitors,
            events.GetValueOrDefault(AnalyticsRules.ContactSubmitEvent),
            events.GetValueOrDefault(AnalyticsRules.ProjectClickEvent),
            events.GetValueOrDefault(AnalyticsRules.ResumeDownloadEvent));
    }

    public async Task<PagedResult<RouteStatRow>> GetTopRoutesAsync(
        DateOnly from, DateOnly to, int page,
        RouteStatSortColumn sortColumn = RouteStatSortColumn.Views,
        SortDirection sortDirection = SortDirection.Descending)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = (await db.DailyRouteStats.AsNoTracking()
                .Where(s => s.Day >= from && s.Day <= to)
                .GroupBy(s => s.Path)
                .Select(g => new RouteStatRow(g.Key, g.Sum(s => s.Views), g.Sum(s => s.Visitors)))
                .ToListAsync())
            .ToDictionary(r => r.Path);

        var today = Today;
        if (to >= today && from <= today)
        {
            var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var live = await db.PageViews.AsNoTracking()
                .Where(v => v.OccurredAt >= start)
                .GroupBy(v => v.Path)
                .Select(g => new RouteStatRow(
                    g.Key, g.Count(), g.Select(v => v.VisitorKey).Distinct().Count()))
                .ToListAsync();
            foreach (var row in live)
            {
                rows[row.Path] = rows.TryGetValue(row.Path, out var existing)
                    ? new RouteStatRow(
                        row.Path, existing.Views + row.Views, existing.Visitors + row.Visitors)
                    : row;
            }
        }

        var sorted = ApplyRouteSort(rows.Values, sortColumn, sortDirection).ToList();
        var total = sorted.Count;
        page = PagedResult<RouteStatRow>.ClampPage(page, total, PageSizes.Admin);
        var items = sorted
            .Skip((page - 1) * PageSizes.Admin)
            .Take(PageSizes.Admin)
            .ToList();
        return new PagedResult<RouteStatRow>(items, page, PageSizes.Admin, total);
    }

    private static IEnumerable<RouteStatRow> ApplyRouteSort(
        IEnumerable<RouteStatRow> rows, RouteStatSortColumn column, SortDirection direction)
    {
        Func<RouteStatRow, object> key = column switch
        {
            RouteStatSortColumn.Path => r => r.Path,
            RouteStatSortColumn.Visitors => r => r.Visitors,
            _ => r => r.Views,
        };
        var sorted = direction == SortDirection.Ascending
            ? rows.OrderBy(key)
            : rows.OrderByDescending(key);
        return sorted.ThenBy(r => r.Path);
    }

    public async Task<IReadOnlyList<ReferrerStatRow>> GetTopReferrersAsync(
        DateOnly from, DateOnly to, int top = 10)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = (await db.DailyReferrerStats.AsNoTracking()
                .Where(s => s.Day >= from && s.Day <= to)
                .GroupBy(s => s.ReferrerHost)
                .Select(g => new ReferrerStatRow(g.Key, g.Sum(s => s.Views)))
                .ToListAsync())
            .ToDictionary(r => r.ReferrerHost);

        var today = Today;
        if (to >= today && from <= today)
        {
            var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var live = await db.PageViews.AsNoTracking()
                .Where(v => v.OccurredAt >= start && v.ReferrerHost != null)
                .GroupBy(v => v.ReferrerHost!)
                .Select(g => new ReferrerStatRow(g.Key, g.Count()))
                .ToListAsync();
            foreach (var row in live)
            {
                rows[row.ReferrerHost] = rows.TryGetValue(row.ReferrerHost, out var existing)
                    ? existing with { Views = existing.Views + row.Views }
                    : row;
            }
        }

        return rows.Values
            .OrderByDescending(r => r.Views)
            .ThenBy(r => r.ReferrerHost)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<EventStatRow>> GetEventBreakdownAsync(DateOnly from, DateOnly to)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = (await db.DailyEventStats.AsNoTracking()
                .Where(s => s.Day >= from && s.Day <= to)
                .GroupBy(s => new { s.Name, s.Target })
                .Select(g => new EventStatRow(g.Key.Name, g.Key.Target, g.Sum(s => s.Count)))
                .ToListAsync())
            .ToDictionary(r => (r.Name, r.Target));

        var today = Today;
        if (to >= today && from <= today)
        {
            var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var live = await db.AnalyticsEvents.AsNoTracking()
                .Where(e => e.OccurredAt >= start)
                .GroupBy(e => new { e.Name, e.Target })
                .Select(g => new EventStatRow(g.Key.Name, g.Key.Target, g.Count()))
                .ToListAsync();
            foreach (var row in live)
            {
                rows[(row.Name, row.Target)] = rows.TryGetValue((row.Name, row.Target), out var existing)
                    ? existing with { Count = existing.Count + row.Count }
                    : row;
            }
        }

        return rows.Values
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Name).ThenBy(r => r.Target)
            .ToList();
    }

    /// <summary>Views over the trailing window including today — dashboard card.</summary>
    public async Task<int> ViewsInLastDaysAsync(int days)
    {
        var today = Today;
        var summary = await GetSummaryAsync(today.AddDays(1 - days), today);
        return summary.Views;
    }
}
