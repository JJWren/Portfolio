namespace Portfolio.Web.Services;

public static class ReportRules
{
    public const int MaxDetailsLength = 1000;
    public const int MaxExcerptLength = 300;
    public const string OtherReason = "Other";

    public static readonly IReadOnlyList<string> Reasons =
    [
        "Spam",
        "Harassment or bullying",
        "Hate speech",
        "Sexually explicit content",
        "Violence or threats",
        "Misinformation",
        OtherReason,
    ];

    /// <summary>Validates reason + details; details are required for "Other".</summary>
    public static bool Validate(string? reason, string? details, out string? error)
    {
        if (string.IsNullOrWhiteSpace(reason) || !Reasons.Contains(reason))
        {
            error = "Pick a reason from the list.";
            return false;
        }

        var trimmedDetails = details?.Trim();
        if (reason == OtherReason && string.IsNullOrEmpty(trimmedDetails))
        {
            error = "\"Other\" needs a short explanation so it can be acted on.";
            return false;
        }

        if (trimmedDetails?.Length > MaxDetailsLength)
        {
            error = $"Details are limited to {MaxDetailsLength} characters.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Snapshots comment text for the report/warning (≤300 chars).</summary>
    public static string? Excerpt(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= MaxExcerptLength
            ? trimmed
            : trimmed[..(MaxExcerptLength - 1)] + "…";
    }
}
