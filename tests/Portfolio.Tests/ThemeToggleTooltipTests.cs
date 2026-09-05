using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins the theme toggle's BJJ tooltip wording (Unit 10 addendum, FR-B1 to
/// FR-B5) the way NoInlineOnClickTests pins the delegated handlers: by
/// scanning the linked source files in the test output rather than driving
/// a browser. App.razor must carry the data-flavor attribute fed by
/// SiteFlavorRules.HtmlDataFlavor (null under the default flavor, so the
/// plain page's markup is unchanged); theme.js must gate on that attribute
/// and hold each approved sentence exactly once; MainLayout's toggle must
/// keep its server-rendered title and its functional aria-label. One
/// rendered probe pins the framework behaviour FR-B1 relies on: a null
/// attribute value is omitted, not rendered empty.
/// </summary>
public class ThemeToggleTooltipTests
{
    private const string ShownWhileDark = "Switch to the white gi (light theme)";
    private const string ShownWhileLight = "Switch to the black gi (dark theme)";

    private static string Linked(string relativePath)
    {
        // Sanity check on the harness: a missing link must name its cause,
        // not surface as a FileNotFoundException.
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/CopyToOutputDirectory items in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

    [Fact]
    public void AppRazor_HtmlElement_CarriesTheFlavorAttribute()
    {
        var app = Linked(Path.Combine("RazorComponents", "App.razor"));

        // Pins the attribute on the <html> tag, whatever its other
        // attributes are. App.razor qualifies every service name in full
        // (its injects and its code block do the same); the optional group
        // keeps this pin from breaking on a later simplify-name cleanup,
        // which would change no behaviour.
        Assert.Matches(
            new Regex(@"<html\b[^>]*\bdata-flavor=""@(Portfolio\.Web\.Services\.)?SiteFlavorRules\.HtmlDataFlavor\(Site\.Flavor\)"""),
            app);
    }

    [Fact]
    public void ThemeJs_GatesOnTheFlavorAttribute_AndHoldsEachSentenceOnce()
    {
        var themeJs = Linked(Path.Combine("js", "theme.js"));

        Assert.Contains("document.documentElement.dataset.flavor !== 'bjj'", themeJs, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(themeJs, Regex.Escape(ShownWhileDark)));
        Assert.Single(Regex.Matches(themeJs, Regex.Escape(ShownWhileLight)));
    }

    [Fact]
    public void MainLayout_ThemeToggle_KeepsItsServerTitleAndAriaLabel()
    {
        var layout = Linked(Path.Combine("RazorComponents", "Layout", "MainLayout.razor"));

        // Scoped to the toggle's own opening tag, so both attributes must
        // sit on that button rather than anywhere in the file.
        var toggle = Regex.Match(layout, @"<button class=""theme-toggle""[^>]*>");
        Assert.True(toggle.Success, "MainLayout.razor should contain the theme-toggle button.");
        Assert.Contains("title=\"Switch theme\"", toggle.Value, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Switch between dark and light theme\"", toggle.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Renderer_OmitsTheFlavorAttribute_UnderTheDefaultFlavor()
        => Assert.Equal("<html lang=\"en\"></html>", await RenderProbeAsync(SiteFlavor.Default));

    [Fact]
    public async Task Renderer_WritesTheFlavorAttribute_UnderTheBjjFlavor()
        => Assert.Equal("<html lang=\"en\" data-flavor=\"bjj\"></html>", await RenderProbeAsync(SiteFlavor.Bjj));

    /// <summary>
    /// A stand-in for App.razor's root element built with the same attribute
    /// API the Razor compiler emits for data-flavor="@expr" (AddAttribute
    /// with a string value), so the rendered output pins what FR-B1 relies
    /// on: a null value omits the attribute; "bjj" writes it. App.razor
    /// itself is not rendered here because its router, asset map and theme
    /// store need the full host.
    /// </summary>
    internal sealed class FlavorAttributeProbe : ComponentBase
    {
        [Parameter]
        public SiteFlavor Flavor { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "html");
            builder.AddAttribute(1, "lang", "en");
            builder.AddAttribute(2, "data-flavor", SiteFlavorRules.HtmlDataFlavor(Flavor));
            builder.CloseElement();
        }
    }

    private static async Task<string> RenderProbeAsync(SiteFlavor flavor)
    {
        // The same HtmlRenderer ceremony as LandingRenderHarness, minus the
        // services the probe does not need.
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<FlavorAttributeProbe>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Flavor"] = flavor }));
            return output.ToHtmlString();
        });
    }
}
