using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class BlogFiltersTests
{
    [Fact]
    public void TryParseMonth_ProducesUtcMonthWindow()
    {
        Assert.True(BlogFilters.TryParseMonth("2026-07", out var start, out var end));
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), end);
        Assert.Equal(DateTimeKind.Utc, start.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("2026-13")]
    [InlineData("July 2026")]
    [InlineData("2026-07-15")]
    public void TryParseMonth_RejectsNonMonths(string? input)
    {
        Assert.False(BlogFilters.TryParseMonth(input, out _, out _));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" docker ", "docker")]
    public void Normalize_TrimsAndCollapsesBlank(string? input, string? expected)
    {
        Assert.Equal(expected, BlogFilters.Normalize(input));
    }

    [Fact]
    public void EscapeLike_EscapesWildcardsAndBackslashes()
    {
        Assert.Equal("100\\% \\_done\\\\", BlogFilters.EscapeLike("100% _done\\"));
    }
}
