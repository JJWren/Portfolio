using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins today's LandingSections markup (BR-12, BR-13) with a real Blazor
/// render. The HtmlRenderer/ServiceCollection ceremony and the
/// SiteConfig/EffectiveSiteContent builders live in
/// <see cref="LandingRenderHarness"/>, shared with AppCssTests.
/// </summary>
public class LandingSectionsRenderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"landing-render-tests-{Guid.NewGuid():N}");

    /// <summary>Creates a real (tiny) file at a fresh path under the per-test temp
    /// dir; OwnerPhotoService.GetVersionedUrl only checks File.Exists.</summary>
    private string CreatePhotoFile()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "owner-photo.webp");
        File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);
        return path;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    [Fact]
    public async Task Render_HeroHeading_AppearsInH1()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(heroHeading: "Position before submission."));

        Assert.Contains("<h1>Position before submission.</h1>", html);
    }

    [Fact]
    public async Task Render_TaglineSet_RendersTaglineParagraph()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(tagline: "Building useful things."));

        Assert.Contains("<p class=\"tagline\">Building useful things.</p>", html);
    }

    [Fact]
    public async Task Render_TaglineEmpty_OmitsTaglineParagraph()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(tagline: string.Empty));

        Assert.DoesNotContain("class=\"tagline\"", html);
    }

    [Fact]
    public async Task Render_About_RendersOneParagraphPerNonBlankLineAndDropsBlankLines()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(about: "First paragraph.\n\nSecond paragraph.\n"));

        Assert.Contains("<div class=\"about-text\">", html);
        Assert.Contains("<p>First paragraph.</p>", html);
        Assert.Contains("<p>Second paragraph.</p>", html);
        // The blank line between the two must not become a third, empty <p>.
        Assert.Equal(2, CountOccurrences(html, "<p>"));
    }

    [Fact]
    public async Task Render_Skills_RendersOneListItemPerSkill()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(skills: ["C#", "ASP.NET Core", "Docker"]));

        Assert.Contains("<ul class=\"skills\">", html);
        Assert.Contains("<li>C#</li>", html);
        Assert.Contains("<li>ASP.NET Core</li>", html);
        Assert.Contains("<li>Docker</li>", html);
        Assert.Equal(3, CountOccurrences(html, "<li>"));
    }

    [Fact]
    public async Task Render_NoAboutAndNoSkills_OmitsAboutSection()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(about: null, skills: []));

        Assert.DoesNotContain("class=\"eyebrow\"", html);
        Assert.DoesNotContain("about-text", html);
        Assert.DoesNotContain("class=\"skills\"", html);
    }

    [Fact]
    public async Task Render_GitHubUrlConfigured_RendersGitHubButton()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(gitHubUrl: "https://github.com/janedev"),
            LandingRenderHarness.BuildContent());

        Assert.Contains("href=\"https://github.com/janedev\"", html);
        Assert.Contains(">GitHub</a>", html);
    }

    [Fact]
    public async Task Render_GitHubUrlUnset_OmitsGitHubButton()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(gitHubUrl: null),
            LandingRenderHarness.BuildContent());

        Assert.DoesNotContain(">GitHub</a>", html);
    }

    [Fact]
    public async Task Render_LinkedInUrlConfigured_RendersLinkedInButton()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(linkedInUrl: "https://linkedin.com/in/janedev"),
            LandingRenderHarness.BuildContent());

        Assert.Contains("href=\"https://linkedin.com/in/janedev\"", html);
        Assert.Contains(">LinkedIn</a>", html);
    }

    [Fact]
    public async Task Render_LinkedInUrlUnset_OmitsLinkedInButton()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(linkedInUrl: null),
            LandingRenderHarness.BuildContent());

        Assert.DoesNotContain(">LinkedIn</a>", html);
    }

    [Fact]
    public async Task Render_Always_RendersGetInTouchLink()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent());

        Assert.Contains("href=\"/contact\"", html);
        Assert.Contains(">Get in touch</a>", html);
    }

    [Fact]
    public async Task Render_OwnerPhotoConfigured_RendersPhotoWithHasPhotoClassAndNormalizedAlt()
    {
        var photoPath = CreatePhotoFile();

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(ownerPhotoFile: photoPath),
            LandingRenderHarness.BuildContent(ownerPhotoAlt: "  Jane at her desk  "));

        Assert.Contains("class=\"container has-photo\"", html);
        Assert.Contains("<img class=\"owner-photo\"", html);
        Assert.Contains("alt=\"Jane at her desk\"", html);
        Assert.Contains("src=\"/owner-photo?v=", html);
    }

    [Fact]
    public async Task Render_OwnerPhotoUnconfigured_OmitsPhotoAndHasPhotoClass()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(ownerPhotoFile: null),
            LandingRenderHarness.BuildContent());

        Assert.DoesNotContain("has-photo", html);
        Assert.DoesNotContain("owner-photo", html);
    }

    [Fact]
    public async Task Render_FullContent_ContainsNoFixedPositioningAndNoScriptTags()
    {
        var photoPath = CreatePhotoFile();
        var site = LandingRenderHarness.BuildConfig(
            gitHubUrl: "https://github.com/janedev",
            linkedInUrl: "https://linkedin.com/in/janedev",
            ownerPhotoFile: photoPath);
        var content = LandingRenderHarness.BuildContent(
            tagline: "Building useful things.",
            about: "First paragraph.\nSecond paragraph.",
            skills: ["C#", "Docker"]);

        var html = await LandingRenderHarness.RenderAsync(site, content);

        // BR-13: LandingSections must render correctly inside the inert admin
        // theme preview, so nothing it emits may be fixed-position or scripted.
        Assert.DoesNotContain("position: fixed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("position:fixed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Render_ContentWithHtmlMetacharacters_EncodesEverything()
    {
        const string unsafeText = "<b>&\"</b>";
        var photoPath = CreatePhotoFile();

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(ownerPhotoFile: photoPath),
            LandingRenderHarness.BuildContent(heroHeading: unsafeText, tagline: unsafeText, ownerPhotoAlt: unsafeText));

        // HeroHeading, Tagline and OwnerPhotoAlt are all admin-editable copy
        // (SiteContentEditor) rendered as plain strings, never MarkupString,
        // so Blazor must HTML-encode them wherever they land: in a text node
        // (HeroHeading, Tagline) and in an attribute value (OwnerPhotoAlt).
        // Pins that this component can never emit admin copy unencoded.
        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp;", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"&lt;b&gt;&amp;&quot;&lt;/b&gt;\"", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
