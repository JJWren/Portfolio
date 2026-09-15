namespace Portfolio.Web.Services;

/// <summary>
/// The one runtime-configurable security setting (FR-D5), resolved once at
/// startup and registered as a singleton next to <see cref="SiteConfig"/>.
/// </summary>
/// <param name="CspMode">Which Content-Security-Policy header, if any, <c>SecurityHeadersMiddleware</c> sends; the other headers are unaffected.</param>
public sealed record SecurityOptions(CspMode CspMode)
{
    /// <summary>Reads <c>SECURITY_CSP_MODE</c>; see <see cref="SecurityHeadersRules.ParseCspMode"/> for its rules.</summary>
    public static SecurityOptions FromConfiguration(IConfiguration config)
        => new(SecurityHeadersRules.ParseCspMode(config["SECURITY_CSP_MODE"]));
}
