using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Portfolio.Web.Services;

public class MarkdownService
{
    private const string UgcLinkRel = "nofollow ugc noopener";

    private static readonly char[] UrlDelimiters = ['/', '?', '#'];

    // Advanced extensions: tables, task lists, autolinks, fenced code, etc.
    // Trusted admin-authored content only (blog posts): raw HTML passes
    // through and GenericAttributes can attach arbitrary HTML attributes.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    // Visitor/user-generated content: raw HTML is escaped (DisableHtml) and
    // only hand-picked extensions run — deliberately not UseAdvancedExtensions,
    // whose GenericAttributes extension attaches arbitrary HTML attributes.
    // Single newlines render as line breaks, comment-style.
    private static readonly MarkdownPipeline UgcPipeline = BuildUgcPipeline();

    private static MarkdownPipeline BuildUgcPipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAutoLinks()
            .UsePipeTables()
            .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
            .UseSoftlineBreakAsHardlineBreak();
        builder.DocumentProcessed += SanitizeUgcDocument;
        return builder.Build();
    }

    public string ToHtml(string markdown)
        => Markdown.ToHtml(markdown ?? string.Empty, Pipeline);

    /// <summary>Renders user-generated markdown (comments, inbox and contact
    /// bodies) through the restricted pipeline.</summary>
    public string ToSafeHtml(string markdown)
        => Markdown.ToHtml(markdown ?? string.Empty, UgcPipeline);

    /// <summary>Single-line plain text of a markdown source, for excerpts.</summary>
    public string ToPlainText(string markdown)
        => string.Join(' ', Markdown.ToPlainText(markdown ?? string.Empty, UgcPipeline)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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

    // Second defense layer, applied to the syntax tree after parsing:
    // CommonMark treats javascript: destinations as legal and Markdig renders
    // them verbatim, so any scheme outside http/https/mailto (relative URLs
    // stay) is stripped to its label text, images collapse to their alt text,
    // and surviving links are tagged as untrusted user content.
    private static void SanitizeUgcDocument(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>().ToList())
        {
            if (link.IsImage || !HasSafeUrl(link.Url))
            {
                ReplaceWithLabel(link);
                continue;
            }

            link.GetAttributes().AddProperty("rel", UgcLinkRel);
        }

        foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
        {
            // Email autolinks carry a bare address; mailto: is added at render.
            if (!autolink.IsEmail && !HasSafeUrl(autolink.Url))
            {
                autolink.ReplaceBy(new LiteralInline(autolink.Url ?? string.Empty));
                continue;
            }

            autolink.GetAttributes().AddProperty("rel", UgcLinkRel);
        }
    }

    /// <summary>Hoists the link's children (its label or alt text) into the
    /// parent container, then removes the link itself.</summary>
    private static void ReplaceWithLabel(LinkInline link)
    {
        var child = link.FirstChild;
        while (child is not null)
        {
            var next = child.NextSibling;
            child.Remove();
            link.InsertBefore(child);
            child = next;
        }

        link.Remove();
    }

    /// <summary>Default-deny scheme check: relative URLs and http, https, and
    /// mailto absolute URLs pass; everything else is rejected.</summary>
    private static bool HasSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var colon = url.IndexOf(':');
        if (colon < 0)
        {
            return true;
        }

        var delimiter = url.IndexOfAny(UrlDelimiters);
        if (delimiter >= 0 && delimiter < colon)
        {
            return true; // the colon sits inside the path/query — no scheme
        }

        var scheme = url[..colon];
        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase);
    }
}
