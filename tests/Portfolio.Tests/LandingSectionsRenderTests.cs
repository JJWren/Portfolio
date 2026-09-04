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

    /// <summary>Every `&lt;div class="rung"&gt;...&lt;/div&gt;` block, in
    /// document order — Road.razor never nests a `&lt;div&gt;` inside a
    /// rung (only `&lt;span&gt;`s), so the non-greedy match always closes on
    /// the rung's own tag, never a nested one.</summary>
    private static List<string> ExtractRungBlocks(string html)
        => [.. Regex.Matches(html, @"<div class=""rung"">.*?</div>", RegexOptions.Singleline).Select(m => m.Value)];

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
                principles: [new Principle("Ship small.", "reading")],
                eras: [new Era(new DateOnly(2013, 4, 5), Belt.Brown, 4, "Gym", "City", "Role.")],
                now: [new NowItem("Training", "Evening classes.")]));

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
    public async Task Render_Bjj_RankBarDegreesAboveMax_ClampsToMaxStripes()
    {
        // BuildContent bypasses Resolve's clamp, so this pins the component's
        // own clamp: no caller can make the belt emit an unbounded stripe run.
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(beltCaption: "Black belt", beltDegrees: 99));

        Assert.Equal(BjjRules.MaxDegrees, CountBeltStripes(html));
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
        Assert.DoesNotContain("Two ladders, one clock", html);
        Assert.DoesNotContain("class=\"road\"", html);
        Assert.DoesNotContain("class=\"now\"", html);
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
        Assert.DoesNotContain("Two ladders, one clock", html);
        Assert.DoesNotContain("class=\"now\"", html);
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
                principles: [new Principle(unsafeText, unsafeText)],
                eras: [new Era(new DateOnly(2013, 4, 5), Belt.Brown, 4, unsafeText, unsafeText, unsafeText)],
                now: [new NowItem(unsafeText, unsafeText)]));

        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp;", html, StringComparison.Ordinal);
    }

    // -- BJJ landing flavor: The road (Unit 10 Phase 3) -------------------

    private static readonly Era[] FiveDistinctBeltEras =
    [
        new(new DateOnly(2010, 1, 1), Belt.White, 2, "Gym A", "City A", "Role A."),
        new(new DateOnly(2012, 6, 15), Belt.Blue, 3, "Gym B", "City B", "Role B."),
        new(new DateOnly(2014, 3, 20), Belt.Purple, 1, "Gym C", "City C", "Role C."),
        new(new DateOnly(2016, 9, 9), Belt.Brown, 4, "Gym D", "City D", "Role D."),
        new(new DateOnly(2018, 12, 1), Belt.Black, 0, "Gym E", "City E", "Role E."),
    ];

    [Fact]
    public async Task Render_Bjj_RoadHeadingAndFiveRowsWithDataBeltAndEraClassesInEnteredOrder()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(eras: FiveDistinctBeltEras));

        Assert.Contains("<p class=\"eyebrow\">The road</p>", html);
        Assert.Contains("<h2>Two ladders, one clock</h2>", html);
        Assert.Equal(5, CountOccurrences(html, "class=\"row era-"));
        Assert.Contains("class=\"row era-1\" data-belt=\"white\"", html);
        Assert.Contains("class=\"row era-2\" data-belt=\"blue\"", html);
        Assert.Contains("class=\"row era-3\" data-belt=\"purple\"", html);
        Assert.Contains("class=\"row era-4\" data-belt=\"brown\"", html);
        Assert.Contains("class=\"row era-5\" data-belt=\"black\"", html);
    }

    [Fact]
    public async Task Render_Bjj_RoadRowsCarryOneRowbeltBandIsoDatesAndTheRestOfTheFields()
    {
        Era[] eras = [new(new DateOnly(2013, 4, 5), Belt.Brown, 4, "Sample Gym", "Sample City", "Changing roles.")];

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(eras: eras));

        // The plain header cell (BR-16, refinement 6: no aria-hidden on the
        // th, so a screen reader sees a consistent column count) plus one
        // <td class="rowbelt"> per row, its BeltBand aria-hidden inside.
        Assert.Contains("<th class=\"rowbelt\"></th>", html);
        Assert.Equal(1, CountOccurrences(html, "<td class=\"rowbelt\">"));
        Assert.Contains("<time datetime=\"2013-04-05\">2013-04-05</time>", html);
        Assert.Contains("<td class=\"belt\" data-label=\"Belt\"><i class=\"swatch\" aria-hidden=\"true\"></i>Brown</td>", html);
        Assert.Contains("<td class=\"gym\" data-label=\"Gym\">Sample Gym</td>", html);
        Assert.Contains("<td class=\"place\" data-label=\"Location\">Sample City</td>", html);
        Assert.Contains("<td class=\"work\" data-label=\"Role\">Changing roles.</td>", html);
    }

    [Fact]
    public async Task Render_Bjj_LadderHasOneRungPerBeltInLadderOrderWithCorrectStripeCounts()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(eras: FiveDistinctBeltEras));

        var rungs = ExtractRungBlocks(html);
        Assert.Equal(5, rungs.Count);

        (string CssClass, string Name, int Stripes)[] expected =
        [
            ("white", "White", 2),
            ("blue", "Blue", 3),
            ("purple", "Purple", 1),
            ("brown", "Brown", 4),
            ("black", "Black", 0),
        ];

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Contains($"belt-band {expected[i].CssClass}", rungs[i]);
            Assert.Contains($"<span class=\"name\">{expected[i].Name}</span>", rungs[i]);
            Assert.Equal(expected[i].Stripes, CountOccurrences(rungs[i], "<i></i>"));
        }
    }

    [Fact]
    public async Task Render_Bjj_RepeatedBelt_ProducesFourRungsButFiveRows()
    {
        Era[] eras =
        [
            new(new DateOnly(2010, 1, 1), Belt.White, 2, "Gym", "City", "Role."),
            new(new DateOnly(2012, 6, 15), Belt.Blue, 3, "Gym", "City", "Role."),
            new(new DateOnly(2014, 3, 20), Belt.Purple, 1, "Gym", "City", "First purple era."),
            new(new DateOnly(2020, 1, 1), Belt.Purple, 4, "Gym", "City", "Second purple era."),
            new(new DateOnly(2018, 12, 1), Belt.Black, 0, "Gym", "City", "Role."),
        ];

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(eras: eras));

        Assert.Equal(4, ExtractRungBlocks(html).Count);
        Assert.Equal(5, CountOccurrences(html, "class=\"row era-"));
    }

    [Fact]
    public async Task Render_Bjj_NoEras_OmitsRoadSection()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(eras: []));

        Assert.DoesNotContain("Two ladders, one clock", html);
        Assert.DoesNotContain("class=\"road\"", html);
    }

    // -- BJJ landing flavor: Now (Unit 10 Phase 3) -------------------------

    [Fact]
    public async Task Render_Bjj_NowRendersFourDtDdPairs()
    {
        NowItem[] items =
        [
            new("Training", "Evening classes."),
            new("Reading", "A long novel."),
            new("Cooking", "Weeknight meals."),
            new("Travel", "Coast roads."),
        ];

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(now: items));

        Assert.Contains("<p class=\"eyebrow\">Now</p>", html);
        Assert.Contains("<dl class=\"now\">", html);
        Assert.Equal(4, CountOccurrences(html, "<dt>"));
        Assert.Equal(4, CountOccurrences(html, "<dd>"));
        Assert.Contains("<dt>Training</dt>", html);
        Assert.Contains("<dd>Evening classes.</dd>", html);
        Assert.Contains("<dt>Travel</dt>", html);
        Assert.Contains("<dd>Coast roads.</dd>", html);
    }

    [Fact]
    public async Task Render_Bjj_NoNowItems_OmitsNowSection()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(now: []));

        Assert.DoesNotContain("class=\"now\"", html);
    }

    [Fact]
    public async Task Render_Bjj_RoadAndNowBlankHideIndependently()
    {
        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.BuildConfig(flavor: SiteFlavor.Bjj),
            LandingRenderHarness.BuildContent(
                eras: [new Era(new DateOnly(2013, 4, 5), Belt.Brown, 4, "Gym", "City", "Role.")],
                now: []));

        Assert.Contains("Two ladders, one clock", html);
        Assert.DoesNotContain("class=\"now\"", html);
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
