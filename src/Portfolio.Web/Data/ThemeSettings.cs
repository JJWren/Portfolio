namespace Portfolio.Web.Data;

/// <summary>
/// Single-row (Id = 1) admin palette overrides. Keys are ThemeRules token keys
/// ("brand-gold", "dark-bg", "light-accent", …); values are canonical
/// "#rrggbb". A missing key means "use the built-in app.css default"; a null
/// or empty dictionary means nothing is overridden.
/// </summary>
public class ThemeSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public Dictionary<string, string>? Overrides { get; set; }

    public DateTime UpdatedAt { get; set; }
}
