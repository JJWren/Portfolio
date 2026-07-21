using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Spaces   everywhere  ", "spaces-everywhere")]
    [InlineData("C# & .NET 10!", "c-net-10")]
    [InlineData("Áccénted Tîtle", "accented-title")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("MiXeD CaSe", "mixed-case")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("---", "")]
    public void Slugify_ProducesUrlSafeSlugs(string? input, string expected)
    {
        Assert.Equal(expected, SlugHelper.Slugify(input));
    }

    [Fact]
    public void Slugify_CapsLengthAt160()
    {
        var slug = SlugHelper.Slugify(new string('a', 200) + " tail");
        Assert.True(slug.Length <= 160);
    }
}
