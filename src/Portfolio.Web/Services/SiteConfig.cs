using System.Globalization;

namespace Portfolio.Web.Services;

/// <summary>
/// Site personalization loaded from environment variables so the same image
/// works for any owner. Required keys fail fast at startup with one message
/// listing everything that's missing.
/// </summary>
public record SiteConfig(
    string OwnerName,
    string SiteTitle,
    string Tagline,
    string? MetaDescription,
    string ContactEmail,
    string? ContactPhone,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? About,
    IReadOnlyList<string> Skills,
    string? SponsorUrl,
    string SponsorText,
    string? ResumeFile = null,
    string? OwnerPhotoFile = null,
    string? OwnerPhotoAlt = null,
    // -- BJJ landing flavor (Unit 10); all optional, blank = null/empty. --
    SiteFlavor Flavor = SiteFlavor.Default,
    string? HeroEyebrow = null,
    IReadOnlyList<string>? GamePlanLines = null,
    string? BeltCaption = null,
    int? BeltDegrees = null,
    IReadOnlyList<string>? PrincipleLines = null,
    IReadOnlyList<string>? EraLines = null,
    IReadOnlyList<string>? NowLines = null)
{
    public static SiteConfig FromConfiguration(IConfiguration config)
    {
        var missing = new List<string>();
        string Require(string key)
        {
            var value = config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add(key);
            }
            return value ?? string.Empty;
        }

        var ownerName = Require("SITE_OWNER_NAME");
        var contactEmail = Require("CONTACT_EMAIL");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required environment variables: {string.Join(", ", missing)}. " +
                "Copy .env.example to .env and fill them in.");
        }

        var skills = (config["SITE_SKILLS"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new SiteConfig(
            OwnerName: ownerName,
            SiteTitle: config["SITE_TITLE"] ?? $"{ownerName} — Portfolio",
            Tagline: config["SITE_TAGLINE"] ?? string.Empty,
            // Search/social snippet; pages fall back to the tagline when unset.
            MetaDescription: NullIfEmpty(config["SITE_META_DESCRIPTION"]),
            ContactEmail: contactEmail,
            ContactPhone: NullIfEmpty(config["CONTACT_PHONE"]),
            LinkedInUrl: NullIfEmpty(config["LINKEDIN_URL"]),
            GitHubUrl: NullIfEmpty(config["GITHUB_URL"]),
            // .env files can't hold real newlines, so a literal "\n" splits paragraphs.
            About: NullIfEmpty(config["SITE_ABOUT"])?.Replace("\\n", "\n"),
            Skills: skills,
            SponsorUrl: NullIfEmpty(config["SPONSOR_URL"]),
            SponsorText: NullIfEmpty(config["SPONSOR_TEXT"]) ?? "Buy me a coffee",
            // Path to a PDF served at /resume; the endpoint and links only
            // exist when this is set.
            ResumeFile: NullIfEmpty(config["RESUME_FILE"]),
            // Path to the Owner Photo served at /owner-photo and shown on the
            // landing hero; blank = the hero renders photo-less.
            OwnerPhotoFile: NullIfEmpty(config["OWNER_PHOTO_FILE"]),
            OwnerPhotoAlt: NullIfEmpty(config["OWNER_PHOTO_ALT"]),
            Flavor: SiteFlavorRules.Parse(config["SITE_FLAVOR"]),
            HeroEyebrow: NullIfEmpty(config["SITE_HERO_EYEBROW"]),
            GamePlanLines: SplitEnvLines(config["SITE_GAME_PLAN"]),
            BeltCaption: NullIfEmpty(config["SITE_BELT_CAPTION"]),
            BeltDegrees: ParseDegrees(config["SITE_BELT_DEGREES"]),
            PrincipleLines: SplitEnvLines(config["SITE_PRINCIPLES"]),
            EraLines: SplitEnvLines(config["SITE_ERAS"]),
            NowLines: SplitEnvLines(config["SITE_NOW"]));
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    // .env files can't hold real newlines, so — like SITE_ABOUT — a literal
    // "\n" separates lines; BjjRules.SplitLines then trims and drops blanks.
    private static IReadOnlyList<string> SplitEnvLines(string? value)
        => BjjRules.SplitLines(NullIfEmpty(value)?.Replace("\\n", "\n"));

    // Unparsable text is ignored (never throws at startup); range-checking
    // against MaxDegrees happens later, at resolve (BjjRules.ClampDegrees).
    private static int? ParseDegrees(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var degrees)
            ? degrees
            : null;
}
