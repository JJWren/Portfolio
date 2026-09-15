using System.Security.Claims;
using Portfolio.Web.Endpoints;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SubmissionRulesTests
{
    // ---- IsExempt -----------------------------------------------------------

    [Fact]
    public void IsExempt_AdminRole_ReturnsTrue()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, AuthEndpoints.AdminRole)], "test");

        Assert.True(SubmissionRules.IsExempt(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void IsExempt_PlainUserRole_ReturnsFalse()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "User")], "test");

        Assert.False(SubmissionRules.IsExempt(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void IsExempt_Anonymous_ReturnsFalse()
        => Assert.False(SubmissionRules.IsExempt(new ClaimsPrincipal(new ClaimsIdentity())));

    // ---- Key ----------------------------------------------------------------

    [Fact]
    public void Key_SignedIn_UsesTheUserIdPrefix()
        => Assert.Equal("user:abc123", SubmissionRules.Key("abc123", "203.0.113.9"));

    [Fact]
    public void Key_SignedIn_IgnoresTheAddressEvenWhenPresent()
        => Assert.Equal("user:abc123", SubmissionRules.Key("abc123", null));

    [Fact]
    public void Key_AnonymousWithAddress_UsesTheIpPrefix()
        => Assert.Equal("ip:203.0.113.9", SubmissionRules.Key(null, "203.0.113.9"));

    [Theory]
    [InlineData(null, "203.0.113.9")]
    [InlineData("", "203.0.113.9")]
    [InlineData("   ", "203.0.113.9")]
    public void Key_BlankOrMissingUserId_FallsBackToTheAddress(string? userId, string address)
        => Assert.Equal("ip:203.0.113.9", SubmissionRules.Key(userId, address));

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData(null, "   ")]
    public void Key_NeitherUserIdNorAddress_FallsBackToIpUnknown(string? userId, string? address)
        => Assert.Equal("ip:unknown", SubmissionRules.Key(userId, address));

    // ---- WaitMessage ----------------------------------------------------------

    [Fact]
    public void WaitMessage_RoundsUpToTheNextWholeMinute()
        => Assert.Equal(
            "You've posted a few comments in a row. Try again in about 2 minutes.",
            SubmissionRules.WaitMessage(TimeSpan.FromSeconds(61), "comments"));

    [Fact]
    public void WaitMessage_ExactWholeMinutesNeedsNoRounding()
        => Assert.Equal(
            "You've posted a few comments in a row. Try again in about 3 minutes.",
            SubmissionRules.WaitMessage(TimeSpan.FromMinutes(3), "comments"));

    [Fact]
    public void WaitMessage_OneMinuteIsSingular()
        => Assert.Equal(
            "You've posted a few reports in a row. Try again in about 1 minute.",
            SubmissionRules.WaitMessage(TimeSpan.FromSeconds(30), "reports"));

    [Fact]
    public void WaitMessage_NeverGoesBelowOneMinuteEvenForZero()
        => Assert.Equal(
            "You've posted a few comments in a row. Try again in about 1 minute.",
            SubmissionRules.WaitMessage(TimeSpan.Zero, "comments"));

    [Fact]
    public void WaitMessage_UsesTheGivenNounForBothCommentsAndReports()
    {
        Assert.Contains("few comments in a row", SubmissionRules.WaitMessage(TimeSpan.FromMinutes(1), "comments"));
        Assert.Contains("few reports in a row", SubmissionRules.WaitMessage(TimeSpan.FromMinutes(1), "reports"));
    }
}
