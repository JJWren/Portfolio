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
/// regex only matches a bare onclick=" that is not preceded by '@'.
/// </summary>
public class NoInlineOnClickTests
{
    private static readonly Regex InlineOnClickAttribute = new("(?<!@)onclick=\"", RegexOptions.Compiled);

    private static string RazorComponentsRoot => Path.Combine(AppContext.BaseDirectory, "RazorComponents");

    [Fact]
    public void RazorComponents_ContainNoInlineOnClickAttribute()
    {
        var files = Directory.GetFiles(RazorComponentsRoot, "*.razor", SearchOption.AllDirectories);

        // Sanity check on the harness itself: if the csproj's copy-to-output
        // link (Portfolio.Tests.csproj) ever stops matching files, this must
        // fail loudly instead of passing on a trivially "clean" empty list.
        Assert.True(files.Length > 10, $"Expected many .razor files under {RazorComponentsRoot}; found {files.Length}.");

        var offenders = files
            .Where(file => InlineOnClickAttribute.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RazorComponentsRoot, file))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Inline onclick=\"\" attribute(s) found (wire a site.js data-action instead): " +
            string.Join(", ", offenders));
    }
}
