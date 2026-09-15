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

    /// <summary>The value with control characters replaced by underscores and cut to <paramref name="maxLength"/>; empty for null.</summary>
    public static string Value(string? value, int maxLength = MaxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // The two Replace calls are the line-break removal CodeQL recognizes as
        // a sanitizer; the loop below covers the remaining control characters
        // (tabs, escapes, DEL) the same way.
        var text = value
            .Replace("\r", "_", StringComparison.Ordinal)
            .Replace("\n", "_", StringComparison.Ordinal);
        if (text.Length > maxLength)
        {
            text = text[..maxLength];
        }

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
