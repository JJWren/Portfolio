using System.Globalization;
using System.Text;

namespace Portfolio.Web.Services;

/// <summary>Which app.css block a token belongs to.</summary>
public enum ThemeGroup
{
    /// <summary>The four graphic colors shared by both themes (ribbons, gradients, focus ring).</summary>
    Brand,

    /// <summary>The default palette in <c>:root</c>.</summary>
    Dark,

    /// <summary>The overrides in <c>:root[data-theme='light']</c>.</summary>
    Light,
}

/// <summary>The mode a preview renders in — orthogonal to the visitor's own toggle.</summary>
public enum ThemeMode
{
    Dark,
    Light,
}

/// <summary>
/// One design token. <see cref="DefaultHex"/> is null only when
/// <see cref="InheritsFrom"/> names the token whose effective value is this
/// token's default (dark accent → brand gold, mirroring app.css
/// <c>--accent: var(--c-gold)</c>).
/// </summary>
/// <param name="Key">Form/dictionary/storage key, e.g. "dark-bg".</param>
/// <param name="CssVar">The CSS custom property, e.g. "--bg".</param>
/// <param name="Group">Which app.css block the token belongs to.</param>
/// <param name="Label">Human label shown in the editor.</param>
/// <param name="DefaultHex">The literal app.css default, canonical lowercase.</param>
/// <param name="InheritsFrom">Key of the token supplying the default when <see cref="DefaultHex"/> is null.</param>
public sealed record ThemeToken(
    string Key,
    string CssVar,
    ThemeGroup Group,
    string Label,
    string? DefaultHex,
    string? InheritsFrom = null);

/// <summary>All token values resolved to literal "#rrggbb", keyed by token key.</summary>
public sealed record EffectiveTheme(IReadOnlyDictionary<string, string> Values)
{
    public string this[string key] => Values[key];
}

/// <summary>Everything App.razor needs, precomputed once per save.</summary>
/// <param name="Theme">The resolved palette.</param>
/// <param name="OverrideCss">Style-block body for overridden tokens only; empty when nothing is overridden.</param>
/// <param name="MetaThemeColor">The effective dark background, for the theme-color meta tag.</param>
public sealed record ThemeSnapshot(EffectiveTheme Theme, string OverrideCss, string MetaThemeColor);

/// <summary>
/// Rules for the admin palette overrides. Blank input means "use the built-in
/// app.css value" and is simply not stored; values are normalized to canonical
/// lowercase "#rrggbb" before storage and re-checked at every emission.
/// </summary>
public static class ThemeRules
{
    /// <summary>"#rrggbb" — the canonical stored length, also the input maxlength.</summary>
    public const int HexLength = 7;

    // WCAG 2.x minimums: AA normal text, and non-text UI indicators (1.4.11).
    private const double NormalTextMinimum = 4.5;
    private const double NonTextMinimum = 3.0;

    /// <summary>
    /// The token catalog, in display/emission order (Brand before Dark so
    /// inherited defaults resolve in one pass). Defaults MUST mirror the
    /// wwwroot/app.css :root and [data-theme='light'] blocks — the
    /// Resolve_Null_MatchesAppCssDefaults test pins every value.
    /// </summary>
    public static readonly IReadOnlyList<ThemeToken> Tokens =
    [
        new("brand-red", "--c-red", ThemeGroup.Brand, "Brand red", "#a63d40"),
        new("brand-gold", "--c-gold", ThemeGroup.Brand, "Brand gold", "#e9b872"),
        new("brand-green", "--c-green", ThemeGroup.Brand, "Brand green", "#90a959"),
        new("brand-blue", "--c-blue", ThemeGroup.Brand, "Brand blue", "#6494aa"),

        new("dark-bg", "--bg", ThemeGroup.Dark, "Background", "#151515"),
        new("dark-surface", "--surface", ThemeGroup.Dark, "Surface", "#1d1c1a"),
        new("dark-surface-2", "--surface-2", ThemeGroup.Dark, "Raised surface", "#262421"),
        new("dark-border", "--border", ThemeGroup.Dark, "Border", "#35322d"),
        new("dark-text", "--text", ThemeGroup.Dark, "Text", "#e8e4dd"),
        new("dark-text-muted", "--text-muted", ThemeGroup.Dark, "Muted text", "#a49d91"),
        new("dark-accent", "--accent", ThemeGroup.Dark, "Accent", null, InheritsFrom: "brand-gold"),
        new("dark-accent-ink", "--accent-ink", ThemeGroup.Dark, "Text on accent", "#151515"),
        new("dark-info", "--info", ThemeGroup.Dark, "Info", "#8fb6c9"),
        new("dark-positive", "--positive", ThemeGroup.Dark, "Positive", "#a9c17a"),
        new("dark-danger", "--danger", ThemeGroup.Dark, "Danger", "#d98c8e"),

        new("light-bg", "--bg", ThemeGroup.Light, "Background", "#f6f3ec"),
        new("light-surface", "--surface", ThemeGroup.Light, "Surface", "#fdfbf6"),
        new("light-surface-2", "--surface-2", ThemeGroup.Light, "Raised surface", "#ece7db"),
        new("light-border", "--border", ThemeGroup.Light, "Border", "#d8d1c2"),
        new("light-text", "--text", ThemeGroup.Light, "Text", "#24211c"),
        new("light-text-muted", "--text-muted", ThemeGroup.Light, "Muted text", "#6b6459"),
        new("light-accent", "--accent", ThemeGroup.Light, "Accent", "#8a6520"),
        new("light-accent-ink", "--accent-ink", ThemeGroup.Light, "Text on accent", "#fdfbf6"),
        new("light-info", "--info", ThemeGroup.Light, "Info", "#3f6e86"),
        new("light-positive", "--positive", ThemeGroup.Light, "Positive", "#5c7a35"),
        new("light-danger", "--danger", ThemeGroup.Light, "Danger", "#8f3538"),
    ];

