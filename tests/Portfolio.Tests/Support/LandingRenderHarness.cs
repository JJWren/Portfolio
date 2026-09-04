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
        IReadOnlyList<string>? principleLines = null)
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
            PrincipleLines: principleLines);

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
        IReadOnlyList<Principle>? principles = null)
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
            Principles: principles);

    /// <summary>
    /// A SiteConfig with every optional branch populated (GitHub, LinkedIn,
    /// and — since <paramref name="ownerPhotoFile"/> must name a file that
    /// really exists, OwnerPhotoService.GetVersionedUrl only checks
    /// File.Exists — a photo), plus the Bjj flavor so every BJJ-only branch
    /// (Unit 10) also renders. Pair with <see cref="MaximalContent"/> so a
    /// render exercises every optional piece of markup LandingSections can
    /// emit today.
    ///
    /// IMPORTANT: AppCssTests' fixed-position cross-check (BR-13) walks the
    /// classes/ids/tags in a maximal render and assumes it has seen
    /// everything LandingSections can ever put on the page. Every later
    /// phase that adds a new optional SiteConfig/EffectiveSiteContent
    /// branch to LandingSections — the road, Now, the photo switch (see
    /// unit10-bjj-landing-plan.md) — MUST extend this pair to populate the
    /// new branch too, or the cross-check silently stops covering it.
    /// </summary>
    public static SiteConfig MaximalConfig(string ownerPhotoFile)
        => BuildConfig(
            gitHubUrl: "https://github.com/janedev",
            linkedInUrl: "https://linkedin.com/in/janedev",
            ownerPhotoFile: ownerPhotoFile,
            flavor: SiteFlavor.Bjj);

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
            ]);
}
