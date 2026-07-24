using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>Landing copy with admin overrides resolved against the .env fallbacks.</summary>
public record EffectiveSiteContent(
    string HeroHeading,
    string Tagline,
    string? About,
    IReadOnlyList<string> Skills);

/// <summary>
/// Rules for the admin site-content overrides. Blank input means "use the
/// .env value" and is stored as null; there is no way to force-blank a
/// non-empty .env value.
/// </summary>
public static class SiteContentRules
{
    // Single source for the SiteContent column sizes AppDbContext applies;
    // changing one is a schema change and needs a migration.
    public const int HeroHeadingMaxLength = 120;
    public const int TaglineMaxLength = 200;
    public const int AboutMaxLength = 4000;

    /// <summary>Trims and normalizes line endings; whitespace-only collapses to null.</summary>
    public static string? NormalizeField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    /// <summary>One skill per line, trimmed, blanks dropped; no content means null.</summary>
    public static List<string>? ParseSkills(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var skills = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return skills.Count > 0 ? skills : null;
    }

    /// <summary>Round-trips stored skills back into the one-per-line textarea.</summary>
    public static string SkillsText(IReadOnlyList<string>? skills)
        => skills is null ? string.Empty : string.Join('\n', skills);

    /// <summary>Returns a friendly error for the first field over its stored size, or null when everything fits.</summary>
    public static string? CheckLengths(string? heroHeading, string? tagline, string? about)
    {
        if (heroHeading is not null && heroHeading.Length > HeroHeadingMaxLength)
        {
            return $"Hero heading is limited to {HeroHeadingMaxLength} characters (yours is {heroHeading.Length}).";
        }

        if (tagline is not null && tagline.Length > TaglineMaxLength)
        {
            return $"Tagline is limited to {TaglineMaxLength} characters (yours is {tagline.Length}).";
        }

        if (about is not null && about.Length > AboutMaxLength)
        {
            return $"About is limited to {AboutMaxLength} characters (yours is {about.Length}).";
        }

        return null;
    }

    /// <summary>Overrides win per field; null (or an empty skills list) falls back to .env.</summary>
    public static EffectiveSiteContent Resolve(SiteConfig site, SiteContent? overrides)
        => new(
            overrides?.HeroHeading ?? site.OwnerName,
            overrides?.Tagline ?? site.Tagline,
            overrides?.About ?? site.About,
            // Copy: the resolved snapshot gets cached and must not alias the
            // (mutable) entity list.
            overrides?.Skills is { Count: > 0 } skills ? skills.ToArray() : site.Skills);
}
