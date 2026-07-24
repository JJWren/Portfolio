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
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.SiteContents
            .FirstOrDefaultAsync(c => c.Id == SiteContent.SingletonId);
        if (row is null)
        {
            row = new SiteContent();
            db.SiteContents.Add(row);
        }

        row.HeroHeading = SiteContentRules.NormalizeField(heroHeading);
        row.Tagline = SiteContentRules.NormalizeField(tagline);
        row.About = SiteContentRules.NormalizeField(about);
        row.Skills = SiteContentRules.ParseSkills(skillsText);
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _cache = null;
    }
}
