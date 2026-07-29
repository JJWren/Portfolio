using System.Globalization;
using System.Text.Json;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SeoRulesTests
{
    private const string Origin = "https://example.dev";

    // -- CanonicalOrigin -----------------------------------------------------

    [Fact]
    public void CanonicalOrigin_Configured_WinsAndTrimsTrailingSlash()
        => Assert.Equal("https://site.dev",
            SeoRules.CanonicalOrigin("https://site.dev/", "http://localhost:8080"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CanonicalOrigin_Blank_FallsBackToTheRequestOrigin(string? configured)
        => Assert.Equal("http://localhost:8080",
            SeoRules.CanonicalOrigin(configured, "http://localhost:8080/"));

    [Fact]
    public void CanonicalOrigin_ConfiguredWithStrayWhitespace_IsTrimmed()
        => Assert.Equal("https://site.dev",
            SeoRules.CanonicalOrigin("  https://site.dev/  ", "http://localhost:8080"));

    // -- CanonicalUrl --------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    public void CanonicalUrl_Root_IsTheBareOrigin(string? path)
        => Assert.Equal(Origin, SeoRules.CanonicalUrl(Origin, path));

    [Fact]
    public void CanonicalUrl_TrailingSlash_Drops()
        => Assert.Equal($"{Origin}/blog", SeoRules.CanonicalUrl(Origin, "/blog/"));

    [Fact]
    public void CanonicalUrl_DeepPath_AppendsUnchanged()
        => Assert.Equal($"{Origin}/blog/welcome", SeoRules.CanonicalUrl(Origin, "/blog/welcome"));

    [Fact]
    public void CanonicalUrl_MissingLeadingSlash_GainsOne()
        => Assert.Equal($"{Origin}/social-card.png", SeoRules.CanonicalUrl(Origin, "social-card.png"));

    // -- AbsoluteUrl ---------------------------------------------------------

    [Theory]
    [InlineData("https://cdn.example/img.png")]
    [InlineData("http://cdn.example/img.png")]
    [InlineData("HTTPS://cdn.example/img.png")]
    public void AbsoluteUrl_AbsoluteHttp_PassesThrough(string url)
        => Assert.Equal(url, SeoRules.AbsoluteUrl(Origin, url));

    [Fact]
    public void AbsoluteUrl_RelativeUploadPath_GainsTheOrigin()
        => Assert.Equal($"{Origin}/uploads/a.jpg", SeoRules.AbsoluteUrl(Origin, "/uploads/a.jpg"));

    [Fact]
    public void AbsoluteUrl_FingerprintedAssetPath_GainsOriginAndSlash()
        => Assert.Equal($"{Origin}/social-card.abc123.png",
            SeoRules.AbsoluteUrl(Origin, "social-card.abc123.png"));

    // -- TruncateDescription ---------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \n\t  ")]
    public void TruncateDescription_Blank_IsEmpty(string? text)
        => Assert.Equal(string.Empty, SeoRules.TruncateDescription(text));

    [Fact]
    public void TruncateDescription_Short_PassesThrough()
        => Assert.Equal("Hello world.", SeoRules.TruncateDescription("Hello world."));

    [Fact]
    public void TruncateDescription_CollapsesInternalWhitespace()
        => Assert.Equal("one two three", SeoRules.TruncateDescription("one\n\n two\t  three"));

    [Fact]
    public void TruncateDescription_ExactlyAtTheLimit_PassesThrough()
    {
        var text = new string('a', SeoRules.DescriptionLimit);

        Assert.Equal(text, SeoRules.TruncateDescription(text));
    }

    [Fact]
    public void TruncateDescription_OverTheLimit_CutsAtAWordBoundaryWithEllipsis()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 60));

        var result = SeoRules.TruncateDescription(text);

        Assert.True(result.Length <= SeoRules.DescriptionLimit);
        Assert.EndsWith("word…", result);
    }

    [Fact]
    public void TruncateDescription_UnbrokenText_HardCutsUnderTheLimit()
    {
        var result = SeoRules.TruncateDescription(new string('a', 500));

        Assert.Equal(SeoRules.DescriptionLimit, result.Length);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void TruncateDescription_CustomLimit_IsHonored()
        => Assert.Equal("alpha beta…", SeoRules.TruncateDescription("alpha beta gamma delta", 12));

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-5)]
    public void TruncateDescription_LimitBelowTwo_Throws(int limit)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => SeoRules.TruncateDescription("anything", limit));

    // -- JSON-LD ---------------------------------------------------------------

    [Fact]
    public void PersonJsonLd_CarriesTheSchemaFields()
    {
        var json = SeoRules.PersonJsonLd("Jane Developer", Origin,
            ["https://github.com/jane", null, " "]);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("Person", root.GetProperty("@type").GetString());
        Assert.Equal("Jane Developer", root.GetProperty("name").GetString());
        Assert.Equal(Origin, root.GetProperty("url").GetString());
        Assert.Equal(new[] { "https://github.com/jane" },
            root.GetProperty("sameAs").EnumerateArray().Select(static e => e.GetString()).ToArray());
    }

    [Fact]
    public void PersonJsonLd_NoLinks_OmitsSameAs()
    {
        var json = SeoRules.PersonJsonLd("Jane", Origin, [null, ""]);

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("sameAs", out _));
    }

    [Fact]
    public void JsonLd_HtmlSensitiveText_CannotTerminateTheScriptBlock()
    {
        var hostile = "</script><script>alert(1)</script>";

        var json = SeoRules.PersonJsonLd(hostile, Origin, []);

        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(hostile, doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void WebSiteJsonLd_CarriesTheSchemaFields()
    {
        var json = SeoRules.WebSiteJsonLd("Jane — Portfolio", Origin);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("WebSite", doc.RootElement.GetProperty("@type").GetString());
        Assert.Equal("Jane — Portfolio", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(Origin, doc.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public void BlogPostingJsonLd_FullPayload_CarriesEverything()
    {
        var published = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2026, 7, 24, 8, 30, 0, DateTimeKind.Utc);

        var json = SeoRules.BlogPostingJsonLd(
            "Welcome", "First post.", $"{Origin}/blog/welcome", "Jane",
            published, modified, $"{Origin}/uploads/hero.jpg");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("BlogPosting", root.GetProperty("@type").GetString());
        Assert.Equal("Welcome", root.GetProperty("headline").GetString());
        Assert.Equal("First post.", root.GetProperty("description").GetString());
        Assert.Equal($"{Origin}/blog/welcome", root.GetProperty("url").GetString());
        Assert.Equal($"{Origin}/blog/welcome", root.GetProperty("mainEntityOfPage").GetString());
        Assert.Equal("Person", root.GetProperty("author").GetProperty("@type").GetString());
        Assert.Equal("Jane", root.GetProperty("author").GetProperty("name").GetString());
        Assert.Equal(published, DateTime.Parse(root.GetProperty("datePublished").GetString()!,
            null, DateTimeStyles.RoundtripKind));
        Assert.Equal(modified, DateTime.Parse(root.GetProperty("dateModified").GetString()!,
            null, DateTimeStyles.RoundtripKind));
        Assert.Equal($"{Origin}/uploads/hero.jpg", root.GetProperty("image").GetString());
    }

    [Fact]
    public void BlogPostingJsonLd_NoImage_OmitsTheImageField()
    {
        var json = SeoRules.BlogPostingJsonLd(
            "Welcome", "First post.", $"{Origin}/blog/welcome", "Jane", null, null, null);

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("image", out _));
    }

    [Fact]
    public void BlogPostingJsonLd_NoDates_OmitsBothDateFields()
    {
        var json = SeoRules.BlogPostingJsonLd(
            "Welcome", "First post.", $"{Origin}/blog/welcome", "Jane", null, null, null);

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("datePublished", out _));
        Assert.False(doc.RootElement.TryGetProperty("dateModified", out _));
    }

    [Fact]
    public void BlogPostingJsonLd_MissingModified_FallsBackToPublished()
    {
        var published = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);

        var json = SeoRules.BlogPostingJsonLd(
            "Welcome", "First post.", $"{Origin}/blog/welcome", "Jane", published, null, null);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(published, DateTime.Parse(doc.RootElement.GetProperty("dateModified").GetString()!,
            null, DateTimeStyles.RoundtripKind));
    }
}
