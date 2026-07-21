namespace Portfolio.Web.Services;

public static class CommentRules
{
    public const int MaxLength = 2000;

    /// <summary>Normalizes a comment body; returns null with an error when invalid.</summary>
    public static string? Validate(string? body, out string? error)
    {
        var trimmed = body?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            error = "Write something first — empty comments aren't saved.";
            return null;
        }

        if (trimmed.Length > MaxLength)
        {
            error = $"Comments are limited to {MaxLength} characters (yours is {trimmed.Length}).";
            return null;
        }

        error = null;
        return trimmed;
    }
}
