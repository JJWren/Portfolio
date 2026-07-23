namespace Portfolio.Web.Services;

public static class PostRules
{
    // Single source for the BlogPost column sizes AppDbContext applies;
    // changing one is a schema change and needs a migration.
    public const int SlugMaxLength = 160;
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 500;
    public const int HeaderImagePathMaxLength = 400;

    /// <summary>Returns a friendly error for the first field over its stored size, or null when everything fits.</summary>
    public static string? CheckLengths(string title, string slug, string summary, string? headerImagePath)
    {
        if (title.Length > TitleMaxLength)
        {
            return $"Titles are limited to {TitleMaxLength} characters (yours is {title.Length}).";
        }

        if (slug.Length > SlugMaxLength)
        {
            return $"Slugs are limited to {SlugMaxLength} characters (yours is {slug.Length}).";
        }

        if (summary.Length > SummaryMaxLength)
        {
            return $"Summaries are limited to {SummaryMaxLength} characters (yours is {summary.Length}).";
        }

        if (headerImagePath is not null && headerImagePath.Length > HeaderImagePathMaxLength)
        {
            return $"Header image paths are limited to {HeaderImagePathMaxLength} characters (yours is {headerImagePath.Length}).";
        }

        return null;
    }
}
