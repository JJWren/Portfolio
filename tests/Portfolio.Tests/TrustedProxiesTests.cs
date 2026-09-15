using System.Net;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class TrustedProxiesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Blank_ReturnsThreeEmptyLists(string? value)
    {
        var result = TrustedProxies.Parse(value);

        Assert.Empty(result.Proxies);
        Assert.Empty(result.Networks);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Parse_SingleAddress_GoesToProxies()
    {
        var result = TrustedProxies.Parse("203.0.113.9");

        var proxy = Assert.Single(result.Proxies);
        Assert.Equal(IPAddress.Parse("203.0.113.9"), proxy);
        Assert.Empty(result.Networks);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Parse_SingleCidr_GoesToNetworks()
    {
        var result = TrustedProxies.Parse("192.0.2.0/24");

        var network = Assert.Single(result.Networks);
        Assert.Equal(IPNetwork.Parse("192.0.2.0/24"), network);
        Assert.Empty(result.Proxies);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Parse_Junk_IsSkipped()
    {
        var result = TrustedProxies.Parse("not-an-address");

        var skipped = Assert.Single(result.Skipped);
        Assert.Equal("not-an-address", skipped);
        Assert.Empty(result.Proxies);
        Assert.Empty(result.Networks);
    }

    [Fact]
    public void Parse_SeveralMixedWithWhitespace_ClassifiesEachToken()
    {
        var result = TrustedProxies.Parse(" 203.0.113.9 , 192.0.2.0/24 ,junk, 198.51.100.0/24 ");

        Assert.Equal([IPAddress.Parse("203.0.113.9")], result.Proxies);
        Assert.Equal([IPNetwork.Parse("192.0.2.0/24"), IPNetwork.Parse("198.51.100.0/24")], result.Networks);
        Assert.Equal(["junk"], result.Skipped);
    }

    [Fact]
    public void Parse_BlankEntriesBetweenCommas_AreDropped()
    {
        var result = TrustedProxies.Parse("203.0.113.9,,  ,192.0.2.0/24");

        Assert.Single(result.Proxies);
        Assert.Single(result.Networks);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Parse_MultipleAddressesAndJunk_PreservesOrderWithinEachList()
    {
        var result = TrustedProxies.Parse("203.0.113.9,198.51.100.5,bogus1,192.0.2.9,bogus2");

        Assert.Equal(
            [IPAddress.Parse("203.0.113.9"), IPAddress.Parse("198.51.100.5"), IPAddress.Parse("192.0.2.9")],
            result.Proxies);
        Assert.Equal(["bogus1", "bogus2"], result.Skipped);
    }
}
