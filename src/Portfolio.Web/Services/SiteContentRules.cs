using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>Landing copy with admin overrides resolved against the .env fallbacks.</summary>
public record EffectiveSiteContent(
    string HeroHeading,
    string Tagline,
    string? About,
    IReadOnlyList<string> Skills,
    string OwnerPhotoAlt,
    // -- BJJ landing flavor (Unit 10); each member defaults so pre-Unit-10
    // call sites (tests, other constructors) keep compiling. Only rendered
    // when SiteConfig.Flavor == Bjj (BR-1); every section blank-hides (BR-2).
    string? HeroEyebrow = null,
    IReadOnlyList<GamePlanNode>? GamePlan = null,
    string? BeltCaption = null,
    int BeltDegrees = 0,
    IReadOnlyList<Principle>? Principles = null)
{
    /// <summary>Never null — empty means "no chart" (BR-2). The nullable
    /// constructor parameter only exists so callers that predate the BJJ
    /// flavor can omit it.</summary>
    public IReadOnlyList<GamePlanNode> GamePlan { get; init; } = GamePlan ?? [];

    /// <summary>Never null — empty means "no Principles section" (BR-2).</summary>
    public IReadOnlyList<Principle> Principles { get; init; } = Principles ?? [];
}

/// <summary>Every raw admin-form string for the site-content editor, one
/// field per input. <see cref="SiteContentRules.Validate"/> checks the
/// whole draft before <see cref="SiteContentService.SaveAsync(SiteContentDraft)"/>
/// persists it.</summary>
public sealed record SiteContentDraft(
    string? HeroHeading,
    string? Tagline,
    string? About,
    string? SkillsText,
    string? OwnerPhotoAlt,
    string? HeroEyebrow,
    string? GamePlanText,
    string? BeltCaption,
    string? BeltDegreesText,
    string? PrinciplesText);

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
    public const int OwnerPhotoAltMaxLength = 200;

    // BJJ landing flavor (Unit 10, BR-11).
    public const int HeroEyebrowMaxLength = 120;
    public const int BeltCaptionMaxLength = 200;

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
    public static List<string>? ParseSkills(string? text) => ParseLines(text);

    /// <summary>One entry per line, trimmed, blanks dropped; no content means
    /// null. Shared by every multi-line admin textarea (skills, game plan,
    /// principles); ParseSkills is this under its original name.</summary>
    public static List<string>? ParseLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return lines.Count > 0 ? lines : null;
    }

    /// <summary>Round-trips stored skills back into the one-per-line textarea.</summary>
    public static string SkillsText(IReadOnlyList<string>? skills) => LinesText(skills);

    /// <summary>Round-trips a stored text[] column back into a one-per-line
    /// textarea value; SkillsText is this under its original name.</summary>
    public static string LinesText(IReadOnlyList<string>? lines)
        => lines is null ? string.Empty : string.Join('\n', lines);

    /// <summary>Parses the belt-degrees form field; blank or unparsable
    /// becomes null. <see cref="Validate"/> separately rejects unparsable
    /// (non-blank) text with a friendly message before a save ever reaches
    /// this — SaveAsync trusts that gate and never throws here.</summary>
    public static int? ParseDegrees(string? text)
        => int.TryParse(NormalizeField(text), out var value) ? value : null;

    /// <summary>Returns a friendly error for the first field over its stored size, or null when everything fits.</summary>
    public static string? CheckLengths(string? heroHeading, string? tagline, string? about, string? ownerPhotoAlt = null)
    {
        if (ownerPhotoAlt is not null && ownerPhotoAlt.Length > OwnerPhotoAltMaxLength)
        {
            return $"Photo alt text is limited to {OwnerPhotoAltMaxLength} characters (yours is {ownerPhotoAlt.Length}).";
        }

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

    /// <summary>
    /// Full-draft validation for the site-content editor: the existing
    /// length checks, then the BJJ-flavor format checks (BR-4 "strict at
    /// save"). Returns the first friendly error, or null when the whole
    /// draft may be saved.
    /// </summary>
    public static string? Validate(SiteContentDraft draft)
    {
        var lengthError = CheckLengths(
            NormalizeField(draft.HeroHeading),
            NormalizeField(draft.Tagline),
            NormalizeField(draft.About),
            NormalizeField(draft.OwnerPhotoAlt));
        if (lengthError is not null)
        {
            return lengthError;
        }

        var heroEyebrow = NormalizeField(draft.HeroEyebrow);
        if (heroEyebrow is not null && heroEyebrow.Length > HeroEyebrowMaxLength)
        {
            return $"Hero eyebrow is limited to {HeroEyebrowMaxLength} characters (yours is {heroEyebrow.Length}).";
        }

        var beltCaption = NormalizeField(draft.BeltCaption);
        if (beltCaption is not null && beltCaption.Length > BeltCaptionMaxLength)
        {
            return $"Belt caption is limited to {BeltCaptionMaxLength} characters (yours is {beltCaption.Length}).";
        }

        var gamePlanError = BjjRules.ValidateGamePlan(ParseLines(draft.GamePlanText) ?? []);
        if (gamePlanError is not null)
        {
            return gamePlanError;
        }

        var principlesError = BjjRules.ValidatePrinciples(ParseLines(draft.PrinciplesText) ?? []);
        if (principlesError is not null)
        {
            return principlesError;
        }

        var beltDegreesText = NormalizeField(draft.BeltDegreesText);
        if (beltDegreesText is not null && !int.TryParse(beltDegreesText, out _))
        {
            return "Belt degrees must be a whole number.";
        }

        var degreesError = BjjRules.ValidateDegrees(ParseDegrees(draft.BeltDegreesText));
        if (degreesError is not null)
        {
            return degreesError;
        }

        return null;
    }

    /// <summary>Overrides win per field; null (or an empty skills/text[] list) falls back to .env (BR-3).</summary>
    public static EffectiveSiteContent Resolve(SiteConfig site, SiteContent? overrides)
    {
        var gamePlanLines = overrides?.GamePlan is { Count: > 0 } gamePlanOverride
            ? gamePlanOverride
            : site.GamePlanLines ?? [];
        var principleLines = overrides?.Principles is { Count: > 0 } principlesOverride
            ? principlesOverride
            : site.PrincipleLines ?? [];

        return new(
            overrides?.HeroHeading ?? site.OwnerName,
            overrides?.Tagline ?? site.Tagline,
            overrides?.About ?? site.About,
            // Copy: the resolved snapshot gets cached and must not alias the
            // (mutable) entity list.
            overrides?.Skills is { Count: > 0 } skills ? skills.ToArray() : site.Skills,
            overrides?.OwnerPhotoAlt ?? site.OwnerPhotoAlt ?? $"Portrait of {site.OwnerName}",
            overrides?.HeroEyebrow ?? site.HeroEyebrow,
            BjjRules.ParseGamePlan(gamePlanLines),
            overrides?.BeltCaption ?? site.BeltCaption,
            BjjRules.ClampDegrees(overrides?.BeltDegrees ?? site.BeltDegrees),
            BjjRules.ParsePrinciples(principleLines));
    }
}
