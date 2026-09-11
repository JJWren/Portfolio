using System.Globalization;

namespace Portfolio.Web.Services;

/// <summary>
/// Pure rules for the résumé: the upload size cap, the PDF signature check,
/// and the two link origins that may be recorded as a download's target.
/// </summary>
public static class ResumeRules
{
    /// <summary>Upload cap, the same figure as the photo and image caps.</summary>
    public const int MaxMegabytes = 5;
    public const long MaxBytes = MaxMegabytes * 1024L * 1024L;

    /// <summary>The admin editor's message for an upload over the cap, derived
    /// from the constant so the wording can't drift from the limit.</summary>
    public static string TooLargeMessage { get; } = $"That file is over {MaxMegabytes} MB.";

    /// <summary>Query string key the Contact and footer links use to name
    /// where a download started.</summary>
    public const string OriginQueryKey = "from";

    public const string FooterOrigin = "footer";
    public const string ContactOrigin = "contact";

    /// <summary>
    /// True when the header starts with the PDF signature ("%PDF-") — the
    /// host-copy workflow means the file extension and any declared content
    /// type can't be trusted, so the bytes decide instead.
    /// </summary>
    public static bool IsPdf(ReadOnlySpan<byte> header)
        => header.Length >= 5 && header[..5].SequenceEqual("%PDF-"u8);

    /// <summary>
    /// Exact, ordinal match against the two known origins; anything else —
    /// including null, empty, differently cased or padded values — is null.
    /// Only an allowlisted value ever reaches the database as an event target.
    /// </summary>
    public static string? ParseOrigin(string? value)
        => value is FooterOrigin or ContactOrigin ? value : null;

    /// <summary>Human-readable size for the admin editor: whole bytes below
    /// 1 KB, a rounded whole number of KB below 1 MB, otherwise MB to one
    /// decimal place.</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{(bytes / 1024.0).ToString("F0", CultureInfo.InvariantCulture)} KB";
        }

        return $"{(bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture)} MB";
    }
}
