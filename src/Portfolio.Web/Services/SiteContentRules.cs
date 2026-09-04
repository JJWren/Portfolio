using System.Globalization;
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
    IReadOnlyList<Principle>? Principles = null,
    IReadOnlyList<Era>? Eras = null,
    IReadOnlyList<NowItem>? Now = null,
    // -- Second owner-photo slot (Unit 10 Phase 4). --
    string? OwnerPhotoFlipAlt = null)
{
    /// <summary>Never null — empty means "no chart" (BR-2). The nullable
    /// constructor parameter only exists so callers that predate the BJJ
    /// flavor can omit it.</summary>
    public IReadOnlyList<GamePlanNode> GamePlan { get; init; } = GamePlan ?? [];

    /// <summary>Never null — empty means "no Principles section" (BR-2).</summary>
    public IReadOnlyList<Principle> Principles { get; init; } = Principles ?? [];

    /// <summary>Never null — empty means "no road" (ladder and table, BR-2).</summary>
    public IReadOnlyList<Era> Eras { get; init; } = Eras ?? [];

    /// <summary>One per distinct belt in <see cref="Eras"/>, first-appearance
    /// order, each carrying the stripes of that belt's last era (BR-8).
    /// Derived from <see cref="Eras"/>, not independently resolved or
    /// stored — but, like <see cref="GamePlan"/>, <see cref="Principles"/>,
    /// <see cref="Eras"/> and <see cref="Now"/>, computed once at
    /// construction (init-assigned from the primary constructor's Eras
    /// parameter) rather than recomputed on every access.</summary>
    public IReadOnlyList<Rung> Rungs { get; init; } = BjjRules.Rungs(Eras ?? []);

    /// <summary>Never null — empty means "no Now section" (BR-2).</summary>
    public IReadOnlyList<NowItem> Now { get; init; } = Now ?? [];

    /// <summary>Never null. <see cref="SiteContentRules.Resolve"/> fills it
    /// like <see cref="OwnerPhotoAlt"/> (override, then the env value, then
    /// "Portrait of {owner}"); a direct construction that omits the parameter
    /// gets an empty string, not that fallback. The nullable constructor
    /// parameter only exists so callers that predate Phase 4 (tests, other
    /// constructors) keep compiling.</summary>
    public string OwnerPhotoFlipAlt { get; init; } = OwnerPhotoFlipAlt ?? string.Empty;
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
    string? PrinciplesText,
    string? ErasText,
    string? NowText,
    string? OwnerPhotoFlipAlt);

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
    /// principles); ParseSkills is this under its original name. Delegates
    /// to <see cref="BjjRules.SplitLines"/> so the admin-textarea splitter
    /// and the env-value splitter share one implementation.</summary>
    public static List<string>? ParseLines(string? text)
    {
        var lines = BjjRules.SplitLines(text);
        return lines.Count > 0 ? lines.ToList() : null;
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
        => int.TryParse(NormalizeField(text), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    /// <summary>Returns a friendly error for the first field over its stored size, or null when everything fits.</summary>
    public static string? CheckLengths(
        string? heroHeading, string? tagline, string? about, string? ownerPhotoAlt = null, string? ownerPhotoFlipAlt = null)
    {
        if (ownerPhotoAlt is not null && ownerPhotoAlt.Length > OwnerPhotoAltMaxLength)
        {
            return $"Photo alt text is limited to {OwnerPhotoAltMaxLength} characters (yours is {ownerPhotoAlt.Length}).";
        }

        // Same 200-char limit as the primary photo's alt text (Unit 10 Phase 4).
        if (ownerPhotoFlipAlt is not null && ownerPhotoFlipAlt.Length > OwnerPhotoAltMaxLength)
        {
            return $"Mat photo alt text is limited to {OwnerPhotoAltMaxLength} characters (yours is {ownerPhotoFlipAlt.Length}).";
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
    /// length checks first (CheckLengths, for the pre-existing fields), then
    /// the BJJ-flavor format checks (BR-4 "strict at save") in the editor's
    /// own field order — hero eyebrow, game plan, belt caption, belt
    /// degrees, principles, eras, now — so the first error reported is
    /// always the first invalid field the admin would scroll to. The BR-9
    /// degrees-vs-eras cross-check runs last, once both fields it compares
    /// have individually validated, and against the EFFECTIVE values (draft,
    /// falling back to <paramref name="site"/>'s environment values) rather
    /// than the draft's own text alone — the editor seeds its textareas from
    /// the DB override, so a field left blank because it defers to
    /// SITE_BELT_DEGREES / SITE_ERAS would otherwise let a real disagreement
    /// between the two save unblocked. <paramref name="site"/> is optional
    /// (defaults to null, keeping every pre-existing call site compiling);
    /// without it the cross-check runs on the draft alone, same as before.
    /// Returns the first friendly error, or null when the whole draft may be
    /// saved.
    /// </summary>
    public static string? Validate(SiteContentDraft draft, SiteConfig? site = null)
    {
        var lengthError = CheckLengths(
            NormalizeField(draft.HeroHeading),
            NormalizeField(draft.Tagline),
            NormalizeField(draft.About),
            NormalizeField(draft.OwnerPhotoAlt),
            NormalizeField(draft.OwnerPhotoFlipAlt));
        if (lengthError is not null)
        {
            return lengthError;
        }

        var heroEyebrow = NormalizeField(draft.HeroEyebrow);
        if (heroEyebrow is not null && heroEyebrow.Length > HeroEyebrowMaxLength)
        {
            return $"Hero eyebrow is limited to {HeroEyebrowMaxLength} characters (yours is {heroEyebrow.Length}).";
        }

        var gamePlanError = BjjRules.ValidateGamePlan(ParseLines(draft.GamePlanText) ?? []);
        if (gamePlanError is not null)
        {
            return gamePlanError;
        }

        var beltCaption = NormalizeField(draft.BeltCaption);
        if (beltCaption is not null && beltCaption.Length > BeltCaptionMaxLength)
        {
            return $"Belt caption is limited to {BeltCaptionMaxLength} characters (yours is {beltCaption.Length}).";
        }

        // Parsed once into a local and branched on blank / not-a-number /
        // out-of-range, rather than parsing the same text twice. Hoisted
        // above the if so the BR-9 cross-check below can reuse the parsed
        // value instead of re-parsing beltDegreesText a second time.
        var beltDegreesText = NormalizeField(draft.BeltDegreesText);
        int? beltDegrees = null;
        if (beltDegreesText is not null)
        {
            if (!int.TryParse(beltDegreesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDegrees))
            {
                return "Belt degrees must be a whole number.";
            }

            beltDegrees = parsedDegrees;
            var degreesError = BjjRules.ValidateDegrees(beltDegrees);
            if (degreesError is not null)
            {
                return degreesError;
            }
        }

        var principlesError = BjjRules.ValidatePrinciples(ParseLines(draft.PrinciplesText) ?? []);
        if (principlesError is not null)
        {
            return principlesError;
        }

        var eraLines = ParseLines(draft.ErasText) ?? [];
        var erasError = BjjRules.ValidateEras(eraLines);
        if (erasError is not null)
        {
            return erasError;
        }

        var nowError = BjjRules.ValidateNow(ParseLines(draft.NowText) ?? []);
        if (nowError is not null)
        {
            return nowError;
        }

        // Only when the draft supplies at least one of the two fields: when
        // both are blank the save touches neither fact, so it must not be
        // blocked by an environment-only disagreement between them (see the
        // summary above).
        if (beltDegreesText is not null || eraLines.Count > 0)
        {
            var effectiveDegrees = beltDegrees ?? site?.BeltDegrees;
            var effectiveEraLines = eraLines.Count > 0 ? eraLines : (site?.EraLines ?? []);

            // Named only on the side that fell back to the environment — the
            // other side came from the draft itself, same as always.
            var degreesSource = beltDegreesText is null ? "SITE_BELT_DEGREES" : null;
            var erasSource = eraLines.Count > 0 ? null : "SITE_ERAS";

            var degreesVsErasError = BjjRules.ValidateDegreesAgainstEras(
                effectiveDegrees, BjjRules.ParseEras(effectiveEraLines), degreesSource, erasSource);
            if (degreesVsErasError is not null)
            {
                return degreesVsErasError;
            }
        }

        return null;
    }

    /// <summary>Bounds a resolved value to at most <paramref name="maxLength"/>
    /// characters, leniently truncating rather than rejecting — Resolve must
    /// never throw on an oversized env or stored value the way Validate
    /// would refuse it at save time (BR-4).</summary>
    private static string? Truncate(string? value, int maxLength)
        => value is null || value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>Overrides win per field; null (or an empty skills/text[] list) falls back to .env (BR-3).
    /// Every BJJ field is additionally bounded the same way Validate would
    /// reject it at save, but leniently: HeroEyebrow and BeltCaption are
    /// truncated, GamePlan keeps its exactly-four-or-none rule, and
    /// Principles/Eras/Now are each capped at their MaxXxx constant (see
    /// BjjRules.Parse*) — a bad env or stored value can never take the
    /// landing page down (BR-4). Rungs is not resolved here: EffectiveSiteContent
    /// computes it once at construction, from Eras, via its own init-assigned
    /// property.</summary>
    public static EffectiveSiteContent Resolve(SiteConfig site, SiteContent? overrides)
    {
        var gamePlanLines = overrides?.GamePlan is { Count: > 0 } gamePlanOverride
            ? gamePlanOverride
            : site.GamePlanLines ?? [];
        var principleLines = overrides?.Principles is { Count: > 0 } principlesOverride
            ? principlesOverride
            : site.PrincipleLines ?? [];
        var eraLines = overrides?.Eras is { Count: > 0 } erasOverride
            ? erasOverride
            : site.EraLines ?? [];
        var nowLines = overrides?.Now is { Count: > 0 } nowOverride
            ? nowOverride
            : site.NowLines ?? [];

        return new(
            overrides?.HeroHeading ?? site.OwnerName,
            overrides?.Tagline ?? site.Tagline,
            overrides?.About ?? site.About,
            // Copy: the resolved snapshot gets cached and must not alias the
            // (mutable) entity list.
            overrides?.Skills is { Count: > 0 } skills ? skills.ToArray() : site.Skills,
            overrides?.OwnerPhotoAlt ?? site.OwnerPhotoAlt ?? $"Portrait of {site.OwnerName}",
            Truncate(overrides?.HeroEyebrow ?? site.HeroEyebrow, HeroEyebrowMaxLength),
            BjjRules.ParseGamePlan(gamePlanLines),
            Truncate(overrides?.BeltCaption ?? site.BeltCaption, BeltCaptionMaxLength),
            BjjRules.ClampDegrees(overrides?.BeltDegrees ?? site.BeltDegrees),
            BjjRules.ParsePrinciples(principleLines),
            BjjRules.ParseEras(eraLines),
            BjjRules.ParseNow(nowLines),
            overrides?.OwnerPhotoFlipAlt ?? site.OwnerPhotoFlipAlt ?? $"Portrait of {site.OwnerName}");
    }
}
