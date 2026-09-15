using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins colorpicker.js's applyDataStyles export (Unit 12a, FR-D3) the way
/// ThemeToggleTooltipTests pins theme.js: by scanning the linked source
/// copied into the test output rather than driving a JS runtime.
/// </summary>
public class ColorPickerModuleTests
{
    private static string ColorPickerJs() => LinkedSource.Read("js", "colorpicker.js");

    [Fact]
    public void ColorPickerJs_ExportsApplyDataStyles()
        => Assert.Contains("export function applyDataStyles(root)", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void ColorPickerJs_PaintsDataStyleThroughCssText()
        => Assert.Contains("style.cssText", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void ColorPickerJs_PaintsDataColorThroughBackground()
        => Assert.Contains("style.background", ColorPickerJs(), StringComparison.Ordinal);

    [Fact]
    public void ColorPickerJs_NeverSetsTheStyleAttributeDirectly()
        => Assert.DoesNotContain("setAttribute('style'", ColorPickerJs(), StringComparison.Ordinal);
}
