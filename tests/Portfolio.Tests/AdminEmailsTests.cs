using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class AdminEmailsTests
{
    [Theory]
    [InlineData("owner@example.com", "owner@example.com", true)]
    [InlineData("owner@example.com", "OWNER@Example.COM", true)]
    [InlineData("a@x.com, b@y.com", "b@y.com", true)]
    [InlineData(" a@x.com ,, b@y.com ", "a@x.com", true)]
    [InlineData("owner@example.com", "visitor@example.com", false)]
    [InlineData("", "anyone@example.com", false)]
    [InlineData(null, "anyone@example.com", false)]
    public void IsAdmin_MatchesConfiguredEmails(string? configured, string candidate, bool expected)
    {
        Assert.Equal(expected, new AdminEmails(configured).IsAdmin(candidate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsAdmin_RejectsMissingCandidateEmail(string? candidate)
    {
        Assert.False(new AdminEmails("owner@example.com").IsAdmin(candidate));
    }
}
