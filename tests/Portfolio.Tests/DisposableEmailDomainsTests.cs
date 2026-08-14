using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class DisposableEmailDomainsTests
{
    [Fact]
    public void Contains_MatchesExactDomain()
    {
        var domains = new DisposableEmailDomains(["mailinator.com"]);
        Assert.True(domains.Contains("mailinator.com"));
        Assert.False(domains.Contains("gmail.com"));
    }

    [Fact]
    public void Contains_MatchesParentSuffix()
    {
        var domains = new DisposableEmailDomains(["mailinator.com"]);
        Assert.True(domains.Contains("anything.mailinator.com"));
        Assert.True(domains.Contains("a.b.mailinator.com"));
    }

    [Fact]
    public void Contains_DoesNotMatchPartialLabels()
    {
        var domains = new DisposableEmailDomains(["mailinator.com"]);
        Assert.False(domains.Contains("notmailinator.com"));
    }

    [Fact]
    public void Contains_IsCaseInsensitive()
    {
        var domains = new DisposableEmailDomains(["MailiNator.Com"]);
        Assert.True(domains.Contains("MAILINATOR.COM"));
    }

    [Fact]
    public void Constructor_SkipsCommentsAndBlankLines()
    {
        var domains = new DisposableEmailDomains(["# a comment", "", "  ", "real.example"]);
        Assert.Equal(1, domains.Count);
        Assert.True(domains.Contains("real.example"));
    }

    [Fact]
    public void EmbeddedList_LoadsAndContainsKnownBurner()
    {
        var domains = new DisposableEmailDomains();
        Assert.True(domains.Count > 1000);
        Assert.True(domains.Contains("mailinator.com"));
    }
}
