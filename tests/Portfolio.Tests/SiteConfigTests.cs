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
        Assert.Null(site.ContactPhone);
        Assert.Null(site.LinkedInUrl);
        Assert.Null(site.GitHubUrl);
        Assert.Null(site.About);
        Assert.Empty(site.Skills);
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
    public void FromConfiguration_BlankOptionalValuesBecomeNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["SITE_OWNER_NAME"] = "Jane",
            ["CONTACT_EMAIL"] = "jane@example.com",
            ["CONTACT_PHONE"] = "  ",
            ["LINKEDIN_URL"] = "",
        });

        var site = SiteConfig.FromConfiguration(config);

        Assert.Null(site.ContactPhone);
        Assert.Null(site.LinkedInUrl);
    }
}
