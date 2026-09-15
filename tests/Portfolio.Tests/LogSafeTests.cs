using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class LogSafeTests
{
    [Theory]
    [InlineData("/blog/some-post", "/blog/some-post")]
    [InlineData("/a\r\nFAKE entry", "/a__FAKE entry")] // a forged second line becomes one line
    [InlineData("/a\nb", "/a_b")]
    [InlineData("tab\there", "tab_here")]
    [InlineData("delhere", "del_here")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Value_ReplacesControlCharacters(string? input, string expected)
        => Assert.Equal(expected, LogSafe.Value(input));

    [Fact]
    public void Value_CapsTheLength()
    {
        var longPath = "/" + new string('a', LogSafe.MaxLength + 50);

        Assert.Equal(LogSafe.MaxLength, LogSafe.Value(longPath).Length);
        Assert.Equal("abc", LogSafe.Value("abcdef", 3));
    }
}
