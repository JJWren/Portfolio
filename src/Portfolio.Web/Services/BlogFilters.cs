using System.Globalization;

namespace Portfolio.Web.Services;

/// <summary>Normalization helpers for the blog list's querystring filters.</summary>
public static class BlogFilters
{
    /// <summary>Blank/whitespace collapses to null (no filter).</summary>
    public static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Parses a "yyyy-MM" month into a UTC [start, end) window.</summary>
    public static bool TryParseMonth(string? month, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        if (string.IsNullOrWhiteSpace(month)
            || !DateTime.TryParseExact(month.Trim(), "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        startUtc = new DateTime(parsed.Year, parsed.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        endUtc = startUtc.AddMonths(1);
        return true;
    }

    /// <summary>Escapes LIKE wildcards so user input matches literally (escape char '\').</summary>
    public static string EscapeLike(string input)
        => input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
