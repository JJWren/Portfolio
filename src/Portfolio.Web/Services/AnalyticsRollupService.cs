using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>
/// Nightly analytics rollup and retention: aggregates each completed UTC day
/// into the Daily*Stat tables (catching up after downtime on startup), then
/// deletes raw rows older than <see cref="AnalyticsRollup.RetentionPeriod"/>.
/// </summary>
public sealed class AnalyticsRollupService(
    IDbContextFactory<AppDbContext> dbFactory,
    TimeProvider timeProvider,
    ILogger<AnalyticsRollupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analytics rollup failed; retrying at the next scheduled run.");
            }

            var now = timeProvider.GetUtcNow();
            var delay = AnalyticsRollup.NextRunUtc(now) - now;
            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var startDay = await FirstUnrolledDayAsync(db, cancellationToken);
        for (var day = startDay; day is not null && day.Value < today; day = day.Value.AddDays(1))
        {
            await RollUpDayAsync(db, day.Value, cancellationToken);
        }

        var cutoff = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - AnalyticsRollup.RetentionPeriod;
        var deletedViews = await db.PageViews
            .Where(v => v.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedEvents = await db.AnalyticsEvents
            .Where(e => e.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        if (deletedViews + deletedEvents > 0)
        {
            logger.LogInformation(
                "Analytics retention removed {Views} raw page views and {Events} events.",
                deletedViews, deletedEvents);
        }
    }

    /// <summary>The day after the watermark, or the first day with raw data
    /// when no rollup has ever run; null when there's nothing to do.</summary>
    private static async Task<DateOnly?> FirstUnrolledDayAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var watermark = await db.DailySiteStats
            .OrderByDescending(s => s.Day)
            .Select(s => (DateOnly?)s.Day)
            .FirstOrDefaultAsync(cancellationToken);
        if (watermark is not null)
        {
            return watermark.Value.AddDays(1);
        }

        var firstView = await db.PageViews
            .OrderBy(v => v.OccurredAt)
            .Select(v => (DateTime?)v.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
        var firstEvent = await db.AnalyticsEvents
            .OrderBy(e => e.OccurredAt)
            .Select(e => (DateTime?)e.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        var first = (firstView, firstEvent) switch
        {
            (null, null) => (DateTime?)null,
            (null, _) => firstEvent,
            (_, null) => firstView,
            _ => firstView < firstEvent ? firstView : firstEvent,
        };
        return first is null ? null : DateOnly.FromDateTime(first.Value);
    }

    private static async Task RollUpDayAsync(
        AppDbContext db, DateOnly day, CancellationToken cancellationToken)
    {
        var from = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = from.AddDays(1);

        var views = await db.PageViews.AsNoTracking()
            .Where(v => v.OccurredAt >= from && v.OccurredAt < to)
            .ToListAsync(cancellationToken);
        var events = await db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt < to)
            .ToListAsync(cancellationToken);

        var result = AnalyticsRollup.Aggregate(day, views, events);

        // Delete-then-insert inside one transaction keeps re-runs idempotent.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.DailySiteStats.Where(s => s.Day == day).ExecuteDeleteAsync(cancellationToken);
        await db.DailyRouteStats.Where(s => s.Day == day).ExecuteDeleteAsync(cancellationToken);
        await db.DailyReferrerStats.Where(s => s.Day == day).ExecuteDeleteAsync(cancellationToken);
        await db.DailyEventStats.Where(s => s.Day == day).ExecuteDeleteAsync(cancellationToken);

        db.DailySiteStats.Add(result.Site);
        db.DailyRouteStats.AddRange(result.Routes);
        db.DailyReferrerStats.AddRange(result.Referrers);
        db.DailyEventStats.AddRange(result.Events);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
