using System.Globalization;

namespace Portfolio.Web.Services;

/// <summary>
/// Pure geometry and formatting rules for the admin stats daily-visitors
/// chart (Unit 14): the "nice" vertical scale, gridline ticks, point
/// coordinates, the line and today-segment paths, the date-label spread and
/// wording, and which side a per-day tooltip opens on. No I/O and no state —
/// every member here is computed straight from its arguments, so it all
/// tests without a database or a rendered component.
/// </summary>
public static class VisitorsChartRules
{
    /// <summary>Fewer than this many points and the chart shows the empty
    /// state instead of a plot (FR-V10).</summary>
    public const int MinPoints = 2;

    private static readonly int[] StepMultipliers = [1, 2, 5];

    /// <summary>
    /// Fixed chart geometry: a 720x240 viewBox with the plot inset to leave
    /// room for the y-axis labels (left) and the date labels (bottom). One
    /// shared <see cref="Layout"/> instance backs every coordinate
    /// calculation below and the component's own markup, so the numbers in
    /// the rendered SVG and the numbers these functions compute can never
    /// drift apart.
    /// </summary>
    public sealed record ChartLayout
    {
        public int Width { get; init; } = 720;
        public int Height { get; init; } = 240;
        public int Left { get; init; } = 44;
        public int Right { get; init; } = 12;
        public int Top { get; init; } = 12;
        public int Bottom { get; init; } = 28;

        /// <summary>Plot width: the viewBox width minus both side insets.</summary>
        public int PlotWidth => Width - Left - Right;

        /// <summary>Plot height: the viewBox height minus the top and bottom insets.</summary>
        public int PlotHeight => Height - Top - Bottom;
    }

    /// <summary>The one chart layout every coordinate below is computed against.</summary>
    public static readonly ChartLayout Layout = new();

    /// <summary>
    /// The later of the period's own start and the first day the site has
    /// any recorded data, so the chart never draws days before the site
    /// existed (FR-V3).
    /// </summary>
    public static DateOnly RangeStart(DateOnly periodStart, DateOnly firstRecordedDay)
        => periodStart > firstRecordedDay ? periodStart : firstRecordedDay;

    /// <summary>
    /// The smallest round number of the form 1, 2 or 5 times a power of ten
    /// that is at least <paramref name="max"/> — the standard "nice number"
    /// step for a chart axis top. Zero or below (a real visitor count is
    /// never negative, but an empty period's max is 0) maps to 1: even the
    /// flattest chart needs a non-zero scale to divide by.
    /// </summary>
    public static int NiceMax(int max)
    {
        if (max <= 0)
        {
            return 1;
        }

        for (var scale = 1; ; scale *= 10)
        {
            foreach (var step in StepMultipliers)
            {
                var candidate = step * scale;
                if (candidate >= max)
                {
                    return candidate;
                }
            }
        }
    }

    /// <summary>
    /// Three to five evenly spaced, round-number gridline values from 0 to
    /// <paramref name="niceMax"/> inclusive: a quarter split (0, n/4, n/2,
    /// 3n/4, n) when every one of those is a whole number, else a half split
    /// (0, n/2, n) when the half is whole, else just the two endpoints
    /// (0, n). That last case is the smallest scales in the 1-2-5
    /// progression (<see cref="NiceMax"/> can return 1 or 5) where neither a
    /// quarter nor a half of the scale is a whole number, so the only
    /// round-number gridlines available are the ends themselves.
    /// </summary>
    public static IReadOnlyList<int> Ticks(int niceMax)
    {
        if (niceMax <= 0)
        {
            return [0];
        }

        if (niceMax % 4 == 0)
        {
            var quarter = niceMax / 4;
            return [0, quarter, quarter * 2, quarter * 3, niceMax];
        }

        if (niceMax % 2 == 0)
        {
            return [0, niceMax / 2, niceMax];
        }

        return [0, niceMax];
    }

    /// <summary>
    /// Horizontal position of point <paramref name="index"/> of
    /// <paramref name="count"/> evenly spaced points across the plot: the
    /// plot's horizontal centre for a single point (nothing to space out
    /// against), otherwise spread edge to edge with index 0 on the left
    /// plot edge and the last index on the right plot edge.
    /// </summary>
    public static double X(int index, int count)
        => count <= 1
            ? Layout.Left + Layout.PlotWidth / 2.0
            : Layout.Left + (index * (double)Layout.PlotWidth / (count - 1));

