using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins the admin site-content editor's current-belt field (Unit 11, FR-C8;
/// domain-entities.md section 5) the way ResumeLinksTests pins other Razor
/// markup: by scanning the linked source copied into the test output. The
/// editor is an interactive admin page behind OAuth, so a rendered check is
/// not available locally; the markup scan is the guard against a regression
/// in the option values, the blank-option label or the binding.
/// </summary>
public class SiteContentEditorTests
{
    private const string SelectOpening = @"<select @bind=""_currentBeltText"" @bind:after=""MarkDirty"">";

    private static string Editor() => LinkedSource.Read("RazorComponents", "Admin", "SiteContentEditor.razor");

    /// <summary>The markup of the current-belt select, opening tag to closing tag.</summary>
    private static string SelectBlock(string editor)
    {
        var start = editor.IndexOf(SelectOpening, StringComparison.Ordinal);
        Assert.True(start >= 0, "Expected the current-belt select bound to _currentBeltText with MarkDirty after.");
        var end = editor.IndexOf("</select>", start, StringComparison.Ordinal);
        Assert.True(end > start, "Expected the select to close.");
        return editor[start..end];
    }

    [Fact]
    public void CurrentBeltSelect_OffersTheBlankDefaultThenTheFiveBeltsInLadderOrder()
    {
        var block = SelectBlock(Editor());
        string[] options =
        [
            @"<option value="""">@CurrentBeltDefaultLabel</option>",
            @"<option value=""white"">White</option>",
            @"<option value=""blue"">Blue</option>",
            @"<option value=""purple"">Purple</option>",
            @"<option value=""brown"">Brown</option>",
            @"<option value=""black"">Black</option>",
        ];

        var previous = -1;
        foreach (var option in options)
        {
            var index = block.IndexOf(option, StringComparison.Ordinal);
            Assert.True(index > previous, $"Expected {option} after the previous option.");
            previous = index;
        }

        Assert.Equal(options.Length, CountOccurrences(block, "<option "));
    }

    [Fact]
    public void CurrentBeltSelect_BlankOptionNamesWhatBlankResolvesTo()
    {
        var editor = Editor();

        // BR-20: blank means the env value when SITE_CURRENT_BELT parses, else black.
        Assert.Contains(@"""Default (black)""", editor, StringComparison.Ordinal);
        Assert.Contains(@"$""Default (from env: {BjjRules.BeltName(envBelt)})""", editor, StringComparison.Ordinal);
        Assert.Contains("Site.CurrentBelt is { } envBelt", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentBeltSelect_ReachesTheDraftAndLoadsFromTheOverrides()
    {
        var editor = Editor();

        Assert.Contains("CurrentBeltText: _currentBeltText", editor, StringComparison.Ordinal);
        Assert.Contains("_currentBeltText = overrides?.CurrentBelt ?? string.Empty;", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentBeltField_SitsBetweenBeltCaptionAndBeltDegrees_WithTheGeneralizedDegreesHint()
    {
        var editor = Editor();

        var caption = editor.IndexOf("<span>Belt caption ", StringComparison.Ordinal);
        var belt = editor.IndexOf(
            @"<span>Current belt <span class=""muted"">the belt the rank bar draws</span></span>",
            StringComparison.Ordinal);
        var degrees = editor.IndexOf(
            @"<span>Belt degrees <span class=""muted"">stripes on the rank bar, 0 to 6 (degrees on a black belt)</span></span>",
            StringComparison.Ordinal);

        Assert.True(caption >= 0, "Expected the Belt caption field.");
        Assert.True(belt > caption, "Expected the Current belt field after Belt caption (FR-C8).");
        Assert.True(degrees > belt, "Expected the Belt degrees field, with its generalized hint (FR-C4), after Current belt.");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
