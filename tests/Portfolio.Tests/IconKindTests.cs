using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class IconKindTests
{
    [Theory]
    [InlineData("github")]
    [InlineData("google")]
    [InlineData("discord")]
    [InlineData("linkedin")]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("heart")]
    [InlineData("external")]
    public void Normalize_KnownKind_PassesThrough(string kind)
        => Assert.Equal(kind, IconKind.Normalize(kind));

    [Theory]
    [InlineData("GitHub", "github")]
    [InlineData("Google", "google")]
    [InlineData("Discord", "discord")]
    public void Normalize_OAuthSchemeCasing_Lowercases(string scheme, string expected)
        // The exact schemes registered in Program.cs, as SignIn.razor passes them.
        => Assert.Equal(expected, IconKind.Normalize(scheme));

    [Fact]
    public void Normalize_UnknownKind_FallsBackToExternal()
        => Assert.Equal(IconKind.Fallback, IconKind.Normalize("bitbucket"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhitespace_FallsBackToExternal(string? kind)
        => Assert.Equal(IconKind.Fallback, IconKind.Normalize(kind));

    [Fact]
    public void Normalize_TrimsPadding()
        => Assert.Equal("github", IconKind.Normalize(" github "));
}
