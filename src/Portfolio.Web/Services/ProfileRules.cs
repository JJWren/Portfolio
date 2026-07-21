namespace Portfolio.Web.Services;

public static class ProfileRules
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 40;

    /// <summary>
    /// Normalizes a chosen display name. Empty clears the custom name (falling
    /// back to the provider name); otherwise it must be 2–40 characters.
    /// Returns false with an error when invalid.
    /// </summary>
    public static bool TryNormalizeDisplayName(string? input, out string? normalized, out string? error)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            normalized = null;
            error = null;
            return true;
        }

        if (trimmed.Length is < MinNameLength or > MaxNameLength)
        {
            normalized = null;
            error = $"Display names are {MinNameLength}–{MaxNameLength} characters.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