    /// <summary>Resolved defaults + empty CSS — first paint and the DB-blip fallback.</summary>
    public static readonly ThemeSnapshot DefaultSnapshot = BuildSnapshot(null);

    /// <summary>Strict #rgb / #rrggbb (case-insensitive); no surrounding whitespace.</summary>
    public static bool IsHex(string value)
    {
        if (value.Length is not (4 or 7) || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!char.IsAsciiHexDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Trims; blank → null; "#AB1" → "#aabb11"; invalid non-blank → null (callers surface errors via CheckHexes first).</summary>
    public static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var hex = value.Trim();
        if (!IsHex(hex))
        {
            return null;
        }

        if (hex.Length == 4)
        {
            hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
        }

        return hex.ToLowerInvariant();
    }

    /// <summary>Returns a friendly error for the first invalid non-blank input, or null when everything parses.</summary>
    public static string? CheckHexes(IReadOnlyDictionary<string, string> rawInputs)
    {
        foreach (var token in Tokens)
        {
            if (!rawInputs.TryGetValue(token.Key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (NormalizeHex(raw) is null)
            {
                return $"{GroupLabel(token.Group)}: {token.Label} must be a hex color like #1a2b3c.";
            }
        }

        return null;
    }

    /// <summary>Normalized valid entries only; unknown keys and invalid values dropped; null when nothing is overridden (the stored shape).</summary>
    public static Dictionary<string, string>? BuildOverrides(IReadOnlyDictionary<string, string> rawInputs)
    {
        Dictionary<string, string>? overrides = null;
        foreach (var token in Tokens)
        {
            if (rawInputs.TryGetValue(token.Key, out var raw) && NormalizeHex(raw) is { } hex)
            {
                (overrides ??= []).Add(token.Key, hex);
            }
        }

        return overrides;
    }

    /// <summary>
    /// Catalog defaults with overrides winning per token. Dark accent follows
    /// the effective brand gold unless explicitly overridden; stored values
    /// that fail re-normalization fall back to the default (a stale or
    /// hand-edited row must never break rendering).
    /// </summary>
    public static EffectiveTheme Resolve(IReadOnlyDictionary<string, string>? overrides)
    {
        var values = new Dictionary<string, string>(Tokens.Count);
        foreach (var token in Tokens)
        {
            string? stored = null;
            if (overrides is not null && overrides.TryGetValue(token.Key, out var raw))
            {
                stored = NormalizeHex(raw);
            }

            // Inherited defaults resolve in one pass because Brand precedes Dark
            // in the catalog.
            values[token.Key] = stored ?? token.DefaultHex ?? values[token.InheritsFrom!];
        }

        return new EffectiveTheme(values);
    }

    /// <summary>The value a token takes when its input is blank — the editor placeholder. Inherited tokens track their current effective source.</summary>
    public static string FallbackFor(ThemeToken token, EffectiveTheme current)
        => token.DefaultHex ?? current[token.InheritsFrom!];

    /// <summary>
    /// ":root{…}" (brand + dark) and ":root[data-theme='light']{…}" blocks for
    /// ONLY the overridden tokens, so un-overridden tokens keep their app.css
    /// semantics (including dark accent's var(--c-gold) reference). Every value
    /// is re-normalized at emission and skipped otherwise — no admin-entered
    /// free text can ever reach the markup.
    /// </summary>
    public static string BuildOverrideCss(IReadOnlyDictionary<string, string>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return string.Empty;
        }

        var root = new StringBuilder();
        var light = new StringBuilder();
        foreach (var token in Tokens)
        {
            if (!overrides.TryGetValue(token.Key, out var value) || NormalizeHex(value) is not { } hex)
            {
                continue;
            }

            var target = token.Group == ThemeGroup.Light ? light : root;
            target.Append(token.CssVar).Append(':').Append(hex).Append(';');
        }

        var css = new StringBuilder();
        if (root.Length > 0)
        {
            css.Append(":root{").Append(root).Append('}');
        }

        if (light.Length > 0)
        {
            if (css.Length > 0)
            {
                css.Append('\n');
            }

            css.Append(":root[data-theme='light']{").Append(light).Append('}');
        }

        return css.ToString();
    }

    /// <summary>
    /// Inline style for the preview frame: all four brand vars plus the full
    /// role set for the mode, plus color-scheme. Deliberately complete — every
    /// var the frame's children read resolves from these inline values, so the
    /// admin page's own current theme cannot leak into the preview.
    /// </summary>
    public static string BuildPreviewStyle(EffectiveTheme theme, ThemeMode mode)
    {
        var roleGroup = mode == ThemeMode.Dark ? ThemeGroup.Dark : ThemeGroup.Light;
        var style = new StringBuilder();
        foreach (var token in Tokens)
        {
            if (token.Group == ThemeGroup.Brand || token.Group == roleGroup)
            {
                style.Append(token.CssVar).Append(':').Append(theme[token.Key]).Append(';');
            }
        }

        style.Append("color-scheme:").Append(mode == ThemeMode.Dark ? "dark" : "light");
        return style.ToString();
    }

    /// <summary>Resolve + BuildOverrideCss + the theme-color meta value (the effective dark background) in one snapshot.</summary>
    public static ThemeSnapshot BuildSnapshot(IReadOnlyDictionary<string, string>? overrides)
    {
        var theme = Resolve(overrides);
        return new ThemeSnapshot(theme, BuildOverrideCss(overrides), theme["dark-bg"]);
    }

    /// <summary>WCAG 2.x relative-luminance contrast ratio between two hex colors (1..21).</summary>
    public static double ContrastRatio(string hexA, string hexB)
    {
        var luminanceA = RelativeLuminance(hexA);
        var luminanceB = RelativeLuminance(hexB);
        var (lighter, darker) = luminanceA >= luminanceB
            ? (luminanceA, luminanceB)
            : (luminanceB, luminanceA);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Non-blocking readability warnings for the pairs the landing UI actually
    /// renders, per mode, plus the fixed-highlight ::selection pair. Empty for
    /// the shipped default palette.
    /// </summary>
    public static IReadOnlyList<string> ContrastWarnings(EffectiveTheme theme)
    {
        var warnings = new List<string>();
        foreach (var mode in new[] { ThemeMode.Dark, ThemeMode.Light })
        {
            var prefix = mode == ThemeMode.Dark ? "dark" : "light";
            var label = mode == ThemeMode.Dark ? "Dark" : "Light";
            Check(warnings, label, "body text on the background", theme[$"{prefix}-text"], theme[$"{prefix}-bg"], NormalTextMinimum);
            Check(warnings, label, "body text on surfaces", theme[$"{prefix}-text"], theme[$"{prefix}-surface"], NormalTextMinimum);
            Check(warnings, label, "muted text on the background", theme[$"{prefix}-text-muted"], theme[$"{prefix}-bg"], NormalTextMinimum);
            Check(warnings, label, "accent links on the background", theme[$"{prefix}-accent"], theme[$"{prefix}-bg"], NormalTextMinimum);
            Check(warnings, label, "button text on the accent", theme[$"{prefix}-accent-ink"], theme[$"{prefix}-accent"], NormalTextMinimum);
            Check(warnings, label, "the focus ring against the background", theme["brand-blue"], theme[$"{prefix}-bg"], NonTextMinimum);
        }

        // ::selection pairs the effective brand gold with a literal #151515
        // for the highlighted text (app.css), independent of mode.
        Check(warnings, "Selection", "brand gold against the fixed #151515 highlight text", theme["brand-gold"], "#151515", NonTextMinimum);
        return warnings;
    }

    private static void Check(
        List<string> warnings, string modeLabel, string pairLabel, string foreground, string background, double minimum)
    {
        // Compare at the displayed one-decimal precision so a ratio that reads
        // as "3.0:1" never warns about the 3:1 minimum (the stock light focus
        // ring sits at 2.98:1).
        var ratio = Math.Round(ContrastRatio(foreground, background), 1);
        if (ratio < minimum)
        {
            warnings.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{modeLabel}: {pairLabel} is {ratio:0.0}:1 — below the {minimum:0.#}:1 minimum."));
        }
    }

    private static string GroupLabel(ThemeGroup group)
        => group switch
        {
            ThemeGroup.Brand => "Brand",
            ThemeGroup.Dark => "Dark theme",
            _ => "Light theme",
        };

    private static double RelativeLuminance(string hex)
    {
        var canonical = NormalizeHex(hex)
            ?? throw new ArgumentException($"Not a hex color: \"{hex}\".", nameof(hex));
        return (0.2126 * Linearize(canonical, 1))
            + (0.7152 * Linearize(canonical, 3))
            + (0.0722 * Linearize(canonical, 5));
    }

    private static double Linearize(string canonicalHex, int offset)
    {
        var channel = int.Parse(canonicalHex.AsSpan(offset, 2), NumberStyles.HexNumber) / 255.0;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
