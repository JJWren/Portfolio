using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pure-function tests for the daily-visitors chart's geometry and
/// formatting rules (Unit 14) — no database, no rendered component. See
/// <see cref="VisitorsChartRenderTests"/> for the rendered-markup pins.
/// </summary>
public class VisitorsChartRulesTests
{
    [Fact]
    public void ChartLayout_MatchesDocumentedGeometry()
    {
        var layout = VisitorsChartRules.Layout;

        Assert.Equal(720, layout.Width);
        Assert.Equal(240, layout.Height);
        Assert.Equal(44, layout.Left);
        Assert.Equal(12, layout.Right);
        Assert.Equal(12, layout.Top);
        Assert.Equal(28, layout.Bottom);
        Assert.Equal(664, layout.PlotWidth);
        Assert.Equal(200, layout.PlotHeight);
    }

    [Theory]
    [InlineData("2025-03-10", "2025-01-01", "2025-03-10")] // period start is later
    [InlineData("2025-01-01", "2025-03-10", "2025-03-10")] // first recorded day is later
    [InlineData("2025-01-01", "2025-01-01", "2025-01-01")] // equal
    public void RangeStart_ReturnsTheLaterOfTheTwoDays(string periodStart, string firstRecordedDay, string expected)
        => Assert.Equal(
            DateOnly.Parse(expected),
            VisitorsChartRules.RangeStart(DateOnly.Parse(periodStart), DateOnly.Parse(firstRecordedDay)));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(12, 20)]
    [InlineData(49, 50)]
    [InlineData(120, 200)]
    [InlineData(1000, 1000)]
    public void NiceMax_SmallestRoundNumberAtLeastMax(int max, int expected)
        => Assert.Equal(expected, VisitorsChartRules.NiceMax(max));

    [Theory]
    [InlineData(2, new[] { 0, 1, 2 })]
    [InlineData(10, new[] { 0, 5, 10 })]
    [InlineData(20, new[] { 0, 5, 10, 15, 20 })]
    [InlineData(50, new[] { 0, 25, 50 })]
    [InlineData(100, new[] { 0, 25, 50, 75, 100 })]
    [InlineData(200, new[] { 0, 50, 100, 150, 200 })]
    [InlineData(500, new[] { 0, 125, 250, 375, 500 })]
    [InlineData(1000, new[] { 0, 250, 500, 750, 1000 })]
    public void Ticks_EvenlySpacedRoundNumbersEndingAtNiceMax(int niceMax, int[] expected)
        => Assert.Equal(expected, VisitorsChartRules.Ticks(niceMax));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Ticks_SmallestScales_ReturnJustTheEndpoints(int niceMax)
    {
        // Neither a quarter nor a half of 1 or 5 is a whole number, so the
        // only round-number gridlines available are the two ends.
        var ticks = VisitorsChartRules.Ticks(niceMax);

        Assert.Equal(new[] { 0, niceMax }, ticks);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(15)]
    [InlineData(4000)]
    public void Ticks_AlwaysBetweenTwoAndFiveValues_FirstZeroLastNiceMaxNoDuplicates(int niceMax)
    {
        var ticks = VisitorsChartRules.Ticks(niceMax);

        Assert.InRange(ticks.Count, 2, 5);
        Assert.Equal(0, ticks[0]);
        Assert.Equal(niceMax, ticks[^1]);
        Assert.Equal(ticks.Count, ticks.Distinct().Count());
    }

    [Theory]
    [InlineData(0, 1, 376.0)] // a single point sits at the plot's horizontal centre
    [InlineData(0, 3, 44.0)]
    [InlineData(2, 3, 708.0)]
    [InlineData(1, 3, 376.0)]
    public void X_StaysInsideThePlot(int index, int count, double expected)
    {
        var x = VisitorsChartRules.X(index, count);

        Assert.Equal(expected, x);
        Assert.InRange(x, (double)VisitorsChartRules.Layout.Left, (double)(VisitorsChartRules.Layout.Width - VisitorsChartRules.Layout.Right));
    }

    [Theory]
    [InlineData(0, 5, 212.0)] // zero baseline sits on the plot's bottom edge
    [InlineData(5, 5, 12.0)] // niceMax sits on the plot's top edge
    [InlineData(3, 5, 92.0)]
    public void Y_StaysInsideThePlot(int value, int niceMax, double expected)
    {
        var y = VisitorsChartRules.Y(value, niceMax);

        Assert.Equal(expected, y);
        Assert.InRange(y, (double)VisitorsChartRules.Layout.Top, (double)(VisitorsChartRules.Layout.Height - VisitorsChartRules.Layout.Bottom));
    }

    [Fact]
    public void LinePath_WithoutToday_DrawsThroughEveryPoint()
    {
        var path = VisitorsChartRules.LinePath([2, 5, 3], niceMax: 5, lastIsToday: false);

        Assert.Equal("M44.0,132.0 L376.0,12.0 L708.0,92.0", path);
    }

    [Fact]
    public void LinePath_WithToday_OmitsTheFinalSegment()
    {
        var path = VisitorsChartRules.LinePath([2, 5, 3], niceMax: 5, lastIsToday: true);

        Assert.Equal("M44.0,132.0 L376.0,12.0", path);
    }

    [Fact]
    public void TodayPath_DrawsTheDashedFinalSegment_ContinuingFromWhereLinePathStopped()
    {
        var values = new[] { 2, 5, 3 };

        var linePath = VisitorsChartRules.LinePath(values, niceMax: 5, lastIsToday: true);
        var todayPath = VisitorsChartRules.TodayPath(values, niceMax: 5);

        Assert.Equal("M376.0,12.0 L708.0,92.0", todayPath);
        // No gap and no overlap: the dashed segment starts exactly where the
        // solid line's last drawn point left off.
        Assert.EndsWith("L376.0,12.0", linePath, StringComparison.Ordinal);
        Assert.StartsWith("M376.0,12.0", todayPath, StringComparison.Ordinal);
    }

    [Fact]
    public void LabelIndexes_TwoPoints_ReturnsBoth()
        => Assert.Equal(new[] { 0, 1 }, VisitorsChartRules.LabelIndexes(2));

    [Fact]
    public void LabelIndexes_SevenPoints_FiveIndexesFirstAndLastNoDuplicates()
        => Assert.Equal(new[] { 0, 2, 3, 5, 6 }, VisitorsChartRules.LabelIndexes(7));

    [Fact]
    public void LabelIndexes_ThirtyPoints_FiveIndexesFirstAndLastNoDuplicates()
        => Assert.Equal(new[] { 0, 7, 15, 22, 29 }, VisitorsChartRules.LabelIndexes(30));

    [Fact]
    public void LabelIndexes_365Points_FiveEvenlySpacedIndexes()
        => Assert.Equal(new[] { 0, 91, 182, 273, 364 }, VisitorsChartRules.LabelIndexes(365));

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(50)]
    public void LabelIndexes_AlwaysIncludesFirstAndLastAndNeverRepeats(int count)
    {
        var indexes = VisitorsChartRules.LabelIndexes(count);

        Assert.Contains(0, indexes);
        Assert.Contains(count - 1, indexes);
        Assert.Equal(indexes.Count, indexes.Distinct().Count());
    }

    [Theory]
    [InlineData(2025, 8, 14, "14 Aug")]
    [InlineData(2025, 1, 1, "1 Jan")]
    [InlineData(2025, 12, 31, "31 Dec")]
    public void FormatDay_UsesInvariantCultureShortForm(int year, int month, int day, string expected)
        => Assert.Equal(expected, VisitorsChartRules.FormatDay(new DateOnly(year, month, day)));

    [Fact]
    public void FormatCount_UsesInvariantCultureThousandsSeparator()
        => Assert.Equal("1,234", VisitorsChartRules.FormatCount(1234));

    [Theory]
    [InlineData(0, 7, "start")]
    [InlineData(3, 7, "start")]
    [InlineData(4, 7, "end")]
    [InlineData(6, 7, "end")]
    [InlineData(0, 2, "start")]
    [InlineData(1, 2, "end")]
    public void TooltipAnchor_FirstHalfStartsSecondHalfEnds(int index, int count, string expected)
        => Assert.Equal(expected, VisitorsChartRules.TooltipAnchor(index, count));
}
