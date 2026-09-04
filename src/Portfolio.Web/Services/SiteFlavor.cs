namespace Portfolio.Web.Services;

/// <summary>
/// Which landing-page flavor to render. Startup configuration only
/// (SITE_FLAVOR) and never admin-editable: it decides which markup and CSS
/// ship, not which words appear (domain-entities.md section 1).
/// </summary>
public enum SiteFlavor
{
    Default,
    Bjj,
}

/// <summary>
/// Parsing for <see cref="SiteFlavor"/>. A separate static class rather than
/// a static member on the enum itself (C# enums can't host static members);
/// follows this codebase's Foo/FooRules split (SiteContentRules, ThemeRules).
/// </summary>
public static class SiteFlavorRules
{
    /// <summary>
    /// Trimmed, case-insensitive "bjj" maps to <see cref="SiteFlavor.Bjj"/>;
    /// blank or any other value falls back to <see cref="SiteFlavor.Default"/>
    /// so an unrecognized SITE_FLAVOR can never take the plain landing page
    /// down (decision 1 in the handoff).
    /// </summary>
    public static SiteFlavor Parse(string? value)
        => string.Equals(value?.Trim(), "bjj", StringComparison.OrdinalIgnoreCase)
            ? SiteFlavor.Bjj
            : SiteFlavor.Default;
}
