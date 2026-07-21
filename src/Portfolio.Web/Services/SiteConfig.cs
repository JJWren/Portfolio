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
    string ContactEmail,
    string? ContactPhone,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? About,
    IReadOnlyList<string> Skills,
    string? SponsorUrl,
    string SponsorText)
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
            ContactEmail: contactEmail,
            ContactPhone: NullIfEmpty(config["CONTACT_PHONE"]),
            LinkedInUrl: NullIfEmpty(config["LINKEDIN_URL"]),
            GitHubUrl: NullIfEmpty(config["GITHUB_URL"]),
            // .env files can't hold real newlines, so a literal "\n" splits paragraphs.
            About: NullIfEmpty(config["SITE_ABOUT"])?.Replace("\\n", "\n"),
            Skills: skills,
            SponsorUrl: NullIfEmpty(config["SPONSOR_URL"]),
            SponsorText: NullIfEmpty(config["SPONSOR_TEXT"]) ?? "Buy me a coffee");
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
