namespace Portfolio.Web.Services;

public static class ProjectUrlRules
{
    /// <summary>Normalizes an optional project URL; returns false with an error unless empty or absolute http(s).</summary>
    public static bool TryNormalize(string? url, string fieldName, out string? normalized, out string? error)
    {
        normalized = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        if (normalized is null)
        {
            error = null;
            return true;
        }

        if (!IsHttp(normalized))
        {
            normalized = null;
            error = $"{fieldName} must be a full http:// or https:// URL.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Render guard for stored URLs that predate save-time validation.</summary>
    public static bool IsHttp(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
