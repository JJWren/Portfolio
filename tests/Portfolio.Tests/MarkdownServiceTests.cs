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
