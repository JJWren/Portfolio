namespace Portfolio.Tests;

/// <summary>
/// Pins colorpicker.js's applyDataStyles export (Unit 12a, FR-D3) the way
/// ThemeToggleTooltipTests pins theme.js: by scanning the linked source
/// copied into the test output rather than driving a JS runtime.
/// </summary>
public class ColorPickerModuleTests
{
    private static string ColorPickerJs()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "js", "colorpicker.js");
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/Link item in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ExportsApplyDataStyles()
        => Assert.Contains("export function applyDataStyles(root)", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void PaintsDataStyleThroughCssText()
        => Assert.Contains("style.cssText", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void PaintsDataColorThroughBackground()
        => Assert.Contains("style.background", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void NeverSetsTheStyleAttributeDirectly()
        => Assert.DoesNotContain("setAttribute('style'", ColorPickerJs(), StringComparison.Ordinal);
}
