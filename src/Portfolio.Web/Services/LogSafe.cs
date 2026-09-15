namespace Portfolio.Web.Services;

/// <summary>
/// Makes a user-supplied value safe to place in a log line (a request path, a
/// sender's mail domain): line breaks and every other control character become
/// an underscore, so one request cannot forge a second log entry in the plain
/// console log, and the value is capped so a long path cannot flood it.
/// CodeQL cs/log-forging, issue #88.
/// </summary>
public static class LogSafe
{
    /// <summary>The default cap, the same length the analytics path column keeps.</summary>
    public const int MaxLength = 300;

    /// <summary>The value cut to <paramref name="maxLength"/> with every control character replaced by an underscore; empty for null.</summary>
    public static string Sanitize(string? value, int maxLength = MaxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Cap first so a long request line is never scanned in full; every
        // replacement below keeps the length, so the order does not change the
        // result. A cap that lands inside a surrogate pair drops the dangling
        // half rather than logging malformed text.
        var text = value.Length > maxLength ? value[..maxLength] : value;
        if (char.IsHighSurrogate(text[^1]))
        {
            text = text[..^1];
        }

        // The two Replace calls are the line-break removal CodeQL recognizes as
        // a sanitizer (the two-argument overload is ordinal); the loop below
        // covers the remaining control characters (tabs, escapes, DEL, the C1
        // range) the same way.
        text = text.Replace("\r", "_").Replace("\n", "_");

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsControl(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
