using System.Text.RegularExpressions;

namespace Portfolio.Tests;

/// <summary>
/// Pins the theme toggle's BJJ tooltip wording (Unit 10 addendum, FR-B1 to
/// FR-B5) the way NoInlineOnClickTests pins the delegated handlers: by
/// scanning the linked source files in the test output rather than driving
/// a browser. App.razor must carry the data-flavor attribute fed by
/// SiteFlavorRules.HtmlDataFlavor (null under the default flavor, so the
/// plain page's markup is unchanged); theme.js must gate on that attribute
/// and hold each approved sentence exactly once; MainLayout's toggle must
/// keep its server-rendered title and its functional aria-label.
/// </summary>
public class ThemeToggleTooltipTests
{
    private const string ShownWhileDark = "Switch to the white gi (light theme)";
    private const string ShownWhileLight = "Switch to the black gi (dark theme)";

    private static string Linked(string relativePath)
    {
        // Sanity check on the harness: a missing link must name its cause,
        // not surface as a FileNotFoundException.
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/CopyToOutputDirectory items in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

    [Fact]
    public void AppRazor_HtmlElement_CarriesTheFlavorAttribute()
    {
        var app = Linked(Path.Combine("RazorComponents", "App.razor"));

        Assert.Contains(
            "<html lang=\"en\" data-flavor=\"@Portfolio.Web.Services.SiteFlavorRules.HtmlDataFlavor(Site.Flavor)\">",
            app, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeJs_GatesOnTheFlavorAttribute_AndHoldsEachSentenceOnce()
    {
        var themeJs = Linked(Path.Combine("js", "theme.js"));

        Assert.Contains("document.documentElement.dataset.flavor !== 'bjj'", themeJs, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(themeJs, Regex.Escape(ShownWhileDark)).Count);
        Assert.Equal(1, Regex.Matches(themeJs, Regex.Escape(ShownWhileLight)).Count);
    }

    [Fact]
    public void MainLayout_ThemeToggle_KeepsItsServerTitleAndAriaLabel()
    {
        var layout = Linked(Path.Combine("RazorComponents", "Layout", "MainLayout.razor"));

        Assert.Contains("title=\"Switch theme\"", layout, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Switch between dark and light theme\"", layout, StringComparison.Ordinal);
    }
}
