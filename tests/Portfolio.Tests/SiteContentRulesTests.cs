using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SiteContentRulesTests
{
    private static SiteConfig BuildConfig(
        string? about = "First paragraph.\nSecond paragraph.",
        IReadOnlyList<string>? skills = null,
        string? ownerPhotoAlt = null,
        string? heroEyebrow = null,
        IReadOnlyList<string>? gamePlanLines = null,
        string? beltCaption = null,
        int? beltDegrees = null,
        IReadOnlyList<string>? principleLines = null)
        => new(
            OwnerName: "Jane Developer",
            SiteTitle: "Jane Developer — Portfolio",
            Tagline: "Building useful things.",
            MetaDescription: null,
            ContactEmail: "jane@example.com",
            ContactPhone: null,
            LinkedInUrl: null,
            GitHubUrl: null,
            About: about,
            Skills: skills ?? ["C#", "Docker"],
            SponsorUrl: null,
            SponsorText: "Buy me a coffee",
            OwnerPhotoAlt: ownerPhotoAlt,
            HeroEyebrow: heroEyebrow,
            GamePlanLines: gamePlanLines,
            BeltCaption: beltCaption,
            BeltDegrees: beltDegrees,
            PrincipleLines: principleLines);

    private static SiteContentDraft EmptyDraft()
        => new(
            HeroHeading: null,
            Tagline: null,
            About: null,
            SkillsText: null,
            OwnerPhotoAlt: null,
            HeroEyebrow: null,
            GamePlanText: null,
            BeltCaption: null,
            BeltDegreesText: null,
            PrinciplesText: null);

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
            new string('x', SiteContentRules.AboutMaxLength),
            new string('x', SiteContentRules.OwnerPhotoAltMaxLength)));

    [Fact]
    public void CheckLengths_OverlongPhotoAlt_NamesTheField()
    {
        var error = SiteContentRules.CheckLengths(
            null, null, null, new string('x', SiteContentRules.OwnerPhotoAltMaxLength + 1));

        Assert.NotNull(error);
        Assert.Contains("Photo alt text", error);
    }

    [Fact]
    public void Resolve_NullOverrides_FallsBackToConfig()
    {
        var site = BuildConfig();

        var effective = SiteContentRules.Resolve(site, null);

        Assert.Equal(site.OwnerName, effective.HeroHeading);
        Assert.Equal(site.Tagline, effective.Tagline);
        Assert.Equal(site.About, effective.About);
        Assert.Equal(site.Skills, effective.Skills);
        // No override and no env value: the alt text derives from the owner.
        Assert.Equal("Portrait of Jane Developer", effective.OwnerPhotoAlt);
    }

    [Fact]
    public void Resolve_OwnerPhotoAlt_EnvValueBeatsTheDerivedDefault()
    {
        var site = BuildConfig(ownerPhotoAlt: "Jane at her desk");

        var effective = SiteContentRules.Resolve(site, null);

        Assert.Equal("Jane at her desk", effective.OwnerPhotoAlt);
    }

    [Fact]
    public void Resolve_OwnerPhotoAlt_OverrideBeatsEnv()
    {
        var site = BuildConfig(ownerPhotoAlt: "Jane at her desk");
        var overrides = new SiteContent { OwnerPhotoAlt = "Jane on stage" };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal("Jane on stage", effective.OwnerPhotoAlt);
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

    // -- BJJ landing flavor: Resolve precedence (BR-3) -------------------

    [Fact]
    public void Resolve_NullOverridesAndNoEnv_BjjFieldsAreEmptyOrNull()
    {
        var site = BuildConfig();

        var effective = SiteContentRules.Resolve(site, null);

        Assert.Null(effective.HeroEyebrow);
        Assert.Empty(effective.GamePlan);
        Assert.Null(effective.BeltCaption);
        Assert.Equal(0, effective.BeltDegrees);
        Assert.Empty(effective.Principles);
    }

    [Fact]
    public void Resolve_NullOverrides_BjjFieldsFallBackToEnv()
    {
        var site = BuildConfig(
            heroEyebrow: "Jane · Engineer",
            gamePlanLines:
            [
                "Guard | Secure the position",
                "Pass | Improve the position",
                "Mount | Keep control",
                "Submit | Finish",
            ],
            beltCaption: "Test belt · Test gym",
            beltDegrees: 3,
            principleLines: ["Ship small. | Small is safe."]);

        var effective = SiteContentRules.Resolve(site, null);

        Assert.Equal("Jane · Engineer", effective.HeroEyebrow);
        Assert.Equal(4, effective.GamePlan.Count);
        Assert.Equal("Guard", effective.GamePlan[0].Term);
        Assert.Equal("Test belt · Test gym", effective.BeltCaption);
        Assert.Equal(3, effective.BeltDegrees);
        Assert.Single(effective.Principles);
    }

    [Fact]
    public void Resolve_HeroEyebrowOverride_WinsOverEnv()
    {
        var site = BuildConfig(heroEyebrow: "Env eyebrow");
        var overrides = new SiteContent { HeroEyebrow = "Override eyebrow" };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal("Override eyebrow", effective.HeroEyebrow);
    }

    [Fact]
    public void Resolve_BeltCaptionOverride_WinsOverEnv()
    {
        var site = BuildConfig(beltCaption: "Env caption");
        var overrides = new SiteContent { BeltCaption = "Override caption" };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal("Override caption", effective.BeltCaption);
    }

    [Fact]
    public void Resolve_BeltDegreesOverride_WinsOverEnv()
    {
        var site = BuildConfig(beltDegrees: 2);
        var overrides = new SiteContent { BeltDegrees = 5 };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal(5, effective.BeltDegrees);
    }

    [Fact]
    public void Resolve_BeltDegrees_ClampsOutOfRangeStoredValue()
    {
        var site = BuildConfig();
        var overrides = new SiteContent { BeltDegrees = 42 };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal(6, effective.BeltDegrees);
    }

    [Fact]
    public void Resolve_GamePlanOverride_WinsOverEnv()
    {
        var site = BuildConfig(gamePlanLines: ["Env1 | a", "Env2 | b", "Env3 | c", "Env4 | d"]);
        var overrides = new SiteContent
        {
            GamePlan =
            [
                "Guard | Secure the position",
                "Pass | Improve the position",
                "Mount | Keep control",
                "Submit | Finish",
            ],
        };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal("Guard", effective.GamePlan[0].Term);
    }

    [Fact]
    public void Resolve_GamePlanEmptyOverride_FallsBackToEnv()
    {
        var site = BuildConfig(gamePlanLines:
        [
            "Guard | Secure the position",
            "Pass | Improve the position",
            "Mount | Keep control",
            "Submit | Finish",
        ]);
        var overrides = new SiteContent { GamePlan = [] };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Equal(4, effective.GamePlan.Count);
        Assert.Equal("Guard", effective.GamePlan[0].Term);
    }

    [Fact]
    public void Resolve_PrinciplesOverride_WinsOverEnv()
    {
        var site = BuildConfig(principleLines: ["Env maxim | env reading"]);
        var overrides = new SiteContent { Principles = ["Override maxim | override reading"] };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Single(effective.Principles);
        Assert.Equal("Override maxim", effective.Principles[0].Maxim);
    }

    [Fact]
    public void Resolve_PrinciplesEmptyOverride_FallsBackToEnv()
    {
        var site = BuildConfig(principleLines: ["Env maxim | env reading"]);
        var overrides = new SiteContent { Principles = [] };

        var effective = SiteContentRules.Resolve(site, overrides);

        Assert.Single(effective.Principles);
        Assert.Equal("Env maxim", effective.Principles[0].Maxim);
    }

    // -- BJJ landing flavor: Validate (BR-4 "strict at save") -----------

    [Fact]
    public void Validate_AllValid_ReturnsNull()
    {
        var draft = EmptyDraft() with
        {
            HeroHeading = "Heading",
            Tagline = "Tag",
            About = "About",
            SkillsText = "C#",
            OwnerPhotoAlt = "Alt",
            HeroEyebrow = "Eyebrow",
            GamePlanText = "Guard | Secure the position\nPass | Improve the position\nMount | Keep control\nSubmit | Finish",
            BeltCaption = "Caption",
            BeltDegreesText = "3",
            PrinciplesText = "Ship small. | reading",
        };

        Assert.Null(SiteContentRules.Validate(draft));
    }

    [Fact]
    public void Validate_EmptyDraft_ReturnsNull()
        => Assert.Null(SiteContentRules.Validate(EmptyDraft()));

    [Fact]
    public void Validate_OverlongHeroEyebrow_NamesTheField()
    {
        var draft = EmptyDraft() with { HeroEyebrow = new string('x', SiteContentRules.HeroEyebrowMaxLength + 1) };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("Hero eyebrow", error);
    }

    [Fact]
    public void Validate_OverlongBeltCaption_NamesTheField()
    {
        var draft = EmptyDraft() with { BeltCaption = new string('x', SiteContentRules.BeltCaptionMaxLength + 1) };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("Belt caption", error);
    }

    [Fact]
    public void Validate_BadGamePlanCount_ReturnsError()
    {
        var draft = EmptyDraft() with { GamePlanText = "Guard | Secure the position" };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("exactly 4", error);
    }

    [Fact]
    public void Validate_TooManyPrinciples_ReturnsError()
    {
        var lines = string.Join('\n', Enumerable.Range(1, 7).Select(i => $"Line {i} | reading"));
        var draft = EmptyDraft() with { PrinciplesText = lines };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("up to 6", error);
    }

    [Fact]
    public void Validate_NonNumericBeltDegrees_ReturnsFriendlyError()
    {
        var draft = EmptyDraft() with { BeltDegreesText = "abc" };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("whole number", error);
    }

    [Fact]
    public void Validate_OutOfRangeBeltDegrees_ReturnsError()
    {
        var draft = EmptyDraft() with { BeltDegreesText = "9" };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("0 and 6", error);
    }

    [Fact]
    public void Validate_ChecksLengthsBeforeBjjFields()
    {
        // Both an overlong hero heading (an existing-field problem) and a
        // bad game-plan count are present; CheckLengths must win — pins the
        // "first problem" order (CheckLengths, then the BJJ format checks).
        var draft = EmptyDraft() with
        {
            HeroHeading = new string('x', SiteContentRules.HeroHeadingMaxLength + 1),
            GamePlanText = "Guard | Secure the position",
        };

        var error = SiteContentRules.Validate(draft);

        Assert.NotNull(error);
        Assert.Contains("Hero heading", error);
    }

    // -- LinesText / ParseLines round trip -------------------------------

    [Fact]
    public void ParseLines_SplitsLinesTrimsAndDropsBlanks()
        => Assert.Equal(["a | b", "c | d"], SiteContentRules.ParseLines("a | b\r\n\n  c | d  \n"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\n  \r\n ")]
    public void ParseLines_NoContent_ReturnsNull(string? text)
        => Assert.Null(SiteContentRules.ParseLines(text));

    [Fact]
    public void LinesText_RoundTripsThroughParseLines()
    {
        var lines = new List<string> { "Guard | Secure the position", "Pass | Improve the position" };

        Assert.Equal(lines, SiteContentRules.ParseLines(SiteContentRules.LinesText(lines)));
    }

    [Fact]
    public void LinesText_Null_ReturnsEmpty()
        => Assert.Equal(string.Empty, SiteContentRules.LinesText(null));

    [Fact]
    public void ParseDegrees_ParsesValidIntegers()
        => Assert.Equal(3, SiteContentRules.ParseDegrees("3"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void ParseDegrees_BlankOrUnparsable_ReturnsNull(string? text)
        => Assert.Null(SiteContentRules.ParseDegrees(text));
}
