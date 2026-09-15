namespace Portfolio.Web.Services;

/// <summary>
/// How the Content-Security-Policy header is emitted, from
/// <c>SECURITY_CSP_MODE</c> (FR-D5): enforced by default, sent as
/// <c>Content-Security-Policy-Report-Only</c> for a dry run with the browser
/// console as the report, or withheld entirely as a rollback escape hatch.
/// The other security headers of <see cref="SecurityHeadersRules.Compose"/>
/// are always sent regardless of this mode.
/// </summary>
public enum CspMode
{
    Enforce,
    ReportOnly,
    Off,
}

/// <summary>
/// Pure composer for every security response header this application sends
/// (FR-D1, FR-D2, FR-D4): a header/directive set, unit-tested value by
/// value, with no dependency on the request pipeline.
/// <c>SecurityHeadersMiddleware</c> is the only caller in production code.
/// </summary>
public static class SecurityHeadersRules
{
    /// <summary>
    /// FR-D1: every powerful browser feature the site never uses, denied
    /// outright so an injected script or a framed third party can never ask
    /// for it.
    /// </summary>
    public const string PermissionsPolicy =
        "accelerometer=(), browsing-topics=(), camera=(), display-capture=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), screen-wake-lock=(), usb=(), xr-spatial-tracking=()";

    /// <summary>
    /// FR-D4: the restrictive policy for <c>/uploads/*</c> (user-supplied
    /// files, including SVG validated by extension only), set by
    /// <c>Program.cs</c>'s static-file <c>OnPrepareResponse</c> next to
    /// <c>Cache-Control</c>. <c>sandbox</c> without <c>allow-scripts</c> plus
    /// <c>default-src 'none'</c> means a scripted SVG opened directly cannot
    /// run script; <c>style-src 'unsafe-inline'</c> still lets its own
    /// styles render.
    /// </summary>
    public const string UploadsCsp = "default-src 'none'; style-src 'unsafe-inline'; sandbox";

    /// <summary>
    /// Parses <c>SECURITY_CSP_MODE</c>: "report-only" and "off" (trimmed,
    /// case-insensitive) select those modes; every other value, blank or
    /// unrecognized included, is "enforce" — a typo in the setting must
    /// never silently turn the policy off.
    /// </summary>
    public static CspMode ParseCspMode(string? value)
    {
        var trimmed = value?.Trim();
        if (string.Equals(trimmed, "report-only", StringComparison.OrdinalIgnoreCase))
        {
            return CspMode.ReportOnly;
        }

        if (string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase))
        {
            return CspMode.Off;
        }

        return CspMode.Enforce;
    }

    /// <summary>
    /// FR-D2, FR-D3: the Content-Security-Policy directives, joined with
    /// <c>"; "</c> in a fixed order. <paramref name="styleHash"/> — already
    /// in the form <c>sha256-&lt;base64&gt;</c>, as
    /// <c>ThemeRules.BuildSnapshot</c> produces it — is appended to
    /// <c>style-src</c>, quoted, only when the current theme snapshot
    /// carries an override block; every other page's <c>style-src</c> names
    /// only <c>'self'</c>, with no <c>'unsafe-inline'</c> anywhere.
    /// </summary>
    public static string BuildCsp(string? styleHash)
    {
        var styleSrc = string.IsNullOrEmpty(styleHash)
            ? "style-src 'self'"
            : $"style-src 'self' '{styleHash}'";

        string[] directives =
        [
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "form-action 'self'",
            "script-src 'self'",
            styleSrc,
            "img-src 'self' blob: https:",
            "font-src 'self'",
            "connect-src 'self'",
            "manifest-src 'self'",
        ];

        return string.Join("; ", directives);
    }

    /// <summary>
    /// FR-D1, FR-D2, FR-D5: the ordered header set for a page response.
    /// <c>SecurityHeadersMiddleware</c> sets each pair only when the
    /// response does not already carry that header, so a response that set
    /// its own policy earlier (the <c>/uploads</c> static-file middleware,
    /// with <see cref="UploadsCsp"/>) keeps it.
    /// </summary>
    public static IReadOnlyList<(string Name, string Value)> Compose(CspMode mode, string? styleHash)
    {
        var headers = new List<(string Name, string Value)>
        {
            ("X-Content-Type-Options", "nosniff"),
            ("Referrer-Policy", "strict-origin-when-cross-origin"),
            ("X-Frame-Options", "DENY"),
            ("Permissions-Policy", PermissionsPolicy),
            ("Cross-Origin-Opener-Policy", "same-origin"),
        };

        switch (mode)
        {
            case CspMode.Enforce:
                headers.Add(("Content-Security-Policy", BuildCsp(styleHash)));
                break;
            case CspMode.ReportOnly:
                headers.Add(("Content-Security-Policy-Report-Only", BuildCsp(styleHash)));
                break;
            case CspMode.Off:
                // FR-D5: the rollback escape hatch. The other headers above
                // are still sent; only the CSP entry is withheld.
                break;
        }

        return headers;
    }
}
