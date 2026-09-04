using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>Every <see cref="SiteContent"/> column, normalized once from a
/// <see cref="SiteContentDraft"/> in <see cref="SiteContentService.SaveAsync"/>
/// and carried unchanged through the upsert (and its retry). One record
/// instead of ten same-typed positional parameters, so the upsert path
/// cannot transpose two strings or two nullable ints by accident.</summary>
internal sealed record SiteContentValues(
    string? HeroHeading,
    string? Tagline,
    string? About,
    List<string>? Skills,
    string? OwnerPhotoAlt,
    string? HeroEyebrow,
    List<string>? GamePlan,
    string? BeltCaption,
    int? BeltDegrees,
    List<string>? Principles,
    List<string>? Eras,
    List<string>? Now,
    string? OwnerPhotoFlipAlt);

public class SiteContentService(IDbContextFactory<AppDbContext> dbFactory, SiteConfig site)
{
    // Single-container deploy, so an in-process cache is safe; SaveAsync clears
    // it. The version counter lets a reader detect that a save happened while
    // its DB read was in flight and skip publishing the now-stale snapshot.
    private volatile EffectiveSiteContent? _cache;
    private int _version;

    /// <summary>Resolved landing copy — DB overrides over .env values, cached until the next save.</summary>
    public async Task<EffectiveSiteContent> GetEffectiveAsync()
    {
        if (_cache is { } cached)
        {
            return cached;
        }

        var versionBefore = Volatile.Read(ref _version);
        SiteContent? overrides;
        try
        {
            overrides = await GetOverridesAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The landing page must render even when the DB blips: serve the
            // .env defaults and leave the cache empty so the next request
            // retries. Cancellations still propagate so aborted requests die.
            return SiteContentRules.Resolve(site, null);
        }

        var resolved = SiteContentRules.Resolve(site, overrides);
        if (Volatile.Read(ref _version) == versionBefore)
        {
            _cache = resolved;
        }

        return resolved;
    }

    /// <summary>Raw override row for the admin form; null when nothing has been saved yet.</summary>
    public async Task<SiteContent?> GetOverridesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.SiteContents.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == SiteContent.SingletonId);
    }

    /// <summary>Normalizes every field (blank → null → .env fallback) and upserts the single row.</summary>
    public async Task SaveAsync(SiteContentDraft draft)
    {
        var values = new SiteContentValues(
            HeroHeading: SiteContentRules.NormalizeField(draft.HeroHeading),
            Tagline: SiteContentRules.NormalizeField(draft.Tagline),
            About: SiteContentRules.NormalizeField(draft.About),
            Skills: SiteContentRules.ParseSkills(draft.SkillsText),
            OwnerPhotoAlt: SiteContentRules.NormalizeField(draft.OwnerPhotoAlt),
            HeroEyebrow: SiteContentRules.NormalizeField(draft.HeroEyebrow),
            GamePlan: SiteContentRules.ParseLines(draft.GamePlanText),
            BeltCaption: SiteContentRules.NormalizeField(draft.BeltCaption),
            BeltDegrees: SiteContentRules.ParseDegrees(draft.BeltDegreesText),
            Principles: SiteContentRules.ParseLines(draft.PrinciplesText),
            Eras: SiteContentRules.ParseLines(draft.ErasText),
            Now: SiteContentRules.ParseLines(draft.NowText),
            OwnerPhotoFlipAlt: SiteContentRules.NormalizeField(draft.OwnerPhotoFlipAlt));

        try
        {
            await UpsertAsync(values);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Loser of a concurrent first save: the row exists now, so one
            // retry lands on the update path (last write wins).
            await UpsertAsync(values);
        }

        // Bump the version before clearing so an in-flight reader can tell its
        // snapshot predates this save and must not repopulate the cache.
        Interlocked.Increment(ref _version);
        _cache = null;
    }

    private async Task UpsertAsync(SiteContentValues values)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.SiteContents
            .FirstOrDefaultAsync(c => c.Id == SiteContent.SingletonId);
        if (row is null)
        {
            row = new SiteContent { Id = SiteContent.SingletonId };
            db.SiteContents.Add(row);
        }

        row.HeroHeading = values.HeroHeading;
        row.Tagline = values.Tagline;
        row.About = values.About;
        row.Skills = values.Skills;
        row.OwnerPhotoAlt = values.OwnerPhotoAlt;
        row.HeroEyebrow = values.HeroEyebrow;
        row.GamePlan = values.GamePlan;
        row.BeltCaption = values.BeltCaption;
        row.BeltDegrees = values.BeltDegrees;
        row.Principles = values.Principles;
        row.Eras = values.Eras;
        row.Now = values.Now;
        row.OwnerPhotoFlipAlt = values.OwnerPhotoFlipAlt;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
