using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Web.Components;
using Portfolio.Web.Services;

namespace Portfolio.Tests.Support;

/// <summary>
/// Shared HtmlRenderer ceremony for rendering <see cref="LandingSections"/>
/// in tests: no bUnit and no new package — HtmlRenderer ships in the
/// ASP.NET Core shared framework, which this test project already reaches
/// through its project reference to Portfolio.Web (a Microsoft.NET.Sdk.Web
/// project's FrameworkReference flows transitively to referencing
/// projects). Used by both LandingSectionsRenderTests and AppCssTests so
/// neither hand-rolls its own ServiceCollection/HtmlRenderer setup.
///
/// <see cref="RenderAsync"/> builds and disposes its own ServiceCollection
/// and HtmlRenderer per call rather than sharing one through a collection
/// fixture — the measured cost of a fresh renderer per test is negligible
/// for a component this size, and a shared instance would add async
/// disposal-ordering concerns for no real benefit.
/// </summary>
internal static class LandingRenderHarness
{
    public static async Task<string> RenderAsync(SiteConfig site, EffectiveSiteContent content)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(site);
        // By type, not a pre-built instance, so the harness keeps compiling
        // when the service grows constructor dependencies (its SiteConfig
        // comes from the registration above).
        services.AddSingleton<OwnerPhotoService>();
        await using var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<LandingSections>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Content"] = content }));
            return output.ToHtmlString();
        });
    }

    /// <summary>
    /// Renders <see cref="GamePlan"/> directly (no LandingSections wrapper
    /// and no SiteConfig/OwnerPhotoService dependency), so a test can drive
    /// its own defensive Nodes.Count guard independent of the caller's
    /// Content.GamePlan.Count gate in LandingSections.
    /// </summary>
    public static async Task<string> RenderGamePlanAsync(IReadOnlyList<GamePlanNode> nodes)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<GamePlan>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Nodes"] = nodes }));
            return output.ToHtmlString();
        });
    }

    /// <summary>Builds a SiteConfig with fixed, neutral defaults; pass only what a given test cares about varying.</summary>
    public static SiteConfig BuildConfig(
        string? gitHubUrl = null,
        string? linkedInUrl = null,
        string? ownerPhotoFile = null,
        SiteFlavor flavor = SiteFlavor.Default,
        string? heroEyebrow = null,
        IReadOnlyList<string>? gamePlanLines = null,
        string? beltCaption = null,
        int? beltDegrees = null,
        IReadOnlyList<string>? principleLines = null,
        IReadOnlyList<string>? eraLines = null,
        IReadOnlyList<string>? nowLines = null,
        string? ownerPhotoFlipFile = null)
        => new(
            OwnerName: "Jane Developer",
            SiteTitle: "Jane Developer — Portfolio",
            Tagline: string.Empty,
            MetaDescription: null,
            ContactEmail: "jane@example.com",
            ContactPhone: null,
            LinkedInUrl: linkedInUrl,
            GitHubUrl: gitHubUrl,
            About: null,
            Skills: [],
            SponsorUrl: null,
            SponsorText: "Buy me a coffee",
            OwnerPhotoFile: ownerPhotoFile,
            Flavor: flavor,
            HeroEyebrow: heroEyebrow,
            GamePlanLines: gamePlanLines,
            BeltCaption: beltCaption,
            BeltDegrees: beltDegrees,
            PrincipleLines: principleLines,
            EraLines: eraLines,
            NowLines: nowLines,
            OwnerPhotoFlipFile: ownerPhotoFlipFile);

    /// <summary>Builds an EffectiveSiteContent with fixed, neutral defaults; pass only what a given test cares about varying.</summary>
    public static EffectiveSiteContent BuildContent(
        string heroHeading = "Jane Developer",
        string tagline = "",
        string? about = null,
        IReadOnlyList<string>? skills = null,
        string ownerPhotoAlt = "Portrait of Jane Developer",
        string? heroEyebrow = null,
        IReadOnlyList<GamePlanNode>? gamePlan = null,
        string? beltCaption = null,
        int beltDegrees = 0,
        IReadOnlyList<Principle>? principles = null,
        IReadOnlyList<Era>? eras = null,
        IReadOnlyList<NowItem>? now = null,
        string ownerPhotoFlipAlt = "Jane on the mat")
        => new(
            HeroHeading: heroHeading,
            Tagline: tagline,
            About: about,
            Skills: skills ?? [],
            OwnerPhotoAlt: ownerPhotoAlt,
            HeroEyebrow: heroEyebrow,
            GamePlan: gamePlan,
            BeltCaption: beltCaption,
            BeltDegrees: beltDegrees,
            Principles: principles,
            Eras: eras,
            Now: now,
            OwnerPhotoFlipAlt: ownerPhotoFlipAlt);

    /// <summary>
    /// A SiteConfig with every optional branch populated (GitHub, LinkedIn,
    /// and — since <paramref name="ownerPhotoFile"/> and
    /// <paramref name="ownerPhotoFlipFile"/> must each name a file that
    /// really exists, OwnerPhotoService.GetVersionedUrl only checks
    /// File.Exists — two photos, so the hero's switch markup renders too),
    /// plus the Bjj flavor so every BJJ-only branch (Unit 10) also renders.
    /// Pair with <see cref="MaximalContent"/> so a render exercises every
    /// optional piece of markup LandingSections can emit today.
    ///
    /// IMPORTANT: AppCssTests' fixed-position cross-check (BR-13) walks the
    /// classes/ids/tags in a maximal render and assumes it has seen
    /// everything LandingSections can ever put on the page. Every later
    /// phase that adds a new optional SiteConfig/EffectiveSiteContent
    /// branch to LandingSections — the road, Now, the photo switch (see
    /// unit10-bjj-landing-plan.md) — MUST extend this pair to populate the
    /// new branch too, or the cross-check silently stops covering it.
    /// </summary>
    public static SiteConfig MaximalConfig(string ownerPhotoFile, string ownerPhotoFlipFile)
        => BuildConfig(
            gitHubUrl: "https://github.com/janedev",
            linkedInUrl: "https://linkedin.com/in/janedev",
            ownerPhotoFile: ownerPhotoFile,
            flavor: SiteFlavor.Bjj,
            ownerPhotoFlipFile: ownerPhotoFlipFile);

    /// <summary>See <see cref="MaximalConfig"/>.</summary>
    public static EffectiveSiteContent MaximalContent()
        => BuildContent(
            tagline: "Building useful things.",
            about: "First paragraph.\nSecond paragraph.",
            skills: ["C#", "Docker"],
            ownerPhotoAlt: "Jane at her desk",
            heroEyebrow: "Jane Developer · Software Engineer",
            gamePlan:
            [
                new GamePlanNode("Plan", "Plan the work", "Write it down first."),
                new GamePlanNode("Build", "Build the thing", "Small, reviewable commits."),
                new GamePlanNode("Test", "Prove it works", "Automate the boring parts."),
                new GamePlanNode("Ship", "Finish", string.Empty),
            ],
            beltCaption: "Test belt · Test gym, Test City",
            beltDegrees: 3,
            principles:
            [
                new Principle("Ship small.", "Small changes are easy to review and easy to revert."),
                new Principle("Write it down.", string.Empty),
            ],
            // Five eras spanning all five belts (so every .row[data-belt]
            // color map and every .belt-band color renders at least once)
            // plus a repeated belt (Purple) so the ladder/rung-collapsing
            // markup (fewer rungs than rows) also renders.
            eras:
            [
                new Era(new DateOnly(2010, 1, 1), Belt.White, 2, "Test Gym", "Test City", "Student."),
                new Era(new DateOnly(2012, 6, 15), Belt.Blue, 3, "Test Gym", "Test City", "Assistant instructor."),
                new Era(new DateOnly(2014, 3, 20), Belt.Purple, 1, "Test Gym", "Test City", "Competing."),
                new Era(new DateOnly(2016, 9, 9), Belt.Purple, 4, "Test Gym Two", "Test City Two", "Coaching."),
                new Era(new DateOnly(2018, 12, 1), Belt.Black, 3, "Test Gym Two", "Test City Two", "Head instructor."),
            ],
            now:
            [
                new NowItem("Training", "Evening classes."),
                new NowItem("Reading", "A long novel."),
            ]);
}
