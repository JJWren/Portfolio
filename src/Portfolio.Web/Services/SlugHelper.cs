using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio.Web.Services;

public static partial class SlugHelper
{
    /// <summary>Turns a title into a URL-safe slug: lowercase, dashes, ASCII alphanumerics.</summary>
    public static string Slugify(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var normalized = title.Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                ascii.Append(ch);
            }
        }

        var slug = NonSlugCharacters().Replace(ascii.ToString().ToLowerInvariant(), "-");
        slug = RepeatedDashes().Replace(slug, "-").Trim('-');
        return slug.Length > PostRules.SlugMaxLength
            ? slug[..PostRules.SlugMaxLength].Trim('-')
            : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedDashes();
}
