using System.Text.Json;

namespace Portfolio.Web.Services;

/// <summary>
/// Pure rules for search and social metadata: canonical URL shapes, meta
/// description truncation, and JSON-LD payloads. JSON-LD is serialized with
/// the default encoder, which escapes every HTML-sensitive character into
/// \uXXXX sequences — the output can never close a script block early, so it
/// is safe to emit via MarkupString by construction.
/// </summary>
public static class SeoRules
{
    /// <summary>Longest text emitted into description meta tags.</summary>
    public const int DescriptionLimit = 160;

    /// <summary>
    /// PUBLIC_BASE_URL when configured (the canonical origin), otherwise the
    /// request origin; never with a trailing slash.
    /// </summary>
    public static string CanonicalOrigin(string? publicBaseUrl, string requestOrigin)
        => (string.IsNullOrWhiteSpace(publicBaseUrl) ? requestOrigin : publicBaseUrl).Trim().TrimEnd('/');

    /// <summary>
    /// Canonical absolute URL for a request path: no query, no trailing
    /// slash. The root canonicalizes to the bare origin, matching the
    /// sitemap's entries.
    /// </summary>
    public static string CanonicalUrl(string origin, string? path)
    {
        var trimmed = (path ?? string.Empty).Trim().TrimEnd('/');
        if (trimmed.Length == 0)
        {
            return origin;
        }

        return trimmed[0] == '/' ? origin + trimmed : $"{origin}/{trimmed}";
    }

    /// <summary>
    /// Absolute URL for a possibly site-relative path — Open Graph consumers
    /// require absolute image URLs. Absolute http(s) values pass through.
    /// </summary>
    public static string AbsoluteUrl(string origin, string pathOrUrl)
        => pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? pathOrUrl
                : CanonicalUrl(origin, pathOrUrl);

    /// <summary>
    /// Single-line description for meta tags: whitespace collapses, and text
    /// over the limit is cut at a word boundary with a trailing ellipsis.
    /// </summary>
    public static string TruncateDescription(string? text, int limit = DescriptionLimit)
    {
        // Below 2 there is no room for even one character plus the ellipsis.
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 2);

        var collapsed = string.Join(' ',
            (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length <= limit)
        {
            return collapsed;
        }

        var cut = collapsed.LastIndexOf(' ', limit - 1);
        var head = cut > 0 ? collapsed[..cut] : collapsed[..(limit - 1)];
        return head + "…";
    }

    /// <summary>
    /// Alt text ready for rendering: trimmed, or empty (decorative) when
    /// null/whitespace — guards values that bypassed the editors' trim,
    /// e.g. legacy rows or direct database writes.
    /// </summary>
    public static string NormalizeAltText(string? altText)
        => string.IsNullOrWhiteSpace(altText) ? string.Empty : altText.Trim();

    /// <summary>schema.org Person for the landing page.</summary>
    public static string PersonJsonLd(string name, string url, IEnumerable<string?> sameAs, string? imageUrl = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Person",
            ["name"] = name,
            ["url"] = url,
        };

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            payload["image"] = imageUrl;
        }

        var links = sameAs.OfType<string>()
            .Where(static link => !string.IsNullOrWhiteSpace(link))
            .ToArray();
        if (links.Length > 0)
        {
            payload["sameAs"] = links;
        }

        return JsonSerializer.Serialize(payload);
    }

    /// <summary>schema.org WebSite for the landing page.</summary>
    public static string WebSiteJsonLd(string name, string url)
        => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = name,
            ["url"] = url,
        });

    /// <summary>schema.org BlogPosting for a published post page.</summary>
    public static string BlogPostingJsonLd(
        string headline,
        string description,
        string url,
        string authorName,
        DateTime? published,
        DateTime? modified,
        string? imageUrl)
    {
        var payload = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = headline,
            ["description"] = description,
            ["url"] = url,
            ["mainEntityOfPage"] = url,
            ["author"] = new Dictionary<string, object>
            {
                ["@type"] = "Person",
                ["name"] = authorName,
            },
        };

        if (published is { } publishedAt)
        {
            payload["datePublished"] = publishedAt.ToString("o");
        }

        if ((modified ?? published) is { } modifiedAt)
        {
            payload["dateModified"] = modifiedAt.ToString("o");
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            payload["image"] = imageUrl;
        }

        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Web-app manifest JSON — name/short_name from site config, colors from
    /// the effective theme; icon paths are fixed wwwroot assets.
    /// </summary>
    public static string WebManifest(string name, string shortName, string themeColor)
        => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["name"] = name,
            ["short_name"] = shortName,
            ["start_url"] = "/",
            ["display"] = "standalone",
            ["background_color"] = themeColor,
            ["theme_color"] = themeColor,
            ["icons"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["src"] = "/favicon-192.png",
                    ["sizes"] = "192x192",
                    ["type"] = "image/png",
                },
                new Dictionary<string, object>
                {
                    ["src"] = "/favicon-512.png",
                    ["sizes"] = "512x512",
                    ["type"] = "image/png",
                },
            },
        });
}
