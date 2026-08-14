using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class AnalyticsRollupTests
{
    private static readonly DateOnly Day = new(2026, 8, 13);

    private static PageView View(string path, string key, string? referrer = null)
        => new() { Path = path, VisitorKey = key, ReferrerHost = referrer };

    private static AnalyticsEvent Event(string name, string? target)
        => new() { Name = name, Target = target, VisitorKey = "k" };

    [Fact]
    public void Aggregate_EmptyDay_ProducesZeroSiteRow()
    {
        var result = AnalyticsRollup.Aggregate(Day, [], []);

        Assert.Equal(Day, result.Site.Day);
        Assert.Equal(0, result.Site.Views);
        Assert.Equal(0, result.Site.Visitors);
        Assert.Empty(result.Routes);
        Assert.Empty(result.Referrers);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Aggregate_CountsViewsAndDistinctVisitors()
    {
        var result = AnalyticsRollup.Aggregate(Day,
        [
            View("/", "a"), View("/", "a"), View("/", "b"),
            View("/blog", "a"),
        ], []);

        Assert.Equal(4, result.Site.Views);
        Assert.Equal(2, result.Site.Visitors);

        var home = Assert.Single(result.Routes, r => r.Path == "/");
        Assert.Equal(3, home.Views);
        Assert.Equal(2, home.Visitors);
        var blog = Assert.Single(result.Routes, r => r.Path == "/blog");
        Assert.Equal(1, blog.Views);
        Assert.Equal(1, blog.Visitors);
    }

    [Fact]
    public void Aggregate_GroupsReferrersIgnoringDirectVisits()
    {
        var result = AnalyticsRollup.Aggregate(Day,
        [
            View("/", "a", "news.ycombinator.com"),
            View("/", "b", "news.ycombinator.com"),
            View("/", "c", "google.com"),
            View("/", "d"),
        ], []);

        Assert.Equal(2, result.Referrers.Count);
        Assert.Equal(2, Assert.Single(result.Referrers, r => r.ReferrerHost == "news.ycombinator.com").Views);
        Assert.Equal(1, Assert.Single(result.Referrers, r => r.ReferrerHost == "google.com").Views);
    }

    [Fact]
    public void Aggregate_GroupsEventsByNameAndTarget()
    {
        var result = AnalyticsRollup.Aggregate(Day, [],
        [
            Event("project-click", "X|repo"),
            Event("project-click", "X|repo"),
            Event("project-click", "Y|home"),
            Event("contact-submit", null),
        ]);

        Assert.Equal(3, result.Events.Count);
        Assert.Equal(2, Assert.Single(result.Events, e => e.Target == "X|repo").Count);
        Assert.Equal(1, Assert.Single(result.Events, e => e.Name == "contact-submit").Count);
    }

    [Theory]
    [InlineData("2026-08-13T00:00:00Z", "2026-08-13T00:20:00Z")] // before today's run
    [InlineData("2026-08-13T00:19:59Z", "2026-08-13T00:20:00Z")]
    [InlineData("2026-08-13T00:20:00Z", "2026-08-14T00:20:00Z")] // exactly at it → tomorrow
    [InlineData("2026-08-13T15:00:00Z", "2026-08-14T00:20:00Z")]
    public void NextRunUtc_ReturnsNextTwentyPastMidnight(string now, string expected)
        => Assert.Equal(
            DateTimeOffset.Parse(expected),
            AnalyticsRollup.NextRunUtc(DateTimeOffset.Parse(now)));
}
