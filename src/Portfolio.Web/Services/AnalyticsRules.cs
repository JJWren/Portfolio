namespace Portfolio.Web.Services;

/// <summary>Pure rules for what gets counted and how values are normalized.</summary>
public static class AnalyticsRules
{
    public const int PathMaxLength = 300;
    public const int ReferrerMaxLength = 200;
    public const int VisitorKeyLength = 64;
    public const int EventNameMaxLength = 40;
    public const int EventTargetMaxLength = 300;

    public const string ProjectClickEvent = "project-click";
    public const string ResumeDownloadEvent = "resume-download";
    public const string ContactSubmitEvent = "contact-submit";

    private static readonly string[] ExcludedPrefixes =
    [
        "/admin", "/auth", "/signin", "/go", "/resume", "/uploads",
        "/healthz", "/_blazor", "/_framework", "/not-found", "/Error",
    ];

    private static readonly string[] BotUserAgentFragments =
    [
        "bot", "crawl", "spider", "slurp", "curl", "wget", "python",
        "httpclient", "headless", "lighthouse", "preview",
        "facebookexternalhit", "monitor",
    ];

    /// <summary>Public HTML pages only — framework, auth, admin, asset, and
    /// tracked-endpoint paths are excluded (the /go and /resume endpoints
    /// record their own events instead).</summary>
    public static bool IsCountablePath(PathString path)
    {
        var value = path.Value ?? "/";
        foreach (var prefix in ExcludedPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // A dot in the last segment means a file (feed.xml, robots.txt, …).
        var lastSlash = value.LastIndexOf('/');
        return !value[(lastSlash + 1)..].Contains('.');
    }

    /// <summary>An empty User-Agent is treated as a bot — every real browser sends one.</summary>
    public static bool IsBot(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return true;
        }

        foreach (var fragment in BotUserAgentFragments)
        {
            if (userAgent.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Do Not Track / Global Privacy Control: honored by not recording at all.</summary>
    public static bool OptedOut(IHeaderDictionary headers)
        => headers["DNT"] == "1" || headers["Sec-GPC"] == "1";

    /// <summary>Reduces a Referer header to its host; null for direct visits,
    /// internal navigation, or anything unparsable.</summary>
    public static string? NormalizeReferrer(string? referrer, HostString ownHost)
    {
        if (string.IsNullOrWhiteSpace(referrer)
            || !Uri.TryCreate(referrer, UriKind.Absolute, out var uri)
            || uri.Host.Length == 0)
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        return string.Equals(host, ownHost.Host, StringComparison.OrdinalIgnoreCase) ? null : host;
    }

    public static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
