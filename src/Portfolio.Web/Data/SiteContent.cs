namespace Portfolio.Web.Data;

/// <summary>
/// Single-row (Id = 1) admin overrides for the landing-page copy. A null
/// column — or an empty Skills list — means "fall back to the .env value
/// in SiteConfig" (see SiteContentRules.Resolve).
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

    /// <summary>Alt text for the Owner Photo; "Portrait of {owner}" when unset.</summary>
    public string? OwnerPhotoAlt { get; set; }

    // -- BJJ landing flavor (Unit 10). Rendered only when SiteConfig.Flavor
    // == Bjj (BR-1); each field blank-hides its own section independently
    // (BR-2). See Services/SiteContentRules.cs (Resolve, Validate) and
    // Services/BjjRules.cs (parsing) for how these are interpreted.

    /// <summary>Line above the hero H1 (the owner's name and title).</summary>
    public string? HeroEyebrow { get; set; }

    /// <summary>Game-plan chart nodes: `term | reading | how`, one per line,
    /// exactly four lines or the chart is hidden (BR-5).</summary>
    public List<string>? GamePlan { get; set; }

    /// <summary>Caption under the rank bar.</summary>
    public string? BeltCaption { get; set; }

    /// <summary>Degree stripes drawn on the rank bar, 0 to 6 (BR-6).</summary>
    public int? BeltDegrees { get; set; }

    /// <summary>Principles: `maxim | reading`, one to six lines (BR-7).</summary>
    public List<string>? Principles { get; set; }

    public DateTime UpdatedAt { get; set; }
}
