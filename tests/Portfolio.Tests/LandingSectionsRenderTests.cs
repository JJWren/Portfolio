using System.Text.RegularExpressions;
using Portfolio.Tests.Support;
using Portfolio.Web.Services;

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

    /// <summary>Extracts the first `&lt;li class="{liClass}"&gt;...&lt;/li&gt;`
    /// block from rendered HTML, so a test can assert on what is nested
    /// inside one specific game-plan node rather than on the page as a
    /// whole (see the color-to-term pairing assertions below).</summary>
    private static string ExtractLiBlock(string html, string liClass)
    {
        var match = Regex.Match(html, $@"<li class=""{Regex.Escape(liClass)}"">.*?</li>", RegexOptions.Singleline);
        Assert.True(match.Success, $"Expected a <li class=\"{liClass}\"> block in the rendered HTML.");
        return match.Value;
    }

    [Fact]
    public async Task Render_HeroHeading_AppearsInH1()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent(heroHeading: "Sample heading"));

        Assert.Contains("<h1>Sample heading</h1>", html);
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

    // -- BJJ landing flavor (Unit 10) ------------------------------------

    [Fact]
    public async Task Render_DefaultFlavor_IdenticalWhetherOrNotBjjDataIsPresent()
    {
        var withoutBjjData = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(),
            LandingRenderHarness.BuildContent());

        var withBjjData = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Default),
            LandingRenderHarness.BuildContent(
                heroEyebrow: "Jane · Engineer",
                gamePlan:
                [
                    new GamePlanNode("Warm-up", "Loosen up", "How"),
                    new GamePlanNode("Drill", "Repeat the motion", "How"),
                    new GamePlanNode("Roll", "Test it live", "How"),
                    new GamePlanNode("Rest", "Recover", "How"),
                ],
                beltCaption: "Test belt",
                beltDegrees: 3,
                principles: [new Principle("Ship small.", "reading")]));

        // BR-1: under the Default flavor, the BJJ columns are simply never
        // consulted — present or not, the rendered markup is byte-for-byte
        // identical to v1.22.0.
        Assert.Equal(withoutBjjData, withBjjData);
    }

    [Fact]
    public async Task Render_Bjj_HeroEyebrowRendersBeforeH1()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(
                // Plain ASCII: HtmlRenderer entity-encodes non-ASCII
                // punctuation like the middle dot (· becomes &#xB7;), which
                // is correct, safe HTML but not literal-string-matchable.
                heroEyebrow: "Jane Developer - Software Engineer",
                heroHeading: "Sample heading"));

        Assert.Contains("<p class=\"eyebrow\">Jane Developer - Software Engineer</p>", html);
        var eyebrowIndex = html.IndexOf("class=\"eyebrow\"", StringComparison.Ordinal);
        var h1Index = html.IndexOf("<h1>Sample heading</h1>", StringComparison.Ordinal);
        Assert.True(eyebrowIndex >= 0);
        Assert.True(h1Index >= 0);
        Assert.True(eyebrowIndex < h1Index);
    }

    [Fact]
    public async Task Render_Default_OmitsHeroEyebrowEvenWhenSet()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Default, heroEyebrow: "Should not render"),
            LandingRenderHarness.BuildContent(heroEyebrow: "Should not render"));

        Assert.DoesNotContain("Should not render", html);
    }

    [Fact]
    public async Task Render_Bjj_GamePlanRendersFourPositionallyColoredLinksToPrinciples()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(gamePlan:
            [
                new GamePlanNode("Warm-up", "Loosen up", "Stretch first."),
                new GamePlanNode("Drill", "Repeat the motion", "Slow reps first."),
                new GamePlanNode("Roll", "Test it live", "Go at full speed."),
                new GamePlanNode("Rest", "Recover", string.Empty),
            ]));

        Assert.Contains("class=\"gp-node gp-red\"", html);
        Assert.Contains("class=\"gp-node gp-gold\"", html);
        Assert.Contains("class=\"gp-node gp-green\"", html);
        Assert.Contains("class=\"gp-node gp-blue\"", html);
        Assert.Equal(4, CountOccurrences(html, "href=\"#principles\""));
        Assert.Contains("<span class=\"term\">Warm-up</span>", html);
        Assert.Contains("<span class=\"read\">Loosen up</span>", html);
        Assert.Contains("<span class=\"how\">Stretch first.</span>", html);
        Assert.Contains("<span class=\"term\">Rest</span>", html);
        Assert.Contains("<span class=\"read\">Recover</span>", html);
        // Rest's How is blank: three nodes carry a .how span, not four.
        Assert.Equal(3, CountOccurrences(html, "class=\"how\""));

        // Pin the color-to-term pairing, not just that all four gp-* classes
        // and all four terms appear somewhere in the page: the first node
        // (red) must itself contain "Warm-up" and the last (blue) must
        // itself contain "Rest", each inside its own #principles link. This
        // fails if GamePlan.razor's positional class order were ever
        // swapped relative to the node order.
        var redNode = ExtractLiBlock(html, "gp-node gp-red");
        Assert.Contains("<span class=\"term\">Warm-up</span>", redNode);
        Assert.Contains("href=\"#principles\"", redNode);

        var blueNode = ExtractLiBlock(html, "gp-node gp-blue");
        Assert.Contains("<span class=\"term\">Rest</span>", blueNode);
        Assert.Contains("href=\"#principles\"", blueNode);
    }

    [Fact]
    public async Task Render_Bjj_GamePlanWithFewerThanFourNodes_IsHidden()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(gamePlan:
            [
                new GamePlanNode("Warm-up", "Loosen up", string.Empty),
                new GamePlanNode("Drill", "Repeat the motion", string.Empty),
            ]));

        Assert.DoesNotContain("game-plan", html);
    }

    [Fact]
    public async Task RenderGamePlan_ThreeNodes_RendersNothing()
    {
        // GamePlan.razor's own defensive guard (BR-5), exercised directly —
        // bypassing both LandingSections' Content.GamePlan.Count gate and
        // BjjRules.ParseGamePlan's exactly-four-or-empty rule, neither of
        // which this render goes through — so a caller that skips those
        // still can never make the component index out of range.
        var html = await LandingRenderHarness.RenderGamePlanAsync(
        [
            new GamePlanNode("Warm-up", "Loosen up", string.Empty),
            new GamePlanNode("Drill", "Repeat the motion", string.Empty),
            new GamePlanNode("Roll", "Test it live", string.Empty),
        ]);

        Assert.DoesNotContain("<ol class=\"game-plan\"", html);
    }

    [Fact]
    public async Task Render_Bjj_RankBarRendersDegreeStripesAndCaption()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(
                // Plain ASCII: see the comment on Render_Bjj_HeroEyebrowRendersBeforeH1.
                beltCaption: "Black belt - Test gym, Test City",
                beltDegrees: 3));

        Assert.Contains("<figure class=\"rank-bar\">", html);
        Assert.Contains("<div class=\"belt\" aria-hidden=\"true\">", html);
        Assert.Contains("<span class=\"belt-body\"></span>", html);
        Assert.Contains("<span class=\"belt-tip\"></span>", html);
        Assert.Contains("<figcaption>Black belt - Test gym, Test City</figcaption>", html);
        Assert.Equal(3, CountBeltStripes(html));
    }

    [Fact]
    public async Task Render_Bjj_RankBarZeroDegrees_RendersNoStripes()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(beltCaption: "Black belt", beltDegrees: 0));

        Assert.Equal(0, CountBeltStripes(html));
    }

    [Fact]
    public async Task Render_Bjj_NoBeltCaption_OmitsRankBar()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(beltCaption: null));

        Assert.DoesNotContain("rank-bar", html);
    }

    [Fact]
    public async Task Render_Bjj_PrinciplesSectionRendersIdAndOnePerPairWithBlankReadingOmitted()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(principles:
            [
                new Principle("Ship small.", "Small changes are safe."),
                new Principle("Write it down.", string.Empty),
            ]));

        Assert.Contains("<section class=\"section\" id=\"principles\">", html);
        Assert.Contains("<div class=\"principles\">", html);
        Assert.Contains("<h3>Ship small.</h3>", html);
        Assert.Contains("<p>Small changes are safe.</p>", html);
        Assert.Contains("<h3>Write it down.</h3>", html);
        Assert.Equal(2, CountOccurrences(html, "class=\"principle\""));
        // Only the first principle has a non-blank reading, so exactly one <p>.
        Assert.Equal(1, CountOccurrences(html, "<p>"));
    }

    [Fact]
    public async Task Render_Bjj_NoPrinciples_OmitsPrinciplesSection()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(principles: []));

        Assert.DoesNotContain("id=\"principles\"", html);
    }

    [Fact]
    public async Task Render_Bjj_EmptyBjjData_OmitsAllBjjSections()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent());

        Assert.DoesNotContain("class=\"eyebrow\"", html);
        Assert.DoesNotContain("game-plan", html);
        Assert.DoesNotContain("rank-bar", html);
        Assert.DoesNotContain("id=\"principles\"", html);
    }

    [Fact]
    public async Task Render_Bjj_OnlyHeroEyebrowSet_RendersOnlyEyebrowSection()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(heroEyebrow: "Solo eyebrow"));

        Assert.Contains("<p class=\"eyebrow\">Solo eyebrow</p>", html);
        Assert.DoesNotContain("game-plan", html);
        Assert.DoesNotContain("rank-bar", html);
        Assert.DoesNotContain("id=\"principles\"", html);
    }

    [Fact]
    public async Task Render_FullBjjContent_ContainsNoFixedPositioningAndNoScriptTags()
    {
        var photoPath = CreatePhotoFile();
        var site = LandingRenderHarness.MaximalConfig(photoPath);
        var content = LandingRenderHarness.MaximalContent();

        var html = await LandingRenderHarness.RenderAsync(site, content);

        // BR-13: LandingSections must render correctly inside the inert admin
        // theme preview, so nothing the BJJ flavor adds may be fixed-position
        // or scripted either.
        Assert.DoesNotContain("position: fixed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("position:fixed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Render_BjjContentWithHtmlMetacharacters_EncodesEverything()
    {
        const string unsafeText = "<b>&\"</b>";

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(
                heroEyebrow: unsafeText,
                gamePlan:
                [
                    new GamePlanNode(unsafeText, unsafeText, unsafeText),
                    new GamePlanNode("Drill", "Repeat the motion", string.Empty),
                    new GamePlanNode("Roll", "Test it live", string.Empty),
                    new GamePlanNode("Rest", "Recover", string.Empty),
                ],
                beltCaption: unsafeText,
                principles: [new Principle(unsafeText, unsafeText)]));

        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp;", html, StringComparison.Ordinal);
    }

    /// <summary>Counts the `&lt;i&gt;&lt;/i&gt;` degree stripes inside the
    /// rendered `.belt-bar` span specifically — the hero's always-present
    /// `.ribbons` block also renders four bare `&lt;i&gt;&lt;/i&gt;` tags, so
    /// a global count would over-count.</summary>
    private static int CountBeltStripes(string html)
    {
        var openTagStart = html.IndexOf("<span class=\"belt-bar\"", StringComparison.Ordinal);
        Assert.True(openTagStart >= 0, "Expected a .belt-bar span in the rendered HTML.");
        var contentStart = html.IndexOf('>', openTagStart) + 1;
        var closeTag = html.IndexOf("</span>", contentStart, StringComparison.Ordinal);
        return CountOccurrences(html[contentStart..closeTag], "<i></i>");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
