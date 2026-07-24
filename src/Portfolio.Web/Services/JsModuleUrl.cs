namespace Portfolio.Web.Services;

/// <summary>
/// Builds URLs for Blazor's JS-interop <c>import</c> identifier.
/// </summary>
public static class JsModuleUrl
{
    /// <summary>
    /// Makes an asset path importable as an ES module. Blazor's interop
    /// <c>import</c> only rebases <c>./</c>-prefixed URLs onto the document
    /// base href; a bare fingerprinted path such as <c>js/crop.{hash}.js</c>
    /// (what <c>Assets[...]</c> returns) reaches native <c>import()</c>
    /// unchanged, where the browser rejects it as a bare module specifier
    /// before any request is made (#40).
    /// </summary>
    /// <param name="assetUrl">The asset path as resolved by <c>Assets[...]</c>.</param>
    /// <returns>A URL that dynamic <c>import()</c> accepts.</returns>
    public static string Resolve(string assetUrl)
    {
        if (assetUrl.StartsWith("./", StringComparison.Ordinal)
            || assetUrl.StartsWith("/", StringComparison.Ordinal)
            || assetUrl.Contains("://", StringComparison.Ordinal))
        {
            return assetUrl;
        }

        return "./" + assetUrl;
    }
}
