using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ContactSpamRulesTests
{
    private static readonly DisposableEmailDomains Disposable =
        new(["mailinator.com", "sharklasers.com"]);

    [Theory]
    [InlineData("No links at all.", 0)]
    [InlineData("See https://example.com for details.", 1)]
    [InlineData("http://a.example and https://b.example", 2)]
    [InlineData("Check [this](www.spam.example) out.", 1)]
    [InlineData("[a](https://x.example) plus [b](www.y.example)", 2)]
    public void CountLinks_CountsBareAndMarkdownLinks(string body, int expected)
        => Assert.Equal(expected, ContactSpamRules.CountLinks(body));

    [Theory]
    [InlineData("Just a normal subject", false)]
    [InlineData("Visit https://spam.example now", true)]
    [InlineData("Deals at www.spam.example", true)]
    public void SubjectHasUrl_DetectsUrls(string subject, bool expected)
        => Assert.Equal(expected, ContactSpamRules.SubjectHasUrl(subject));

    [Theory]
    [InlineData("a@b.com", "b.com")]
    [InlineData("weird@sub.domain.example", "sub.domain.example")]
    [InlineData("no-at-sign", null)]
    [InlineData("trailing@", null)]
    public void DomainOf_ExtractsDomain(string email, string? expected)
        => Assert.Equal(expected, ContactSpamRules.DomainOf(email));

    [Fact]
    public void SoftFlagReason_CleanMessage_ReturnsNull()
        => Assert.Null(ContactSpamRules.SoftFlagReason(
            "real@gmail.com", "Hello", "I liked your blog post.", Disposable));

    [Fact]
    public void SoftFlagReason_DisposableDomain_Flags()
        => Assert.Equal("disposable-domain", ContactSpamRules.SoftFlagReason(
            "x@mailinator.com", "Hello", "Nice site.", Disposable));

    [Fact]
    public void SoftFlagReason_TooManyBodyLinks_Flags()
        => Assert.Equal("body-links", ContactSpamRules.SoftFlagReason(
            "real@gmail.com", "Hello", "https://a.example https://b.example", Disposable));

    [Fact]
    public void SoftFlagReason_SingleBodyLink_DoesNotFlag()
        => Assert.Null(ContactSpamRules.SoftFlagReason(
            "real@gmail.com", "Hello", "My repo: https://github.com/me/x", Disposable));

    [Fact]
    public void SoftFlagReason_CombinesMultipleReasons()
        => Assert.Equal("disposable-domain,subject-url", ContactSpamRules.SoftFlagReason(
            "x@sharklasers.com", "See www.spam.example", "Hi.", Disposable));

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("a", null, "a")]
    [InlineData(null, "b", "b")]
    [InlineData("a", "b", "a,b")]
    public void CombineReasons_MergesNullables(string? first, string? second, string? expected)
        => Assert.Equal(expected, ContactSpamRules.CombineReasons(first, second));
}
