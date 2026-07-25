using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ThemeRulesTests
{
    // ---- Catalog invariants -------------------------------------------------

    [Fact]
    public void Tokens_Has26UniqueKeys()
    {
        Assert.Equal(26, ThemeRules.Tokens.Count);
        Assert.Equal(26, ThemeRules.Tokens.Select(t => t.Key).Distinct().Count());
    }

    [Fact]
    public void Tokens_GroupCounts_Are4And11And11()
    {
        Assert.Equal(4, ThemeRules.Tokens.Count(t => t.Group == ThemeGroup.Brand));
        Assert.Equal(11, ThemeRules.Tokens.Count(t => t.Group == ThemeGroup.Dark));
        Assert.Equal(11, ThemeRules.Tokens.Count(t => t.Group == ThemeGroup.Light));
    }

    [Fact]
    public void Tokens_OnlyDarkAccentInherits_AllOthersHaveCanonicalDefaults()
    {
        var inheriting = ThemeRules.Tokens.Where(t => t.DefaultHex is null).ToList();

        var darkAccent = Assert.Single(inheriting);
        Assert.Equal("dark-accent", darkAccent.Key);
        Assert.Equal("brand-gold", darkAccent.InheritsFrom);

        foreach (var token in ThemeRules.Tokens.Where(t => t.DefaultHex is not null))
        {
            // Canonical means normalization is a no-op.
            Assert.Equal(token.DefaultHex, ThemeRules.NormalizeHex(token.DefaultHex));
        }
    }

    [Fact]
    public void Tokens_DarkAndLightDeclareTheSameCssVars()
    {
        var darkVars = ThemeRules.Tokens.Where(t => t.Group == ThemeGroup.Dark).Select(t => t.CssVar).ToHashSet();
        var lightVars = ThemeRules.Tokens.Where(t => t.Group == ThemeGroup.Light).Select(t => t.CssVar).ToHashSet();

        Assert.Equal(darkVars, lightVars);
    }

    // ---- IsHex / NormalizeHex ----------------------------------------------

    [Theory]
    [InlineData("#abc")]
    [InlineData("#ABC")]
    [InlineData("#a1b2c3")]
    [InlineData("#A1B2C3")]
    public void IsHex_AcceptsThreeAndSixDigits_CaseInsensitive(string value)
        => Assert.True(ThemeRules.IsHex(value));

    [Theory]
    [InlineData("red")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("e9b872")]
    [InlineData("#gggggg")]
    [InlineData(" #abc")]
    public void IsHex_RejectsEverythingElse(string value)
        => Assert.False(ThemeRules.IsHex(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeHex_WhitespaceOnly_ReturnsNull(string? value)
        => Assert.Null(ThemeRules.NormalizeHex(value));

    [Theory]
    [InlineData("#AB1", "#aabb11")]
    [InlineData(" #E9B872 ", "#e9b872")]
    [InlineData("#a63d40", "#a63d40")]
    public void NormalizeHex_ExpandsShorthandTrimsAndLowercases(string input, string expected)
        => Assert.Equal(expected, ThemeRules.NormalizeHex(input));

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("rgb(1,2,3)")]
    [InlineData("#gggggg")]
    public void NormalizeHex_Invalid_ReturnsNull(string value)
        => Assert.Null(ThemeRules.NormalizeHex(value));

    // ---- CheckHexes ---------------------------------------------------------

    [Fact]
    public void CheckHexes_InvalidEntry_NamesTheGroupAndToken()
    {
        var error = ThemeRules.CheckHexes(new Dictionary<string, string> { ["dark-bg"] = "oops" });

        Assert.NotNull(error);
        Assert.Contains("Dark theme", error);
        Assert.Contains("Background", error);
    }

    [Fact]
    public void CheckHexes_BlanksAndValids_ReturnsNull()
        => Assert.Null(ThemeRules.CheckHexes(new Dictionary<string, string>
        {
            ["brand-gold"] = "#FFD700",
            ["dark-bg"] = "  ",
            ["light-accent"] = "",
        }));

    // ---- BuildOverrides -----------------------------------------------------

    [Fact]
    public void BuildOverrides_DropsBlanksInvalidsAndUnknownKeys()
    {
        var overrides = ThemeRules.BuildOverrides(new Dictionary<string, string>
        {
            ["dark-bg"] = "#000000",
            ["light-bg"] = "   ",
            ["dark-text"] = "not-a-color",
            ["not-a-token"] = "#123456",
        });

        Assert.NotNull(overrides);
        var entry = Assert.Single(overrides);
        Assert.Equal("dark-bg", entry.Key);
        Assert.Equal("#000000", entry.Value);
    }

    [Fact]
    public void BuildOverrides_NothingOverridden_ReturnsNull()
        => Assert.Null(ThemeRules.BuildOverrides(new Dictionary<string, string>
        {
            ["dark-bg"] = "",
            ["light-bg"] = "   ",
        }));

    [Fact]
    public void BuildOverrides_NormalizesValues()
    {
        var overrides = ThemeRules.BuildOverrides(new Dictionary<string, string> { ["brand-red"] = " #AB1 " });

        Assert.Equal("#aabb11", overrides?["brand-red"]);
    }

    // ---- Resolve ------------------------------------------------------------

    [Fact]
    public void Resolve_NullOverrides_MatchesAppCssDefaults()
    {
        var effective = ThemeRules.Resolve(null);

        // The drift alarm: these literals must mirror wwwroot/app.css exactly.
        foreach (var token in ThemeRules.Tokens.Where(t => t.DefaultHex is not null))
        {
            Assert.Equal(token.DefaultHex, effective[token.Key]);
        }

        Assert.Equal("#e9b872", effective["dark-accent"]); // var(--c-gold)
    }

    [Fact]
    public void Resolve_OverrideWinsPerToken()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["dark-bg"] = "#000000" });

        Assert.Equal("#000000", effective["dark-bg"]);
        // Un-overridden tokens keep the defaults.
        Assert.Equal("#f6f3ec", effective["light-bg"]);
        Assert.Equal("#e9b872", effective["brand-gold"]);
    }

    [Fact]
    public void Resolve_DarkAccent_FollowsOverriddenBrandGold()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["brand-gold"] = "#ffd700" });

        Assert.Equal("#ffd700", effective["dark-accent"]);
        // The light accent is its own literal and must not move.
        Assert.Equal("#8a6520", effective["light-accent"]);
    }

    [Fact]
    public void Resolve_ExplicitDarkAccent_WinsOverBrandGold()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string>
        {
            ["brand-gold"] = "#ffd700",
            ["dark-accent"] = "#00ff00",
        });

        Assert.Equal("#00ff00", effective["dark-accent"]);
        Assert.Equal("#ffd700", effective["brand-gold"]);
    }

    [Fact]
    public void Resolve_InvalidStoredValue_FallsBackToDefault()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["dark-bg"] = "garbage" });

        Assert.Equal("#151515", effective["dark-bg"]);
    }

    [Fact]
    public void FallbackFor_InheritedToken_TracksCurrentEffectiveSource()
    {
        var darkAccent = ThemeRules.Tokens.Single(t => t.Key == "dark-accent");
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["brand-gold"] = "#ffd700" });

        Assert.Equal("#ffd700", ThemeRules.FallbackFor(darkAccent, effective));
    }

    [Fact]
    public void FallbackFor_LiteralToken_IsItsDefault()
    {
        var lightBg = ThemeRules.Tokens.Single(t => t.Key == "light-bg");

        Assert.Equal("#f6f3ec", ThemeRules.FallbackFor(lightBg, ThemeRules.Resolve(null)));
    }

    // ---- BuildOverrideCss ---------------------------------------------------

    [Fact]
    public void BuildOverrideCss_NoOverrides_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ThemeRules.BuildOverrideCss(null));
        Assert.Equal(string.Empty, ThemeRules.BuildOverrideCss(new Dictionary<string, string>()));
    }

    [Fact]
    public void BuildOverrideCss_EmitsOnlyOverriddenTokens()
    {
        var css = ThemeRules.BuildOverrideCss(new Dictionary<string, string> { ["dark-bg"] = "#000000" });

        Assert.Equal(":root{--bg:#000000;}", css);
    }

    [Fact]
    public void BuildOverrideCss_BrandAndDarkShareRootBlock_LightGetsAttributeBlock()
    {
        var css = ThemeRules.BuildOverrideCss(new Dictionary<string, string>
        {
            ["brand-gold"] = "#ffd700",
            ["dark-bg"] = "#000000",
            ["light-bg"] = "#ffffff",
        });

        Assert.Equal(
            ":root{--c-gold:#ffd700;--bg:#000000;}\n:root[data-theme='light']{--bg:#ffffff;}",
            css);
    }

    [Fact]
    public void BuildOverrideCss_OnlyLightOverrides_OmitsRootBlock()
    {
        var css = ThemeRules.BuildOverrideCss(new Dictionary<string, string> { ["light-accent"] = "#123456" });

        Assert.Equal(":root[data-theme='light']{--accent:#123456;}", css);
    }

    [Fact]
    public void BuildOverrideCss_SkipsNonCanonicalValues()
    {
        // The injection-safety gate: a bad stored value must vanish, not render.
        var css = ThemeRules.BuildOverrideCss(new Dictionary<string, string>
        {
            ["dark-bg"] = "}body{display:none}",
        });

        Assert.Equal(string.Empty, css);
    }

    // ---- BuildPreviewStyle --------------------------------------------------

    [Theory]
    [InlineData(ThemeMode.Dark, "dark")]
    [InlineData(ThemeMode.Light, "light")]
    public void BuildPreviewStyle_ContainsAll15VarsAndColorScheme(ThemeMode mode, string scheme)
    {
        var style = ThemeRules.BuildPreviewStyle(ThemeRules.Resolve(null), mode);

        foreach (var cssVar in new[]
        {
            "--c-red", "--c-gold", "--c-green", "--c-blue",
            "--bg", "--surface", "--surface-2", "--border", "--text", "--text-muted",
            "--accent", "--accent-ink", "--info", "--positive", "--danger",
        })
        {
            Assert.Contains($"{cssVar}:", style);
        }

        Assert.EndsWith($"color-scheme:{scheme}", style);
    }

    [Fact]
    public void BuildPreviewStyle_LightMode_UsesLightValues()
    {
        var style = ThemeRules.BuildPreviewStyle(ThemeRules.Resolve(null), ThemeMode.Light);

        Assert.Contains("--bg:#f6f3ec;", style);
        Assert.DoesNotContain("--bg:#151515;", style);
    }

    [Fact]
    public void BuildPreviewStyle_ReflectsOverrides()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["dark-bg"] = "#010203" });

        Assert.Contains("--bg:#010203;", ThemeRules.BuildPreviewStyle(effective, ThemeMode.Dark));
    }

    // ---- Snapshots ----------------------------------------------------------

    [Fact]
    public void DefaultSnapshot_HasEmptyCssAndDarkBgMeta()
    {
        Assert.Equal(string.Empty, ThemeRules.DefaultSnapshot.OverrideCss);
        Assert.Equal("#151515", ThemeRules.DefaultSnapshot.MetaThemeColor);
    }

    [Fact]
    public void BuildSnapshot_MetaThemeColor_TracksOverriddenDarkBg()
    {
        var snapshot = ThemeRules.BuildSnapshot(new Dictionary<string, string> { ["dark-bg"] = "#0a0b0c" });

        Assert.Equal("#0a0b0c", snapshot.MetaThemeColor);
        Assert.Equal(":root{--bg:#0a0b0c;}", snapshot.OverrideCss);
    }

    // ---- Contrast -----------------------------------------------------------

    [Fact]
    public void ContrastRatio_BlackOnWhite_Is21()
        => Assert.Equal(21.0, ThemeRules.ContrastRatio("#000000", "#ffffff"), 3);

    [Fact]
    public void ContrastRatio_KnownAAPair_IsAbout4Point54()
        => Assert.Equal(4.54, Math.Round(ThemeRules.ContrastRatio("#767676", "#ffffff"), 2));

    [Fact]
    public void ContrastRatio_IsSymmetric()
        => Assert.Equal(
            ThemeRules.ContrastRatio("#e9b872", "#151515"),
            ThemeRules.ContrastRatio("#151515", "#e9b872"));

    [Fact]
    public void ContrastWarnings_DefaultPalette_IsEmpty()
        => Assert.Empty(ThemeRules.ContrastWarnings(ThemeRules.Resolve(null)));

    [Fact]
    public void ContrastWarnings_LowContrastText_WarnsWithModeAndRatio()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["dark-text"] = "#444444" });

        var warning = Assert.Single(
            ThemeRules.ContrastWarnings(effective),
            w => w.Contains("body text on the background"));
        Assert.StartsWith("Dark:", warning);
        Assert.Contains("1.9:1", warning);
        Assert.Contains("4.5:1 minimum", warning);
    }

    [Fact]
    public void ContrastWarnings_FocusRing_UsesThreeToOne()
    {
        // Brand blue against a same-lightness light background.
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["light-bg"] = "#8899aa" });

        var warning = Assert.Single(
            ThemeRules.ContrastWarnings(effective),
            w => w.Contains("focus ring") && w.StartsWith("Light:"));
        Assert.Contains("3:1 minimum", warning);
    }

    [Fact]
    public void ContrastWarnings_SelectionGold_UsesThreeToOne()
    {
        var effective = ThemeRules.Resolve(new Dictionary<string, string> { ["brand-gold"] = "#222222" });

        var warning = Assert.Single(
            ThemeRules.ContrastWarnings(effective),
            w => w.StartsWith("Selection:"));
        Assert.Contains("#151515", warning);
        Assert.Contains("3:1 minimum", warning);
    }
}
