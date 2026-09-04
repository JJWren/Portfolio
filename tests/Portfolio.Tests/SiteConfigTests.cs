using Microsoft.Extensions.Configuration;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SiteConfigTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void FromConfiguration_MissingRequiredKeys_ThrowsListingAllOfThem()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var ex = Assert.Throws<InvalidOperationException>(() => SiteConfig.FromConfiguration(config));

        Assert.Contains("SITE_OWNER_NAME", ex.Message);
        Assert.Contains("CONTACT_EMAIL", ex.Message);
    }

    [Fact]
    public void FromConfiguration_MinimalConfig_AppliesDefaults()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane Developer",
            ["CONTACT_EMAIL"] = "jane@example.com",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal("Jane Developer", site.OwnerName);
        Assert.Equal("Jane Developer — Portfolio", site.SiteTitle);
        Assert.Equal(string.Empty, site.Tagline);
        Assert.Null(site.MetaDescription);
        Assert.Null(site.ContactPhone);
        Assert.Null(site.LinkedInUrl);
        Assert.Null(site.GitHubUrl);
        Assert.Null(site.About);
        Assert.Empty(site.Skills);
        Assert.Null(site.SponsorUrl);
        Assert.Equal("Buy me a coffee", site.SponsorText);
        Assert.Null(site.ResumeFile);
        Assert.Null(site.OwnerPhotoFile);
        Assert.Null(site.OwnerPhotoAlt);
    }

    [Fact]
    public void FromConfiguration_SponsorLinkIsConfigurable()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SPONSOR_URL"] = "https://ko-fi.com/jane",
            ["SPONSOR_TEXT"] = "Support my work",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal("https://ko-fi.com/jane", site.SponsorUrl);
        Assert.Equal("Support my work", site.SponsorText);
    }

    [Fact]
    public void FromConfiguration_SkillsAreSplitAndTrimmed()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_SKILLS"] = " C#, ASP.NET Core ,,Docker ",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(new[] { "C#", "ASP.NET Core", "Docker" }, site.Skills);
    }

    [Fact]
    public void FromConfiguration_MetaDescription_IsOptionalAndBlankBecomesNull()
    {
        var withValue = SiteConfig.FromConfiguration(BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_META_DESCRIPTION"] = "Software engineer building useful things.",
        }));
        var blank = SiteConfig.FromConfiguration(BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_META_DESCRIPTION"] = "   ",
        }));

        Assert.Equal("Software engineer building useful things.", withValue.MetaDescription);
        Assert.Null(blank.MetaDescription);
    }

    [Fact]
    public void FromConfiguration_BlankOptionalValuesBecomeNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["CONTACT_PHONE"] = "  ",
            ["LINKEDIN_URL"] = "",
            ["OWNER_PHOTO_FILE"] = "  ",
            ["OWNER_PHOTO_ALT"] = "",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Null(site.ContactPhone);
        Assert.Null(site.LinkedInUrl);
        Assert.Null(site.OwnerPhotoFile);
        Assert.Null(site.OwnerPhotoAlt);
    }

    [Fact]
    public void FromConfiguration_OwnerPhotoIsConfigurable()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["OWNER_PHOTO_FILE"] = "/app/photo/owner-photo.webp",
            ["OWNER_PHOTO_ALT"] = "Jane at her desk",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal("/app/photo/owner-photo.webp", site.OwnerPhotoFile);
        Assert.Equal("Jane at her desk", site.OwnerPhotoAlt);
    }

    [Fact]
    public void FromConfiguration_MinimalConfig_BjjFieldsDefaultToNullOrEmpty()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane Developer",
            ["CONTACT_EMAIL"] = "jane@example.com",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(SiteFlavor.Default, site.Flavor);
        Assert.Null(site.HeroEyebrow);
        Assert.Empty(site.GamePlanLines!);
        Assert.Null(site.BeltCaption);
        Assert.Null(site.BeltDegrees);
        Assert.Empty(site.PrincipleLines!);
    }

    [Theory]
    [InlineData("bjj", SiteFlavor.Bjj)]
    [InlineData("BJJ", SiteFlavor.Bjj)]
    [InlineData(" bjj ", SiteFlavor.Bjj)]
    [InlineData("", SiteFlavor.Default)]
    [InlineData(null, SiteFlavor.Default)]
    [InlineData("foo", SiteFlavor.Default)]
    public void FromConfiguration_FlavorParsing(string? value, SiteFlavor expected)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_FLAVOR"] = value,
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(expected, site.Flavor);
    }

    [Theory]
    [InlineData("bjj", SiteFlavor.Bjj)]
    [InlineData(" BJJ ", SiteFlavor.Bjj)]
    [InlineData("", SiteFlavor.Default)]
    [InlineData("foo", SiteFlavor.Default)]
    public void SiteFlavorRules_Parse(string value, SiteFlavor expected)
        => Assert.Equal(expected, SiteFlavorRules.Parse(value));

    [Fact]
    public void FromConfiguration_HeroEyebrowAndBeltCaption_BlankBecomesNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_HERO_EYEBROW"] = "  ",
            ["SITE_BELT_CAPTION"] = "",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Null(site.HeroEyebrow);
        Assert.Null(site.BeltCaption);
    }

    [Fact]
    public void FromConfiguration_HeroEyebrowAndBeltCaption_AreConfigurable()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_HERO_EYEBROW"] = "Jane Developer · Software Engineer",
            ["SITE_BELT_CAPTION"] = "Test belt · Test gym",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal("Jane Developer · Software Engineer", site.HeroEyebrow);
        Assert.Equal("Test belt · Test gym", site.BeltCaption);
    }

    [Fact]
    public void FromConfiguration_GamePlanAndPrinciples_SplitOnLiteralNewlineTrimmedAndBlanksDropped()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_GAME_PLAN"] = "Plan | Plan it \\n\\n  Build | Build it  \\nTest | Test it\\nShip | Ship it",
            ["SITE_PRINCIPLES"] = "Ship small. | Small is safe\\n\\nWrite it down. | ",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(
            ["Plan | Plan it", "Build | Build it", "Test | Test it", "Ship | Ship it"],
            site.GamePlanLines);
        Assert.Equal(["Ship small. | Small is safe", "Write it down. |"], site.PrincipleLines);
    }

    [Fact]
    public void FromConfiguration_GamePlanAndPrinciples_UnsetBecomesEmpty()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Empty(site.GamePlanLines!);
        Assert.Empty(site.PrincipleLines!);
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("0", 0)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("x", null)]
    [InlineData("2.5", null)]
    public void FromConfiguration_BeltDegreesParsing(string? value, int? expected)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_BELT_DEGREES"] = value,
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(expected, site.BeltDegrees);
    }

    [Fact]
    public void FromConfiguration_BeltDegreesOutOfBjjRange_StillParsesUnclamped()
    {
        // SiteConfig itself never clamps — BjjRules.ClampDegrees does that at
        // resolve, so an out-of-range env value is still a parsed int here.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_BELT_DEGREES"] = "9",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(9, site.BeltDegrees);
    }

    [Fact]
    public void FromConfiguration_MinimalConfig_EraAndNowLinesDefaultToEmpty()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane Developer",
            ["CONTACT_EMAIL"] = "jane@example.com",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Empty(site.EraLines!);
        Assert.Empty(site.NowLines!);
    }

    [Fact]
    public void FromConfiguration_EraAndNowLines_SplitOnLiteralNewlineTrimmedAndBlanksDropped()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["SITE_ERAS"] = "2010-01-01 | white | 2 | Gym A | City A | Role A\\n\\n  2012-06-15 | blue | 3 | Gym B | City B | Role B  \\n",
            ["SITE_NOW"] = "Training | Evening classes.\\n\\nReading | ",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Equal(
            ["2010-01-01 | white | 2 | Gym A | City A | Role A", "2012-06-15 | blue | 3 | Gym B | City B | Role B"],
            site.EraLines);
        Assert.Equal(["Training | Evening classes.", "Reading |"], site.NowLines);
    }
}
