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
}
