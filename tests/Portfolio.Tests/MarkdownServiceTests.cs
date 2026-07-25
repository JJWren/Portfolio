using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _markdown = new();

    [Fact]
    public void ToHtml_RendersBasicMarkdown()
    {
        var html = _markdown.ToHtml("# Heading\n\nSome **bold** text.");

        Assert.Contains("<h1", html);
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void ToHtml_RendersFencedCodeWithLanguageClass()
    {
        var html = _markdown.ToHtml("```csharp\nvar x = 1;\n```");

        Assert.Contains("language-csharp", html);
    }

    [Fact]
    public void ToHtml_RendersPipeTables()
    {
        var html = _markdown.ToHtml("| a | b |\n|---|---|\n| 1 | 2 |");

        Assert.Contains("<table", html);
    }

    [Fact]
    public void ToHtml_HandlesNull()
    {
        Assert.Equal(string.Empty, _markdown.ToHtml(null!).Trim());
    }

    [Fact]
    public void ToSafeHtml_RendersEmphasisAndStrikethrough()
    {
        var html = _markdown.ToSafeHtml("**bold** and ~~gone~~");

        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<del>gone</del>", html);
    }

    [Fact]
    public void ToSafeHtml_RendersFencedCodeWithLanguageClass()
    {
        var html = _markdown.ToSafeHtml("```csharp\nvar x = 1;\n```");

        Assert.Contains("language-csharp", html);
    }

    [Fact]
    public void ToSafeHtml_RendersPipeTables()
    {
        var html = _markdown.ToSafeHtml("| a | b |\n|---|---|\n| 1 | 2 |");

        Assert.Contains("<table", html);
    }

    [Fact]
    public void ToSafeHtml_TreatsSingleNewlineAsLineBreak()
    {
        var html = _markdown.ToSafeHtml("line one\nline two");

        Assert.Contains("<br", html);
    }

    [Fact]
    public void ToSafeHtml_AutolinksBareUrls()
    {
        var html = _markdown.ToSafeHtml("see https://example.com today");

        Assert.Contains("<a href=\"https://example.com\"", html);
        Assert.Contains("rel=\"nofollow ugc noopener\"", html);
    }

    [Fact]
    public void ToSafeHtml_MarksAngleAutolinksAsUntrusted()
    {
        var html = _markdown.ToSafeHtml("<https://example.com>");

        Assert.Contains("<a href=\"https://example.com\"", html);
        Assert.Contains("rel=\"nofollow ugc noopener\"", html);
    }

    [Fact]
    public void ToSafeHtml_MarksLinksAsUntrusted()
    {
        var html = _markdown.ToSafeHtml("[site](https://example.com)");

        Assert.Contains("rel=\"nofollow ugc noopener\"", html);
    }

    [Fact]
    public void ToSafeHtml_EscapesRawHtml()
    {
        var html = _markdown.ToSafeHtml("<script>alert(1)</script>");

        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void ToSafeHtml_EscapesInlineHtmlWithEventHandlers()
    {
        var html = _markdown.ToSafeHtml("hi <img src=x onerror=alert(1)> there");

        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void ToSafeHtml_LeavesGenericAttributeSyntaxInert()
    {
        var html = _markdown.ToSafeHtml("[x](https://example.com){onclick=alert(1)}");

        var anchorStart = html.IndexOf("<a");
        var anchor = html[anchorStart..html.IndexOf('>', anchorStart)];
        Assert.DoesNotContain("onclick", anchor);
    }

    [Fact]
    public void ToSafeHtml_DropsJavascriptSchemeLinks()
    {
        var html = _markdown.ToSafeHtml("[x](javascript:alert(1))");

        Assert.DoesNotContain("javascript:", html);
        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public void ToSafeHtml_NeutralizesJavascriptAutolinks()
    {
        var html = _markdown.ToSafeHtml("<javascript:alert(1)>");

        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public void ToSafeHtml_KeepsRelativeLinks()
    {
        var html = _markdown.ToSafeHtml("[home](/blog)");

        Assert.Contains("<a href=\"/blog\"", html);
    }

    [Fact]
    public void ToSafeHtml_StripsMarkdownImages()
    {
        var html = _markdown.ToSafeHtml("![alt](https://example.com/a.png)");

        Assert.DoesNotContain("<img", html);
        Assert.Contains("alt", html);
    }

    [Fact]
    public void ToSafeHtml_HandlesNull()
    {
        Assert.Equal(string.Empty, _markdown.ToSafeHtml(null!).Trim());
    }

    [Fact]
    public void ToPlainText_FlattensMarkdownToSingleLine()
    {
        var text = _markdown.ToPlainText("# Head\n\nsome **bold** text");

        Assert.Equal("Head some bold text", text);
    }

    [Fact]
    public void ToPlainText_HandlesNull()
    {
        Assert.Equal(string.Empty, _markdown.ToPlainText(null!));
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("short text", 1)]
    public void ReadingTime_HasMinimumOfOneMinute(string markdown, int expected)
    {
        Assert.Equal(expected, _markdown.ReadingTimeMinutes(markdown));
    }

    [Fact]
    public void ReadingTime_ScalesWithWordCount()
    {
        var sixHundredWords = string.Join(' ', Enumerable.Repeat("word", 600));
        Assert.Equal(3, _markdown.ReadingTimeMinutes(sixHundredWords));
    }
}
