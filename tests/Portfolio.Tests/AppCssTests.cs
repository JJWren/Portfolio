using System.Text.RegularExpressions;
using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins BR-13 (preview-safe): no rule that could ever style the landing page
/// may declare position: fixed. Built on <see cref="CssScanner"/> (a
/// brace-depth-aware scanner, not a real CSS parser — see its own summary
/// for what that trades away) and <see cref="LandingRenderHarness"/> (the
/// shared HtmlRenderer ceremony); this file adds the "does a fixed-position
/// selector's subject match something LandingSections renders" domain
/// check on top of both.
/// </summary>
public class AppCssTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"app-css-tests-{Guid.NewGuid():N}");

    // app.css is a few thousand lines; parsed once per test run (not once
    // per fact) so every fact below isn't independently re-reading and
    // re-scanning the whole file from disk.
    private static readonly Lazy<IReadOnlyList<CssRule>> AppCssRulesLazy =
        new(() => CssScanner.ParseLeafRules(ReadAppCss()));

    private static IReadOnlyList<CssRule> AppCssRules => AppCssRulesLazy.Value;

    private static string ReadAppCss()
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app.css"));

    /// <summary>Leaf rules whose declarations set `position: fixed` (or the no-space form).</summary>
    private static IEnumerable<CssRule> FixedPositionRules(IEnumerable<CssRule> rules)
        => rules.Where(static r => Regex.IsMatch(r.Declarations, @"position\s*:\s*fixed", RegexOptions.IgnoreCase));

    /// <summary>
    /// Every simple-selector token (lowercase tag name, ".class" or "#id")
    /// naming something in <paramref name="html"/>: every element's own
    /// tag, every class in a class="..." attribute, and every id="..."
    /// value. The universal selector "*" always matches non-empty rendered
    /// HTML, so callers special-case it instead of looking it up here.
    /// </summary>
    private static HashSet<string> RenderedTokens(string html)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match tag in Regex.Matches(html, @"<([A-Za-z][A-Za-z0-9]*)(?=[\s/>])"))
        {
            tokens.Add(tag.Groups[1].Value.ToLowerInvariant());
        }

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

    /// <summary>
    /// Every (selector, token) pair among <paramref name="fixedPositionRules"/>
    /// whose subject token matches <paramref name="renderedTokens"/> (or is
    /// the universal selector). Throws — rather than silently reporting "no
    /// violations" — for a selector whose subject yields no matchable token
    /// at all: that means the scanner has a gap, not that the rule is safe,
    /// and a gap here must never pass a test silently.
    /// </summary>
    private static IEnumerable<(string Selector, string Token)> FindViolations(
        IEnumerable<CssRule> fixedPositionRules,
        HashSet<string> renderedTokens)
    {
        foreach (var rule in fixedPositionRules)
        {
            var tokens = CssScanner.SubjectSelectorTokens(rule.Selector);
            if (tokens.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Selector '{rule.Selector}' declares position: fixed but the scanner could not extract " +
                    "any matchable simple-selector token from its subject. Extend " +
                    "CssScanner.SubjectSelectorTokens instead of letting a rule like this pass unchecked.");
            }

            foreach (var token in tokens)
            {
                if (token == "*" || renderedTokens.Contains(token))
                {
                    yield return (rule.Selector, token);
                }
            }
        }
    }

    /// <summary>Renders LandingSections with every optional piece (photo, tagline,
    /// about, skills, GitHub/LinkedIn links) present, so the collected token set
    /// covers everything the component can ever emit.</summary>
    private async Task<HashSet<string>> RenderMaximalTokensAsync()
    {
        Directory.CreateDirectory(_tempDir);
        var photoPath = Path.Combine(_tempDir, "owner-photo.webp");
        await File.WriteAllBytesAsync(photoPath, [0x00, 0x01, 0x02, 0x03]);

        var html = await LandingRenderHarness.RenderAsync(
            LandingRenderHarness.MaximalConfig(photoPath),
            LandingRenderHarness.MaximalContent());

        return RenderedTokens(html);
    }

    [Fact]
    public void FixedPositionSelectors_AppCssHasExactlyBlazorErrorUiFixed()
    {
        // Sanity check that the scanner itself works: today's one known
        // fixed-position rule must be found, or the cross-check below would
        // pass vacuously.
        var selector = Assert.Single(FixedPositionRules(AppCssRules)).Selector;

        Assert.Contains("#blazor-error-ui", selector, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FixedPositionSelectors_NeverNameAClassOrIdRenderedByLandingSections()
    {
        var renderedTokens = await RenderMaximalTokensAsync();

        var violations = FindViolations(FixedPositionRules(AppCssRules), renderedTokens).ToList();

        Assert.True(
            violations.Count == 0,
            string.Join(Environment.NewLine, violations.Select(v =>
                $"'{v.Selector}' declares position: fixed and also names '{v.Token}', which LandingSections " +
                "renders (BR-13: nothing it renders may be fixed-position).")));
    }

    [Theory]
    // Real id, but not one LandingSections ever renders.
    [InlineData("#blazor-error-ui { position: fixed; }")]
    // The only "position: fixed { }" text is inside a comment; must be ignored.
    [InlineData("/* .owner-photo { position: fixed; } */\n.owner-photo { color: red; }")]
    public async Task FixedPositionSelectors_SyntheticCssWithNoRenderedMatch_IsNotAViolation(string css)
    {
        var renderedTokens = await RenderMaximalTokensAsync();

        var violations = FindViolations(FixedPositionRules(CssScanner.ParseLeafRules(css)), renderedTokens).ToList();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(".theme-preview-frame img { position: fixed; }", "img")] // type subject, class ancestor ignored
    [InlineData("h1 { position: fixed; }", "h1")] // bare type selector
    [InlineData(".theme-preview-frame * { position: fixed; }", "*")] // universal subject
    [InlineData(".owner-photo:hover { position: fixed; }", ".owner-photo")] // pseudo-class ignored, class matches
    [InlineData("@media (min-width: 700px) { .owner-photo { position: fixed; } }", ".owner-photo")] // nested in @media
    [InlineData("a, .btn { position: fixed; }", "a")] // selector list
    public async Task FixedPositionSelectors_SyntheticCssWithRenderedMatch_IsAViolation(string css, string expectedToken)
    {
        var renderedTokens = await RenderMaximalTokensAsync();

        var violations = FindViolations(FixedPositionRules(CssScanner.ParseLeafRules(css)), renderedTokens).ToList();

        Assert.Contains(violations, v => v.Token == expectedToken);
    }

    [Fact]
    public void FixedPositionSelectors_SubjectWithNoMatchableTokens_FailsLoudlyInsteadOfPassingSilently()
    {
        var rules = CssScanner.ParseLeafRules(":hover { position: fixed; }");

        var ex = Assert.Throws<InvalidOperationException>(
            () => FindViolations(FixedPositionRules(rules), new HashSet<string>()).ToList());
        Assert.Contains(":hover", ex.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
