using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins Unit 12a facts about the linked App.razor: the theme override style
/// element stays exactly the one line its Content-Security-Policy hash is
/// computed over (FR-D3; ThemeRules.StyleHash hashes _theme.OverrideCss, not
/// the markup, so a reformat that adds a second line or trailing content
/// would silently break the hash match); the dead import map never comes
/// back (FR-D3 replaced, requirements section 10.4: removing the unused
/// ImportMap component — no module in the project imports anything — is
/// what leaves no executable inline script behind); and, from the Copilot
/// review remediation, _theme is read from SecurityHeadersMiddleware's
/// request item first (so the CSP hash and this block always agree) with
/// ThemeService.GetSnapshotAsync as the fallback for a render the
/// middleware never touched.
/// </summary>
public class AppRazorTests
{
    private static string AppRazor() => LinkedSource.Read("RazorComponents", "App.razor");

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

    [Fact]
    public void HasACascadingHttpContextParameter()
    {
        var source = AppRazor();

        Assert.Contains("[CascadingParameter]", source, StringComparison.Ordinal);
        Assert.Contains("public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OnInitializedAsync_ReadsTheRequestItemKey_AndFallsBackToTheStore()
    {
        var source = AppRazor();

        Assert.Contains("SecurityHeadersMiddleware.ThemeSnapshotItemKey", source, StringComparison.Ordinal);
        Assert.Contains("await ThemeStore.GetSnapshotAsync()", source, StringComparison.Ordinal);
    }
}
