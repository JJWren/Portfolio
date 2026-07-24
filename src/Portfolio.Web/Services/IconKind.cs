namespace Portfolio.Web.Services;

/// <summary>
/// Canonical glyph names for Icon.razor. Anything unrecognized falls back to
/// a generic external-link glyph, so a future OAuth provider or link type can
/// never render blank.
/// </summary>
public static class IconKind
{
    public const string Fallback = "external";

    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "github",
        "google",
        "discord",
        "linkedin",
        "email",
        "phone",
        "heart",
        Fallback,
    };

    /// <summary>Case-insensitive and trimming, so OAuth schemes ("GitHub") map directly.</summary>
    public static string Normalize(string? kind)
    {
        var normalized = kind?.Trim().ToLowerInvariant();
        return normalized is not null && Known.Contains(normalized) ? normalized : Fallback;
    }
}
