using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Web.Components;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins BR-13 (preview-safe): no rule that could ever style the landing page
/// may declare position: fixed. Not a CSS parser — a brace-depth-aware
/// scanner that isolates each leaf rule's own selector and declarations
/// (skipping over @media/@supports preludes, which hold no declarations of
/// their own), good enough for this repo's hand-authored app.css.
/// </summary>
public class AppCssTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"app-css-tests-{Guid.NewGuid():N}");

    private static string ReadAppCss()
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app.css"));

    /// <summary>
    /// Selector text (immediately before the `{`) of every leaf rule block
    /// (no rule nested inside it) whose declarations include
    /// `position: fixed` (or the no-space form).
    /// </summary>
    private static List<string> FindFixedPositionSelectors(string css)
    {
        // Comments could hide a brace or a "position: fixed" that isn't really
        // there; strip them before scanning.
        var text = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        var results = new List<string>();
        var stack = new Stack<(string Prelude, int BodyStart, bool HasNestedRule)>();
        var tokenStart = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                var prelude = text[tokenStart..i].Trim();
                if (stack.Count > 0)
                {
                    // The enclosing block (e.g. "@media (...)") holds rules,
                    // not declarations, so its own body is never checked below.
                    var parent = stack.Pop();
                    stack.Push((parent.Prelude, parent.BodyStart, true));
                }

                stack.Push((prelude, i + 1, false));
                tokenStart = i + 1;
            }
            else if (text[i] == '}' && stack.Count > 0)
            {
                var (leafPrelude, bodyStart, hasNestedRule) = stack.Pop();
                if (!hasNestedRule
                    && Regex.IsMatch(text[bodyStart..i], @"position\s*:\s*fixed", RegexOptions.IgnoreCase))
                {
                    results.Add(leafPrelude);
                }

                tokenStart = i + 1;
            }
        }

        return results;
    }

    /// <summary>Every `.class` / `#id` token named anywhere in a selector list.</summary>
    private static IEnumerable<string> SelectorTokens(string selectorList)
        => Regex.Matches(selectorList, @"[.#][A-Za-z0-9_-]+").Select(static m => m.Value);

    /// <summary>Every `.class` / `#id` token present on any element in rendered HTML.</summary>
    private static HashSet<string> RenderedSelectorTokens(string html)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match classAttr in Regex.Matches(html, @"class=""([^""]*)"""))
        {
            foreach (var name in classAttr.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                tokens.Add("." + name);
            }
        }

        foreach (Match idAttr in Regex.Matches(html, @"id=""([^""]*)"""))
        {
            tokens.Add("#" + idAttr.Groups[1].Value);
        }

        return tokens;
    }

    /// <summary>Renders LandingSections with every optional piece (photo, tagline,
    /// about, skills, GitHub/LinkedIn links) present, so the collected class set
    /// covers everything the component can ever emit.</summary>
    private async Task<string> RenderFullLandingHtmlAsync()
    {
        Directory.CreateDirectory(_tempDir);
        var photoPath = Path.Combine(_tempDir, "owner-photo.webp");
        await File.WriteAllBytesAsync(photoPath, [0x00, 0x01, 0x02, 0x03]);

        var site = new SiteConfig(
            OwnerName: "Jane Developer",
            SiteTitle: "Jane Developer — Portfolio",
            Tagline: string.Empty,
            MetaDescription: null,
            ContactEmail: "jane@example.com",
            ContactPhone: null,
            LinkedInUrl: "https://linkedin.com/in/janedev",
            GitHubUrl: "https://github.com/janedev",
            About: null,
            Skills: [],
            SponsorUrl: null,
            SponsorText: "Buy me a coffee",
            OwnerPhotoFile: photoPath);
        var content = new EffectiveSiteContent(
            HeroHeading: "Jane Developer",
            Tagline: "Building useful things.",
            About: "First paragraph.\nSecond paragraph.",
            Skills: ["C#", "Docker"],
            OwnerPhotoAlt: "Jane at her desk");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(site);
        services.AddSingleton(new OwnerPhotoService(site));
        await using var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<LandingSections>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Content"] = content }));
            return output.ToHtmlString();
        });
    }

    [Fact]
    public void FixedPositionSelectors_IncludesBlazorErrorUi()
    {
        // Sanity check that the scanner itself works: today's one known
        // fixed-position rule must be found, or the test below would pass
        // vacuously.
        var selectors = FindFixedPositionSelectors(ReadAppCss());

        Assert.Contains(selectors, s => s.Contains("#blazor-error-ui", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FixedPositionSelectors_NeverNameAClassOrIdRenderedByLandingSections()
    {
        var selectors = FindFixedPositionSelectors(ReadAppCss());
        var renderedTokens = RenderedSelectorTokens(await RenderFullLandingHtmlAsync());

        foreach (var selector in selectors)
        {
            foreach (var token in SelectorTokens(selector))
            {
                Assert.False(
                    renderedTokens.Contains(token),
                    $"'{selector}' declares position: fixed and also names '{token}', " +
                    "which LandingSections renders (BR-13: nothing it renders may be fixed-position).");
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
