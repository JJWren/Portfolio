using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class SiteContentService(IDbContextFactory<AppDbContext> dbFactory, SiteConfig site)
{
    // Single-container deploy, so an in-process cache is safe; SaveAsync clears it.
    private volatile EffectiveSiteContent? _cache;

    /// <summary>Resolved landing copy — DB overrides over .env values, cached until the next save.</summary>
    public async Task<EffectiveSiteContent> GetEffectiveAsync()
    {
        if (_cache is { } cached)
        {
            return cached;
        }

        var resolved = SiteContentRules.Resolve(site, await GetOverridesAsync());
        _cache = resolved;
        return resolved;
    }

    /// <summary>Raw override row for the admin form; null when nothing has been saved yet.</summary>
    public async Task<SiteContent?> GetOverridesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.SiteContents.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == SiteContent.SingletonId);
    }

    /// <summary>Normalizes each field (blank → null → .env fallback) and upserts the single row.</summary>
    public async Task SaveAsync(string? heroHeading, string? tagline, string? about, string? skillsText)
    {
        var normalizedHeroHeading = SiteContentRules.NormalizeField(heroHeading);
        var normalizedTagline = SiteContentRules.NormalizeField(tagline);
        var normalizedAbout = SiteContentRules.NormalizeField(about);
        var skills = SiteContentRules.ParseSkills(skillsText);

        try
        {
            await UpsertAsync(normalizedHeroHeading, normalizedTagline, normalizedAbout, skills);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Loser of a concurrent first save: the row exists now, so one
            // retry lands on the update path (last write wins).
            await UpsertAsync(normalizedHeroHeading, normalizedTagline, normalizedAbout, skills);
        }

        _cache = null;
    }

    private async Task UpsertAsync(string? heroHeading, string? tagline, string? about, List<string>? skills)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.SiteContents
            .FirstOrDefaultAsync(c => c.Id == SiteContent.SingletonId);
        if (row is null)
        {
            row = new SiteContent { Id = SiteContent.SingletonId };
            db.SiteContents.Add(row);
        }

        row.HeroHeading = heroHeading;
        row.Tagline = tagline;
        row.About = about;
        row.Skills = skills;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
