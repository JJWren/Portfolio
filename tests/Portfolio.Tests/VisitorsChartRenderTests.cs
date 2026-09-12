using System.Text.RegularExpressions;
using Portfolio.Tests.Support;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins today's VisitorsChart markup with a real Blazor render (Unit 14),
/// the same HtmlRenderer technique <see cref="LandingSectionsRenderTests"/>
/// uses — see <see cref="LandingRenderHarness.RenderVisitorsChartAsync"/>.
/// Pure geometry and formatting are covered separately by
/// <see cref="VisitorsChartRulesTests"/>; this file pins what the component
/// itself decides: structure, counts, and the today/empty-state branches.
/// Dates and counts throughout are invented (BR-18) — none of this is real
/// site traffic.
/// </summary>
public class VisitorsChartRenderTests
{
    /// <summary>Every `&lt;tr&gt;...&lt;/tr&gt;` block, in document order —
    /// the data table never nests a `&lt;tr&gt;` inside a row (only
    /// `&lt;td&gt;`s), so the non-greedy match always closes on the row's
    /// own tag, never a nested one.</summary>
    private static List<string> ExtractTrBlocks(string html)
        => [.. Regex.Matches(html, "<tr>.*?</tr>", RegexOptions.Singleline).Select(m => m.Value)];

    private static readonly DailyVisitorPoint[] SevenPointsNoneToday =
    [
        new(new DateOnly(2025, 3, 1), 5, false),
        new(new DateOnly(2025, 3, 2), 12, false),
        new(new DateOnly(2025, 3, 3), 0, false),
        new(new DateOnly(2025, 3, 4), 8, false),
        new(new DateOnly(2025, 3, 5), 20, false),
        new(new DateOnly(2025, 3, 6), 3, false),
        new(new DateOnly(2025, 3, 7), 7, false),
    ];

    private static readonly DailyVisitorPoint[] SevenPointsLastIsToday =
    [
        new(new DateOnly(2025, 3, 1), 5, false),
        new(new DateOnly(2025, 3, 2), 12, false),
        new(new DateOnly(2025, 3, 3), 0, false),
        new(new DateOnly(2025, 3, 4), 8, false),
        new(new DateOnly(2025, 3, 5), 20, false),
        new(new DateOnly(2025, 3, 6), 3, false),
        new(new DateOnly(2025, 3, 7), 7, true),
    ];

    [Fact]
    public async Task Render_SevenPoints_HeadingAndCaptionPresent()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        Assert.Contains("<h2 id=\"visitors-chart-title\">Daily visitors</h2>", html, StringComparison.Ordinal);
        Assert.Contains("<p class=\"muted\">one point per UTC day</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_HasSkipLinkAndDataSummaryId()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        // The skip link escapes the run of up to 365 day tab-stops straight
        // to the data table (FR-V9/accessibility remediation); the <details>
        // summary must carry the id the link points at.
        Assert.Contains(
            "<a class=\"skip-link\" href=\"#visitors-data-summary\">Skip the daily detail</a>",
            html, StringComparison.Ordinal);
        Assert.Contains("<summary id=\"visitors-data-summary\">", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_OnePoint_ShowsEmptyStateWithNoSvgOrTable()
    {
        DailyVisitorPoint[] points = [new(new DateOnly(2025, 3, 1), 5, false)];

        var html = await LandingRenderHarness.RenderVisitorsChartAsync(points);

        Assert.Contains("Not enough data to chart yet.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_ZeroPoints_ShowsEmptyState()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync([]);

        Assert.Contains("Not enough data to chart yet.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_SevenDayGroups()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        // "<g class=\"day\"" (with the closing quote) never matches the
        // "<g class=\"days\">" wrapper: the character right after "day" in
        // that wrapper is 's', not a quote.
        Assert.Equal(7, LandingRenderHarness.CountOccurrences(html, "<g class=\"day\""));
    }

    [Fact]
    public async Task Render_SevenPoints_SevenTableRowsWithRightDatesAndCounts()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        // 7 data rows plus the <thead> header row.
        Assert.Equal(8, LandingRenderHarness.CountOccurrences(html, "<tr>"));

        (string Date, int Visitors)[] expected =
        [
            ("2025-03-01", 5),
            ("2025-03-02", 12),
            ("2025-03-03", 0),
            ("2025-03-04", 8),
            ("2025-03-05", 20),
            ("2025-03-06", 3),
            ("2025-03-07", 7),
        ];

        var rows = ExtractTrBlocks(html);
        // The header row plus one row per point.
        Assert.Equal(expected.Length + 1, rows.Count);

        for (var i = 0; i < expected.Length; i++)
        {
            // Row 0 is the <thead> header row, so the data rows start at 1;
            // asserting both cells against the SAME extracted row (rather
            // than against the page as a whole) pins each date to its own
            // count, not just that both sets appear somewhere in the html.
            var row = rows[i + 1];
            Assert.Contains($"<td>{expected[i].Date}</td>", row, StringComparison.Ordinal);
            Assert.Contains($"<td>{expected[i].Visitors}</td>", row, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Render_LastPointIsToday_TodayRowCellSaysTodaySoFarAndDashedSegmentRenders()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsLastIsToday);

        Assert.Contains("<td>2025-03-07 today so far</td>", html, StringComparison.Ordinal);
        Assert.Contains("<path class=\"today\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_LastPointIsNotToday_PlainDateCellAndNoDashedSegment()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        Assert.Contains("<td>2025-03-07</td>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("today so far", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<path class=\"today\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_OneAriaLabelPerDayWithTheRightWording()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        Assert.Equal(7, LandingRenderHarness.CountOccurrences(html, "aria-label=\""));
        Assert.Contains("aria-label=\"1 Mar: 5 visitors\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"3 Mar: 0 visitors\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"5 Mar: 20 visitors\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_LastPointIsToday_TodayAriaLabelSaysTodaySoFar()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsLastIsToday);

        Assert.Contains("aria-label=\"7 Mar, today so far: 7 visitors\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_ContainsNoScriptNoStyleAttributeAndNoExternalUrl()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsLastIsToday);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http", html, StringComparison.OrdinalIgnoreCase);
    }
}
