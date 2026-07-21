using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ProfileRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_EmptyClearsTheCustomName(string? input)
    {
        var ok = ProfileRules.TryNormalizeDisplayName(input, out var normalized, out var error);

        Assert.True(ok);
        Assert.Null(normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Jo", "Jo")]
    [InlineData("  The Guy With The Dogs  ", "The Guy With The Dogs")]
    public void TryNormalize_TrimsAndAcceptsValidNames(string input, string expected)
    {
        var ok = ProfileRules.TryNormalizeDisplayName(input, out var normalized, out var error);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("J")]
    [InlineData("this-name-is-way-way-way-too-long-to-be-a-handle")]
    public void TryNormalize_RejectsBadLengths(string input)
    {
        var ok = ProfileRules.TryNormalizeDisplayName(input, out var normalized, out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.NotNull(error);
    }
}
