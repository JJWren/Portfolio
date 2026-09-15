using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ThemeService(IDbContextFactory<AppDbContext> dbFactory)
{
    // Single-container deploy, so an in-process cache is safe; SaveAsync clears
    // it. The version counter lets a reader detect that a save happened while
    // its DB read was in flight and skip publishing the now-stale snapshot
    // into either _cache or _lastKnown.
    private volatile ThemeSnapshot? _cache;
    private volatile ThemeSnapshot? _lastKnown;
    private int _version;

    /// <summary>
    /// The most recently loaded or saved snapshot, kept even once
    /// <c>GetSnapshotAsync</c>'s own cache is cleared by a save or a later
    /// read fails. Null only before the very first successful load or save.
    /// A caller that must not retry the database on every call once the app
    /// has served one snapshot (<see cref="SecurityHeadersMiddleware"/>)
    /// prefers this over a fresh <see cref="GetSnapshotAsync"/>; before that
    /// first load, during an outage from process start, it stays null and
    /// such a caller falls back to <see cref="GetSnapshotAsync"/>'s own
    /// default-snapshot guard.
    /// </summary>
    public ThemeSnapshot? LastKnown
    {
        get => _lastKnown;
        private set => _lastKnown = value;
    }

    /// <summary>Resolved palette + emitted override CSS, cached until the next save.</summary>
    public async Task<ThemeSnapshot> GetSnapshotAsync()
    {
        if (_cache is { } cached)
        {
            return cached;
        }

        var versionBefore = Volatile.Read(ref _version);
        ThemeSettings? overrides;
        try
        {
            overrides = await GetOverridesAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Every page must render even when the DB blips: serve the built-in
            // palette and leave the cache empty so the next request retries.
            // LastKnown is left untouched, so a caller preferring it over a
            // fresh load keeps serving the real last-known snapshot through
            // the outage instead of silently reverting to defaults.
            // Cancellations still propagate so aborted requests die.
            return ThemeRules.DefaultSnapshot;
        }

        var snapshot = ThemeRules.BuildSnapshot(overrides?.Overrides);
        if (Volatile.Read(ref _version) == versionBefore)
        {
            _cache = snapshot;
            LastKnown = snapshot;
        }

        return snapshot;
    }

    /// <summary>Raw override row for the admin form; null when nothing has been saved yet.</summary>
    /// <remarks><c>virtual</c> only so tests can simulate a successful load without a database (the project has no test host; see ThemeServiceTests).</remarks>
    public virtual async Task<ThemeSettings?> GetOverridesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ThemeSettings.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ThemeSettings.SingletonId);
    }

    /// <summary>Normalizes the inputs (blank/invalid → dropped; nothing overridden → null column) and upserts the single row.</summary>
    public async Task SaveAsync(IReadOnlyDictionary<string, string> rawInputs)
    {
        var overrides = ThemeRules.BuildOverrides(rawInputs);

        try
        {
            await UpsertAsync(overrides);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Loser of a concurrent first save: the row exists now, so one
            // retry lands on the update path (last write wins).
            await UpsertAsync(overrides);
        }

        // Bump the version before clearing so an in-flight reader can tell its
        // snapshot predates this save and must not repopulate the cache.
        Interlocked.Increment(ref _version);
        _cache = null;

        // Built straight from the overrides just persisted — no database
        // round trip — so the very next request (the theme editor's own
        // forced reload after a save) sees the new snapshot immediately and
        // never a hash that predates this save.
        LastKnown = ThemeRules.BuildSnapshot(overrides);
    }

    private async Task UpsertAsync(Dictionary<string, string>? overrides)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.ThemeSettings
            .FirstOrDefaultAsync(t => t.Id == ThemeSettings.SingletonId);
        if (row is null)
        {
            row = new ThemeSettings { Id = ThemeSettings.SingletonId };
            db.ThemeSettings.Add(row);
        }

        // Always a fresh dictionary instance (never mutated in place) so the
        // jsonb column's change tracking sees the assignment.
        row.Overrides = overrides;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
