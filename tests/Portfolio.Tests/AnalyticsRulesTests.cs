using Microsoft.AspNetCore.Http;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class AnalyticsRulesTests
{
    [Theory]
    [InlineData("/", true)]
    [InlineData("/projects", true)]
    [InlineData("/blog/some-post-slug", true)]
    [InlineData("/contact", true)]
    [InlineData("/admin", false)]
    [InlineData("/admin/messages", false)]
    [InlineData("/auth/login/GitHub", false)]
    [InlineData("/signin", false)]
    [InlineData("/go/1/repo", false)]
    [InlineData("/resume", false)]
    [InlineData("/uploads/abc.png", false)]
    [InlineData("/healthz", false)]
    [InlineData("/_blazor/negotiate", false)]
    [InlineData("/_framework/blazor.web.js", false)]
    [InlineData("/not-found", false)]
    [InlineData("/feed.xml", false)]
    [InlineData("/robots.txt", false)]
    [InlineData("/app.css", false)]
    public void IsCountablePath_CountsOnlyPublicPages(string path, bool expected)
        => Assert.Equal(expected, AnalyticsRules.IsCountablePath(new PathString(path)));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)", true)]
    [InlineData("curl/8.4.0", true)]
    [InlineData("python-requests/2.31", true)]
    [InlineData("UptimeMonitor/1.0", true)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0", false)]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) Safari/604.1", false)]
    public void IsBot_DetectsNonHumans(string? userAgent, bool expected)
        => Assert.Equal(expected, AnalyticsRules.IsBot(userAgent));

    [Fact]
    public void OptedOut_HonorsDntAndGpc()
    {
        Assert.True(AnalyticsRules.OptedOut(new HeaderDictionary { ["DNT"] = "1" }));
        Assert.True(AnalyticsRules.OptedOut(new HeaderDictionary { ["Sec-GPC"] = "1" }));
        Assert.False(AnalyticsRules.OptedOut(new HeaderDictionary()));
        Assert.False(AnalyticsRules.OptedOut(new HeaderDictionary { ["DNT"] = "0" }));
    }

    [Theory]
    [InlineData("https://news.ycombinator.com/item?id=1", "news.ycombinator.com")]
    [InlineData("https://WWW.Google.com/search", "www.google.com")]
    [InlineData("https://example.org/page", null)] // own host
    [InlineData("not a url", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void NormalizeReferrer_ReducesToExternalHost(string? referrer, string? expected)
        => Assert.Equal(expected, AnalyticsRules.NormalizeReferrer(referrer, new HostString("example.org")));

    [Fact]
    public void Truncate_CapsLongValues()
    {
        Assert.Equal("abc", AnalyticsRules.Truncate("abc", 5));
        Assert.Equal("abcde", AnalyticsRules.Truncate("abcdefgh", 5));
    }
}
