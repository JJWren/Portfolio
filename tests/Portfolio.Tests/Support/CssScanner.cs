using System.Text.RegularExpressions;

namespace Portfolio.Tests.Support;

/// <summary>
/// One leaf CSS rule (a <c>{ ... }</c> block with no rule nested inside it)
/// from <see cref="CssScanner.ParseLeafRules"/>: its own selector list, its
/// declaration block text, and the enclosing at-rule preludes (<c>@media
/// (...)</c>, <c>@supports (...)</c>, <c>@keyframes name</c>), outermost
/// first, that it sits inside.
/// </summary>
internal sealed record CssRule(string Selector, string Declarations, IReadOnlyList<string> Ancestors);

/// <summary>
/// A brace-depth-aware CSS scanner good enough for this repo's
/// hand-authored app.css — not a real CSS parser. Known limitations, all
/// unexercised by this repo's app.css today: comments and quoted string
/// literals are blanked everywhere in the input (including inside a
/// selector's own attribute-value quotes — <c>[data-h="nw"]</c> becomes
/// <c>[data-h=  ]</c> in a stored Selector/Ancestors string) before any
/// brace-counting runs, so a literal brace inside either can never desync
/// the parser, at the cost of that cosmetic loss on selector text; there is
/// no handling of a backslash-escaped quote inside a string literal; there
/// is no CSS-nesting support (only at-rules can hold nested rules here);
/// and an attribute selector whose value contains a space or a combinator
/// character (for example <c>[title="a &gt; b"]</c>) would be split
/// incorrectly by <see cref="SubjectSelectorTokens"/>.
/// </summary>
internal static class CssScanner
{
    private static readonly Regex CommentPattern = new(@"/\*.*?\*/", RegexOptions.Singleline);
    private static readonly Regex StringLiteralPattern = new("\"[^\"]*\"|'[^']*'");

    // Matches one "piece" of a compound selector at a time: the universal
    // selector, a pseudo-class/element (with an optional (...) argument, e.g.
    // ::before or :nth-child(2)), an attribute selector, a class, an id, or a
    // type name. Order matters only where leading characters could otherwise
    // be ambiguous; here every branch has a distinct leading character.
    private static readonly Regex SimpleSelectorToken = new(
        @"\*|::?[A-Za-z-]+(?:\([^)]*\))?|\[[^\]]*\]|\.[A-Za-z0-9_-]+|#[A-Za-z0-9_-]+|[A-Za-z][A-Za-z0-9_-]*");

    /// <summary>
    /// Every leaf rule in <paramref name="css"/>. An at-rule block that only
    /// ever holds other rules (<c>@media</c>, <c>@supports</c>, a
    /// <c>@keyframes</c> wrapping <c>from</c>/<c>to</c>/percentage rules) is
    /// never itself returned as a leaf — its prelude instead becomes an
    /// <see cref="CssRule.Ancestors"/> entry on every rule nested inside it.
    /// </summary>
    public static IReadOnlyList<CssRule> ParseLeafRules(string css)
    {
        var text = StripCommentsAndStrings(css);

        var results = new List<CssRule>();
        var stack = new Stack<Frame>();
        var tokenStart = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                var prelude = text[tokenStart..i].Trim();
                IReadOnlyList<string> ancestors = [];
                if (stack.Count > 0)
                {
                    var parent = stack.Peek();
                    // The enclosing block (e.g. "@media (...)") holds rules,
                    // not declarations, so its own body is never checked as
                    // a leaf below.
                    parent.HasNestedRule = true;
                    ancestors = [.. parent.Ancestors, parent.Prelude];
                }

                stack.Push(new Frame { Prelude = prelude, BodyStart = i + 1, Ancestors = ancestors });
                tokenStart = i + 1;
            }
            else if (text[i] == '}' && stack.Count > 0)
            {
                var frame = stack.Pop();
                if (!frame.HasNestedRule)
                {
                    results.Add(new CssRule(frame.Prelude, text[frame.BodyStart..i], frame.Ancestors));
                }

                tokenStart = i + 1;
            }
        }

        return results;
    }

    /// <summary>
    /// Leaf rules with an ancestor prelude containing
    /// <paramref name="atRulePreludeSubstring"/> (ordinal) — e.g.
    /// "prefers-reduced-motion" or "@supports" — so a later phase can ask
    /// "which selectors sit inside the reduced-motion block" or "is every
    /// rule declaring animation-timeline inside an @supports ancestor"
    /// without re-walking the rule list by hand.
    /// </summary>
    public static IEnumerable<CssRule> RulesInside(this IEnumerable<CssRule> rules, string atRulePreludeSubstring)
        => rules.Where(r => r.Ancestors.Any(a => a.Contains(atRulePreludeSubstring, StringComparison.Ordinal)));

    /// <summary>
    /// The simple-selector tokens — a lowercase type name, "*", ".class" or
    /// "#id" — naming the <i>subject</i> of every selector in a
    /// comma-separated selector list: the last compound in each selector's
    /// descendant/child/sibling combinator chain, since that's the element
    /// the rule's declarations actually paint. Ancestor compounds (the
    /// ".theme-preview-frame" in ".theme-preview-frame img") are
    /// deliberately dropped: a caller matching these tokens against
    /// rendered output can then only ever over-match an ancestor-only
    /// coincidence, never miss a genuine one — over-flagging is the safe
    /// direction. Pseudo-classes/elements (":hover", "::before") and
    /// attribute selectors ("[type='text']") are dropped too: they only
    /// narrow what the subject already names, never widen it, so they can
    /// never turn a real match into a miss.
    /// </summary>
    public static IReadOnlyList<string> SubjectSelectorTokens(string selectorList)
    {
        var tokens = new List<string>();
        foreach (var selector in selectorList.Split(','))
        {
            var compounds = Regex.Matches(selector, @"[^\s>+~]+").Select(static m => m.Value).ToArray();
            if (compounds.Length == 0)
            {
                continue;
            }

            var subject = compounds[^1];
            foreach (Match token in SimpleSelectorToken.Matches(subject))
            {
                var value = token.Value;
                if (value[0] is ':' or '[')
                {
                    continue; // Pseudo-class/element or attribute selector: narrows, doesn't widen.
                }

                tokens.Add(value[0] is '.' or '#' or '*' ? value : value.ToLowerInvariant());
            }
        }

        return tokens;
    }

    /// <summary>
    /// Strips <c>/* ... */</c> comments and quoted string literals (blanked
    /// to spaces of the same length, quotes included) so a comment or a
    /// declaration like <c>content: "{"</c> can never desync the brace
    /// stack or fake a <c>position: fixed</c> match. Comments are stripped
    /// first, then strings, as two independent passes rather than one
    /// combined tokenizer — see the class summary for what that trades
    /// away.
    /// </summary>
    private static string StripCommentsAndStrings(string css)
    {
        var withoutComments = CommentPattern.Replace(css, string.Empty);
        return StringLiteralPattern.Replace(withoutComments, static m => new string(' ', m.Value.Length));
    }

    private sealed class Frame
    {
        public required string Prelude { get; init; }
        public required int BodyStart { get; init; }
        public required IReadOnlyList<string> Ancestors { get; init; }
        public bool HasNestedRule { get; set; }
    }
}
