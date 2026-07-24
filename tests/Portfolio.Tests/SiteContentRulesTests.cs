using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SiteContentRulesTests
{
    private static SiteConfig BuildConfig(
        string? about = "First paragraph.\nSecond paragraph.",
        IReadOnlyList<string>? skills = null)
        => new(
            OwnerName: "Jane Developer",
            SiteTitle: "Jane Developer — Portfolio",
            Tagline: "Building useful things.",
            ContactEmail: "jane@example.com",
            ContactPhone: null,
            LinkedInUrl: null,
            GitHubUrl: null,
            About: about,
            Skills: skills ?? ["C#", "Docker"],
            SponsorUrl: null,
            SponsorText: "Buy me a coffee");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeField_WhitespaceOnly_ReturnsNull(string? value)
        => Assert.Null(SiteContentRules.NormalizeField(value));

    [Fact]
    public void NormalizeField_TrimsAndNormalizesLineEndings()
        => Assert.Equal("a\nb\nc", SiteContentRules.NormalizeField("  a\r\nb\rc  "));

    [Fact]
    public void ParseSkills_SplitsLinesTrimsAndDropsBlanks()
        => Assert.Equal(["C#", "ASP.NET Core", "SQL"],
            SiteContentRules.ParseSkills("C#\r\n\n  ASP.NET Core  \nSQL\n"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\n  \r\n ")]
    public void ParseSkills_NoContent_ReturnsNull(string? text)
        => Assert.Null(SiteContentRules.ParseSkills(text));

    [Fact]
    public void SkillsText_RoundTripsThroughParse()
    {
        var skills = new List<string> { "C#", "ASP.NET Core", "Docker" };

        Assert.Equal(skills, SiteContentRules.ParseSkills(SiteContentRules.SkillsText(skills)));
    }

    [Fact]
    public void SkillsText_Null_ReturnsEmpty()
        => Assert.Equal(string.Empty, SiteContentRules.SkillsText(null));

    [Fact]
    public void CheckLengths_OverlongField_NamesTheField()
    {
        var error = SiteContentRules.CheckLengths(
            new string('x', SiteContentRules.HeroHeadingMaxLength + 1), null, null);

        Assert.NotNull(error);
        Assert.Contains("Hero heading", error);
    }

    [Fact]
    public void CheckLengths_AllWithinLimits_ReturnsNull()
        => Assert.Null(SiteContentRules.CheckLengths(
            new string('x', SiteContentRules.HeroHeadingMaxLength),
            new string('x', SiteContentRules.TaglineMaxLength),
            new string('x', SiteContentRules.AboutMaxLength)));

    [Fact]
    public void Resolve_NullOverrides_FallsBackToConfig()
    {
        var site = BuildConfig();

        var effective = SiteContentRules.Resolve(site, null);

        Assert.Equal(site.OwnerName, effective.HeroHeading);
        Assert.Equal(site.Tagline, effective.Tagline);
        Assert.Equal(site.About, effective.About);
        Assert.Equal(site.Skills, effective.Skills);
    }

    [Fact]
    public void Resolve_OverrideWinsPerField()
    {
        var site = BuildConfig();
        var overrides = new SiteContent
        {
            HeroHeading = "Jane, but cooler",
            Skills = ["Rust", "Go"],
        };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal("Jane, but cooler", effective.HeroHeading);
        Assert.Equal(["Rust", "Go"], effective.Skills);
        // Un-overridden fields keep the .env values.
        Assert.Equal(site.Tagline, effective.Tagline);
        Assert.Equal(site.About, effective.About);
    }

    [Fact]
    public void Resolve_EmptySkillsList_FallsBackToConfig()
    {
        var site = BuildConfig();
        var overrides = new SiteContent { Skills = [] };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal(site.Skills, effective.Skills);
    }
}
