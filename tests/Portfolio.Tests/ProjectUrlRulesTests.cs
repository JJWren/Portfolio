using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ProjectUrlRulesTests
{
    [Theory]
    [InlineData("https://example.com/project", "https://example.com/project")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("  https://example.com  ", "https://example.com")]
    public void TryNormalize_TrimsAndAcceptsHttpUrls(string input, string expected)
    {
        var ok = ProjectUrlRules.TryNormalize(input, "Homepage URL", out var normalized, out var error);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_AcceptsEmptyAsNull(string? input)
    {
        var ok = ProjectUrlRules.TryNormalize(input, "Homepage URL", out var normalized, out var error);

        Assert.True(ok);
        Assert.Null(normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("example.com/no-scheme")]
    [InlineData("/relative/path")]
    public void TryNormalize_RejectsNonHttpUrls(string input)
    {
        var ok = ProjectUrlRules.TryNormalize(input, "Repo URL", out var normalized, out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.Contains("Repo URL", error);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,hi", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsHttp_GuardsRendering(string? url, bool expected)
        => Assert.Equal(expected, ProjectUrlRules.IsHttp(url));
}
