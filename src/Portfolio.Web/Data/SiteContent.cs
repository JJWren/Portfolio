namespace Portfolio.Web.Data;

/// <summary>
/// Single-row (Id = 1) admin overrides for the landing-page copy. A null
/// column means "fall back to the .env value in SiteConfig".
/// </summary>
public class SiteContent
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Landing hero H1; the owner name when unset.</summary>
    public string? HeroHeading { get; set; }

    public string? Tagline { get; set; }

    /// <summary>Real newlines separate paragraphs.</summary>
    public string? About { get; set; }

    public List<string>? Skills { get; set; }

    public DateTime UpdatedAt { get; set; }
}
