namespace Portfolio.Web.Services;

public static class ProjectRules
{
    // Single source for the Project column sizes AppDbContext applies;
    // changing one is a schema change and needs a migration.
    public const int TitleMaxLength = 120;
    public const int SummaryMaxLength = 500;
    public const int HeaderImagePathMaxLength = 400;
    public const int HomepageUrlMaxLength = 400;
    public const int RepoUrlMaxLength = 400;

    /// <summary>Returns a friendly error for the first field over its stored size, or null when everything fits.</summary>
    public static string? CheckLengths(
        string title, string summary, string? headerImagePath, string? homepageUrl, string? repoUrl)
    {
        if (title.Length > TitleMaxLength)
        {
            return $"Titles are limited to {TitleMaxLength} characters (yours is {title.Length}).";
        }

        if (summary.Length > SummaryMaxLength)
        {
            return $"Summaries are limited to {SummaryMaxLength} characters (yours is {summary.Length}).";
        }

        if (headerImagePath is not null && headerImagePath.Length > HeaderImagePathMaxLength)
        {
            return $"Card image paths are limited to {HeaderImagePathMaxLength} characters (yours is {headerImagePath.Length}).";
        }

        if (homepageUrl is not null && homepageUrl.Length > HomepageUrlMaxLength)
        {
            return $"Homepage URLs are limited to {HomepageUrlMaxLength} characters (yours is {homepageUrl.Length}).";
        }

        if (repoUrl is not null && repoUrl.Length > RepoUrlMaxLength)
        {
            return $"Repo URLs are limited to {RepoUrlMaxLength} characters (yours is {repoUrl.Length}).";
        }

        return null;
    }
}
