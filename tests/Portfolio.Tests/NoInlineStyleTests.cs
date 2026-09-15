using System.Text.RegularExpressions;

namespace Portfolio.Tests;

/// <summary>
/// Pins the Unit 12a non-negotiable: no component under Components/ emits a
/// style attribute (FR-D3), because the Content-Security-Policy's style-src
/// carries no 'unsafe-inline'. The regex requires "style" to not be preceded
/// by a word character or hyphen, so ThemeEditor.razor's data-style="..."
/// and data-color="..." (colorpicker.js's applyDataStyles hand-off) never
/// match — the same idiom NoInlineOnClickTests uses to spare data-onclick.
/// </summary>
public class NoInlineStyleTests
{
    private static readonly Regex InlineStyleAttribute = new(
        @"(?<![\w-])style\s*=\s*[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string RazorComponentsRoot => Path.Combine(AppContext.BaseDirectory, "RazorComponents");

    [Fact]
    public void RazorComponents_ContainNoStyleAttribute()
    {
        // Sanity checks on the harness itself, the same way NoInlineOnClickTests
        // guards against a silently-empty link in Portfolio.Tests.csproj.
        Assert.True(Directory.Exists(RazorComponentsRoot),
            $"Expected the linked .razor sources under {RazorComponentsRoot}; the directory is missing (check the None/LinkBase item in Portfolio.Tests.csproj).");

        var files = Directory.GetFiles(RazorComponentsRoot, "*.razor", SearchOption.AllDirectories);
        Assert.True(files.Length > 10, $"Expected many .razor files under {RazorComponentsRoot}; found {files.Length}.");

        var offenders = files
            .Where(file => InlineStyleAttribute.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RazorComponentsRoot, file))
            .ToList();

        Assert.True(offenders.Count == 0,
            "style=\"\" attribute(s) found (use data-style/data-color and colorpicker.js's applyDataStyles instead): " +
            string.Join(", ", offenders));
    }

    [Theory]
    [InlineData("<div style=\"color:red\">", true)]
    [InlineData("<div style='color:red'>", true)]
    [InlineData("<div STYLE = \"color:red\">", true)]
    [InlineData("<div data-style=\"color:red\">", false)]
    [InlineData("<button data-color=\"#fff\">", false)]
    [InlineData("<div xstyle=\"x\">", false)]
    [InlineData("private string _previewStyle = ThemeRules.BuildPreviewStyle(t, m);", false)]
    public void InlineStylePattern_MatchesBareStyleAttributeOnly(string markup, bool expected)
        => Assert.Equal(expected, InlineStyleAttribute.IsMatch(markup));
}
