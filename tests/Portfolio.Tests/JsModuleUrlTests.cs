using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class JsModuleUrlTests
{
    // Regression for #40: Blazor's interop import only rebases "./"-prefixed
    // URLs, so a bare fingerprinted asset path must gain the prefix or the
    // browser rejects it as a bare module specifier.
    [Theory]
    [InlineData("js/crop.js", "./js/crop.js")]
    [InlineData("js/crop.tf30qk4dnf.js", "./js/crop.tf30qk4dnf.js")]
    public void Resolve_PrefixesBareAssetPaths(string assetUrl, string expected)
    {
        Assert.Equal(expected, JsModuleUrl.Resolve(assetUrl));
    }

    [Theory]
    [InlineData("./js/crop.js")]
    [InlineData("/js/crop.js")]
    [InlineData("https://cdn.example.com/js/crop.js")]
    public void Resolve_LeavesUrlLikePathsUnchanged(string assetUrl)
    {
        Assert.Equal(assetUrl, JsModuleUrl.Resolve(assetUrl));
    }
}
