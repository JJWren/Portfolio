using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ThemeService(IDbContextFactory<AppDbContext> dbFactory)
{
    // Single-container deploy, so an in-process cache is safe; SaveAsync clears
    // it. The version counter lets a reader detect that a save happened while
    // its DB read was in flight and skip publishing the now-stale snapshot.
    private volatile ThemeSnapshot? _cache;
    private int _version;

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
            // Cancellations still propagate so aborted requests die.
            return ThemeRules.DefaultSnapshot;
        }

        var snapshot = ThemeRules.BuildSnapshot(overrides?.Overrides);
        if (Volatile.Read(ref _version) == versionBefore)
        {
            _cache = snapshot;
        }

        return snapshot;
    }

    /// <summary>Raw override row for the admin form; null when nothing has been saved yet.</summary>
    public async Task<ThemeSettings?> GetOverridesAsync()
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
