using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ReportRulesTests
{
    [Fact]
    public void Validate_AcceptsStandardReasonWithoutDetails()
    {
        Assert.True(ReportRules.Validate("Spam", null, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not a real reason")]
    public void Validate_RejectsUnknownReasons(string? reason)
    {
        Assert.False(ReportRules.Validate(reason, "details", out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_OtherRequiresDetails()
    {
        Assert.False(ReportRules.Validate(ReportRules.OtherReason, "  ", out var error));
        Assert.NotNull(error);
        Assert.True(ReportRules.Validate(ReportRules.OtherReason, "something specific", out _));
    }

    [Fact]
    public void Validate_RejectsOverlongDetails()
    {
        var details = new string('x', ReportRules.MaxDetailsLength + 1);
        Assert.False(ReportRules.Validate("Spam", details, out var error));
        Assert.Contains(ReportRules.MaxDetailsLength.ToString(), error);
    }

    [Fact]
    public void Excerpt_PassesShortTextThroughTrimmed()
    {
        Assert.Equal("hello", ReportRules.Excerpt("  hello  "));
    }

    [Fact]
    public void Excerpt_TruncatesWithEllipsisAtLimit()
    {
        var excerpt = ReportRules.Excerpt(new string('a', 500));

        Assert.NotNull(excerpt);
        Assert.Equal(ReportRules.MaxExcerptLength, excerpt!.Length);
        Assert.EndsWith("…", excerpt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Excerpt_EmptyBecomesNull(string? input)
    {
        Assert.Null(ReportRules.Excerpt(input));
    }
}