    /// <summary>
    /// Vertical position of <paramref name="value"/> against a vertical
    /// scale from 0 to <paramref name="niceMax"/>: 0 sits on the plot's
    /// bottom edge (the zero baseline, FR-V6) and <paramref name="niceMax"/>
    /// sits on its top edge.
    /// </summary>
    public static double Y(int value, int niceMax)
    {
        var scale = niceMax <= 0 ? 1 : niceMax;
        return Layout.Top + Layout.PlotHeight - (value * (double)Layout.PlotHeight / scale);
    }

    /// <summary>One coordinate, formatted the same way everywhere in the
    /// chart: invariant culture, one decimal place.</summary>
    public static string FormatCoordinate(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>
    /// The solid line's SVG path data through <paramref name="values"/> (one
    /// per day, in day order): every point when <paramref name="lastIsToday"/>
    /// is false, or every point except the last one when it is true — the
    /// component draws that final segment separately, dashed, via
    /// <see cref="TodayPath"/>, so the solid line stops one point short.
    /// </summary>
    public static string LinePath(IReadOnlyList<int> values, int niceMax, bool lastIsToday)
    {
        var count = values.Count;
        var solidCount = lastIsToday ? count - 1 : count;
        if (solidCount <= 0)
        {
            return string.Empty;
        }

        var segments = new string[solidCount];
        for (var i = 0; i < solidCount; i++)
        {
            var command = i == 0 ? "M" : "L";
            segments[i] = $"{command}{FormatCoordinate(X(i, count))},{FormatCoordinate(Y(values[i], niceMax))}";
        }

        return string.Join(" ", segments);
    }

    /// <summary>
    /// The dashed final segment's SVG path data, from the second-to-last
    /// point to the last (today's) point. Meaningful only when there are at
    /// least two points and the last one is today — the only time the
    /// component calls it.
    /// </summary>
    public static string TodayPath(IReadOnlyList<int> values, int niceMax)
    {
        var count = values.Count;
        var fromX = FormatCoordinate(X(count - 2, count));
        var fromY = FormatCoordinate(Y(values[^2], niceMax));
        var toX = FormatCoordinate(X(count - 1, count));
        var toY = FormatCoordinate(Y(values[^1], niceMax));
        return $"M{fromX},{fromY} L{toX},{toY}";
    }

    /// <summary>
    /// Indexes (into a <paramref name="count"/>-long point list) to draw a
    /// date label under: every index when there are <paramref name="target"/>
    /// or fewer points, otherwise <paramref name="target"/> indexes spread as
    /// evenly as rounding allows, with the first and last always exact and
    /// never repeated.
    /// </summary>
    public static IReadOnlyList<int> LabelIndexes(int count, int target = 5)
    {
        if (count <= 0)
        {
            return [];
        }

        if (count <= target)
        {
            return [.. Enumerable.Range(0, count)];
        }

        var step = (count - 1) / (double)(target - 1);
        var indexes = new List<int>(target);
        for (var i = 0; i < target; i++)
        {
            var index = (int)Math.Round(i * step, MidpointRounding.AwayFromZero);
            if (indexes.Count == 0 || indexes[^1] != index)
            {
                indexes.Add(index);
            }
        }

        // step * (target - 1) equals count - 1 exactly by construction, so
        // rounding can only ever collapse a neighbor into the index before
        // it, never displace the final index away from count - 1 — this is
        // a defensive fix-up, not a path this data can actually take.
        if (indexes[^1] != count - 1)
        {
            indexes[^1] = count - 1;
        }

        return indexes;
    }

    /// <summary>"14 Aug": day of month (no leading zero) and short month
    /// name, invariant culture — never the site visitor's own locale.</summary>
    public static string FormatDay(DateOnly day) => day.ToString("d MMM", CultureInfo.InvariantCulture);

    /// <summary>
    /// Which side a hovered day's tooltip should open on, as an SVG
    /// <c>text-anchor</c> value: "start" (the box grows to the right of the
    /// point) for the first half of the days, "end" (the box grows to the
    /// left) for the second half — so the box always grows toward the middle
    /// of the chart and away from the viewBox edge it would otherwise overflow.
    /// </summary>
    public static string TooltipAnchor(int index, int count) => index < count / 2.0 ? "start" : "end";
}
