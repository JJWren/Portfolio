using Markdig;

namespace Portfolio.Web.Services;

public class MarkdownService
{
    // Advanced extensions: tables, task lists, autolinks, fenced code, etc.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string ToHtml(string markdown)
        => Markdown.ToHtml(markdown ?? string.Empty, Pipeline);

    /// <summary>Estimated reading time at ~200 words per minute, minimum 1 minute.</summary>
    public int ReadingTimeMinutes(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return 1;
        }

        var words = markdown.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(words / 200.0));
    }
}
