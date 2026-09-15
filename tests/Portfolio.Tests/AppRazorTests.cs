namespace Portfolio.Tests;

/// <summary>
/// Pins two Unit 12a facts about the linked App.razor: the theme override
/// style element stays exactly the one line its Content-Security-Policy
/// hash is computed over (FR-D3; ThemeRules.StyleHash hashes
/// _theme.OverrideCss, not the markup, so a reformat that adds a second
/// line or trailing content would silently break the hash match), and the
/// dead import map never comes back (FR-D7: no module in the project
/// imports another).
/// </summary>
public class AppRazorTests
{
    private static string AppRazor()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "RazorComponents", "App.razor");
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/LinkBase item in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

    [Fact]
    public void OverrideStyleElement_IsExactlyOneLineWithNothingElseOnIt()
    {
        var lines = AppRazor().Split('\n');
        var styleLine = Assert.Single(lines, line => line.TrimStart().StartsWith("<style>", StringComparison.Ordinal));

        Assert.Equal("<style>@((MarkupString)_theme.OverrideCss)</style>", styleLine.Trim());
    }

    [Fact]
    public void DoesNotContainTheImportMap()
        => Assert.DoesNotContain("<ImportMap", AppRazor(), StringComparison.Ordinal);
}
