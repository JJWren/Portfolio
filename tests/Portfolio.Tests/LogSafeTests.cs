using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class LogSafeTests
{
    [Theory]
    [InlineData("/blog/some-post", "/blog/some-post")]
    [InlineData("/a\r\nFAKE entry", "/a__FAKE entry")] // a forged second line becomes one line
    [InlineData("/a\nb", "/a_b")]
    [InlineData("a\rb", "a_b")] // a lone carriage return
    [InlineData("tab\there", "tab_here")]
    [InlineData("del\x7Fhere", "del_here")] // DEL (0x7F), the last C0-range control character
    [InlineData("a\x0085b", "a_b")] // NEL (U+0085), a C1 control character
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Sanitize_ReplacesControlCharacters(string? input, string expected)
        => Assert.Equal(expected, LogSafe.Sanitize(input));

    [Fact]
    public void Sanitize_CapsTheLength()
    {
        var longPath = "/" + new string('a', LogSafe.MaxLength + 50);

        Assert.Equal(LogSafe.MaxLength, LogSafe.Sanitize(longPath).Length);
        Assert.Equal("abc", LogSafe.Sanitize("abcdef", 3));
    }

    [Fact]
    public void Sanitize_DropsAHalfSurrogateLeftAtTheCap()
    {
        // The cap lands between the two halves of a supplementary character.
        var input = new string('a', LogSafe.MaxLength - 1) + char.ConvertFromUtf32(0x1F600);

        var result = LogSafe.Sanitize(input);

        Assert.Equal(LogSafe.MaxLength - 1, result.Length);
        Assert.False(char.IsSurrogate(result[^1]));
    }
}
