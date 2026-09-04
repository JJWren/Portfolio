using System.Text.RegularExpressions;

namespace Portfolio.Tests;

/// <summary>
/// Pins the handoff constraint that page interactivity comes from CSS or the
/// site.js delegated data-action click handler, never a raw inline
/// onclick="" attribute (Unit 10 Phase 5 close-out, which replaced the last
/// four — the nav burger, the theme toggle and the two carousel buttons —
/// with data-action). Blazor's own @onclick="..." directive, used throughout
/// the InteractiveServer admin pages and a handful of other components, is a
/// different mechanism entirely and is deliberately not flagged here: the
/// regex only matches a bare onclick attribute (any letter case, either
/// quote) whose name is not preceded by '@', a word character or a hyphen,
/// so @onclick, data-onclick and the like never match.
/// </summary>
public class NoInlineOnClickTests
{
    private static readonly Regex InlineOnClickAttribute = new(
        @"(?<![\w@-])onclick\s*=\s*[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string RazorComponentsRoot => Path.Combine(AppContext.BaseDirectory, "RazorComponents");

    [Fact]
    public void RazorComponents_ContainNoInlineOnClickAttribute()
    {
        // Sanity checks on the harness itself: if the csproj's copy-to-output
        // link (Portfolio.Tests.csproj) ever stops copying files, this must fail
        // with a message that names the cause, not with a DirectoryNotFoundException
        // from GetFiles or a trivially "clean" pass on an empty list.
        Assert.True(Directory.Exists(RazorComponentsRoot),
            $"Expected the linked .razor sources under {RazorComponentsRoot}; the directory is missing (check the None/LinkBase item in Portfolio.Tests.csproj).");

        var files = Directory.GetFiles(RazorComponentsRoot, "*.razor", SearchOption.AllDirectories);
        Assert.True(files.Length > 10, $"Expected many .razor files under {RazorComponentsRoot}; found {files.Length}.");

        var offenders = files
            .Where(file => InlineOnClickAttribute.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RazorComponentsRoot, file))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Inline onclick=\"\" attribute(s) found (wire a site.js data-action instead): " +
            string.Join(", ", offenders));
    }

    [Theory]
    [InlineData("<button type=\"button\" onclick=\"x()\">", true)]
    [InlineData("<button onClick='x()'>", true)]
    [InlineData("<button ONCLICK = \"x()\">", true)]
    [InlineData("<button @onclick=\"Save\">", false)]
    [InlineData("<div data-onclick=\"x\">", false)]
    [InlineData("<div xonclick=\"x\">", false)]
    [InlineData("<button data-action=\"toggle-nav\">", false)]
    public void InlineOnClickPattern_MatchesBareAttributesOnly(string markup, bool expected)
        => Assert.Equal(expected, InlineOnClickAttribute.IsMatch(markup));
}
