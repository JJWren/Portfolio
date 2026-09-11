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

        Assert.Contains("<h2 id=\"visitors-chart-title\">Daily visitors</h2>", html);
        Assert.Contains("<p class=\"muted\">one point per UTC day</p>", html);
    }

    [Fact]
    public async Task Render_OnePoint_ShowsEmptyStateWithNoSvgOrTable()
    {
        DailyVisitorPoint[] points = [new(new DateOnly(2025, 3, 1), 5, false)];

        var html = await LandingRenderHarness.RenderVisitorsChartAsync(points);

        Assert.Contains("Not enough data to chart yet.", html);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_ZeroPoints_ShowsEmptyState()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync([]);

        Assert.Contains("Not enough data to chart yet.", html);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_SevenDayGroups()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        // "<g class=\"day\"" (with the closing quote) never matches the
        // "<g class=\"days\">" wrapper: the character right after "day" in
        // that wrapper is 's', not a quote.
        Assert.Equal(7, CountOccurrences(html, "<g class=\"day\""));
    }

    [Fact]
    public async Task Render_SevenPoints_SevenTableRowsWithRightDatesAndCounts()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        // 7 data rows plus the <thead> header row.
        Assert.Equal(8, CountOccurrences(html, "<tr>"));

        Assert.Contains("<td>2025-03-01</td>", html);
        Assert.Contains("<td>2025-03-02</td>", html);
        Assert.Contains("<td>2025-03-03</td>", html);
        Assert.Contains("<td>2025-03-04</td>", html);
        Assert.Contains("<td>2025-03-05</td>", html);
        Assert.Contains("<td>2025-03-06</td>", html);
        Assert.Contains("<td>2025-03-07</td>", html);

        // The seven invented visitor counts, each rendered N0 in its own cell.
        Assert.Contains("<td>5</td>", html);
        Assert.Contains("<td>12</td>", html);
        Assert.Contains("<td>0</td>", html);
        Assert.Contains("<td>8</td>", html);
        Assert.Contains("<td>20</td>", html);
        Assert.Contains("<td>3</td>", html);
        Assert.Contains("<td>7</td>", html);
    }

    [Fact]
    public async Task Render_LastPointIsToday_TodayRowCellSaysTodaySoFarAndDashedSegmentRenders()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsLastIsToday);

        Assert.Contains("<td>2025-03-07 today so far</td>", html);
        Assert.Contains("<path class=\"today\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_LastPointIsNotToday_PlainDateCellAndNoDashedSegment()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        Assert.Contains("<td>2025-03-07</td>", html);
        Assert.DoesNotContain("today so far", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<path class=\"today\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_SevenPoints_OneAriaLabelPerDayWithTheRightWording()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsNoneToday);

        Assert.Equal(7, CountOccurrences(html, "aria-label=\""));
        Assert.Contains("aria-label=\"1 Mar: 5 visitors\"", html);
        Assert.Contains("aria-label=\"3 Mar: 0 visitors\"", html);
        Assert.Contains("aria-label=\"5 Mar: 20 visitors\"", html);
    }

    [Fact]
    public async Task Render_LastPointIsToday_TodayAriaLabelSaysTodaySoFar()
    {
        var html = await LandingRenderHarness.RenderVisitorsChartAsync(SevenPointsLastIsToday);

        Assert.Contains("aria-label=\"7 Mar, today so far: 7 visitors\"", html);
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
