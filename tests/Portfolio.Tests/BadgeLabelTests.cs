using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class BadgeLabelTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(10, "9+")]
    [InlineData(150, "9+")]
    public void Format_CapsCountAtNinePlus(int count, string expected)
        => Assert.Equal(expected, BadgeLabel.Format(count));
}
