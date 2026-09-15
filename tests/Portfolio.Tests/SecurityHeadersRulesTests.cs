using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins every security header and Content-Security-Policy directive this
/// application sends (FR-D1, FR-D2, FR-D4, FR-D5) value by value, so a
/// change to any of them is a deliberate edit of this file, not an accident.
/// </summary>
public class SecurityHeadersRulesTests
{
    // ---- ParseCspMode ---------------------------------------------------

    [Theory]
    [InlineData("report-only", CspMode.ReportOnly)]
    [InlineData("Report-Only", CspMode.ReportOnly)]
    [InlineData("REPORT-ONLY", CspMode.ReportOnly)]
    [InlineData(" report-only ", CspMode.ReportOnly)]
    [InlineData("off", CspMode.Off)]
    [InlineData("Off", CspMode.Off)]
    [InlineData("OFF", CspMode.Off)]
    [InlineData(" off ", CspMode.Off)]
    [InlineData("enforce", CspMode.Enforce)]
    [InlineData("Enforce", CspMode.Enforce)]
    [InlineData("ENFORCE", CspMode.Enforce)]
    [InlineData(null, CspMode.Enforce)]
    [InlineData("", CspMode.Enforce)]
    [InlineData("   ", CspMode.Enforce)]
    [InlineData("garbage", CspMode.Enforce)]
    [InlineData("report only", CspMode.Enforce)] // no hyphen: unrecognized, falls back
    [InlineData("offx", CspMode.Enforce)]
    public void ParseCspMode_MapsKnownValuesAndDefaultsEverythingElseToEnforce(string? value, CspMode expected)
        => Assert.Equal(expected, SecurityHeadersRules.ParseCspMode(value));

    // ---- PermissionsPolicy ------------------------------------------------

    [Fact]
    public void PermissionsPolicy_IsExactlyThePinnedValue()
        => Assert.Equal(
            "accelerometer=(), autoplay=(), browsing-topics=(), camera=(), clipboard-read=(), clipboard-write=(), display-capture=(), fullscreen=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), picture-in-picture=(), screen-wake-lock=(), usb=(), web-share=(), xr-spatial-tracking=()",
            SecurityHeadersRules.PermissionsPolicy);

    [Fact]
    public void PermissionsPolicy_DeniesTheFiveRequirementNamedFeatures()
    {
        // FR-D1: "at least camera, microphone, geolocation, payment and USB".
        Assert.Contains("camera=()", SecurityHeadersRules.PermissionsPolicy);
        Assert.Contains("microphone=()", SecurityHeadersRules.PermissionsPolicy);
        Assert.Contains("geolocation=()", SecurityHeadersRules.PermissionsPolicy);
        Assert.Contains("payment=()", SecurityHeadersRules.PermissionsPolicy);
        Assert.Contains("usb=()", SecurityHeadersRules.PermissionsPolicy);
    }

    // ---- UploadsCsp ---------------------------------------------------------

    [Fact]
    public void UploadsCsp_IsExactlyThePinnedValue()
        => Assert.Equal("default-src 'none'; style-src 'unsafe-inline'; sandbox", SecurityHeadersRules.UploadsCsp);

    // ---- BuildCsp -----------------------------------------------------------

    [Fact]
    public void BuildCsp_NoHash_IsExactlyThePinnedValueWithSelfOnlyStyleSrc()
        => Assert.Equal(
            "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self'; img-src 'self' blob: https:; font-src 'self'; connect-src 'self'; manifest-src 'self'",
            SecurityHeadersRules.BuildCsp(null));

    [Fact]
    public void BuildCsp_EmptyHash_IsTreatedLikeNoHash()
        => Assert.Equal(SecurityHeadersRules.BuildCsp(null), SecurityHeadersRules.BuildCsp(string.Empty));

    [Fact]
    public void BuildCsp_WithHash_IsExactlyThePinnedValueWithTheQuotedHashAppended()
        => Assert.Equal(
            "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self' 'sha256-abc123=='; img-src 'self' blob: https:; font-src 'self'; connect-src 'self'; manifest-src 'self'",
            SecurityHeadersRules.BuildCsp("sha256-abc123=="));

    [Fact]
    public void BuildCsp_NeverContainsUnsafeInlineUnsafeEvalOrANonce()
    {
        var csp = SecurityHeadersRules.BuildCsp("sha256-abc123==");

        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        Assert.DoesNotContain("nonce-", csp);
    }

    // ---- Compose --------------------------------------------------------

    [Fact]
    public void Compose_Enforce_ReturnsEveryHeaderInOrderEndingWithContentSecurityPolicy()
    {
        var headers = SecurityHeadersRules.Compose(CspMode.Enforce, null);

        string[] expectedNames =
        [
            "X-Content-Type-Options",
            "Referrer-Policy",
            "X-Frame-Options",
            "Permissions-Policy",
            "Cross-Origin-Opener-Policy",
            "X-DNS-Prefetch-Control",
            "X-Permitted-Cross-Domain-Policies",
            "Content-Security-Policy",
        ];
        Assert.Equal(expectedNames, headers.Select(h => h.Name).ToArray());

        var byName = headers.ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("nosniff", byName["X-Content-Type-Options"]);
        Assert.Equal("strict-origin-when-cross-origin", byName["Referrer-Policy"]);
        Assert.Equal("DENY", byName["X-Frame-Options"]);
        Assert.Equal(SecurityHeadersRules.PermissionsPolicy, byName["Permissions-Policy"]);
        Assert.Equal("same-origin", byName["Cross-Origin-Opener-Policy"]);
        Assert.Equal("off", byName["X-DNS-Prefetch-Control"]);
        Assert.Equal("none", byName["X-Permitted-Cross-Domain-Policies"]);
        Assert.Equal(SecurityHeadersRules.BuildCsp(null), byName["Content-Security-Policy"]);
    }

    [Fact]
    public void Compose_ReportOnly_SendsTheReportOnlyHeaderNameInsteadWithTheSameValue()
    {
        var headers = SecurityHeadersRules.Compose(CspMode.ReportOnly, "sha256-xyz==");

        Assert.DoesNotContain(headers, h => h.Name == "Content-Security-Policy");
        var entry = Assert.Single(headers, h => h.Name == "Content-Security-Policy-Report-Only");
        Assert.Equal(SecurityHeadersRules.BuildCsp("sha256-xyz=="), entry.Value);
    }

    [Fact]
    public void Compose_Off_SendsNoCspHeaderOfEitherNameButKeepsEveryOtherHeader()
    {
        var headers = SecurityHeadersRules.Compose(CspMode.Off, "sha256-xyz==");

        string[] expectedNames =
        [
            "X-Content-Type-Options",
            "Referrer-Policy",
            "X-Frame-Options",
            "Permissions-Policy",
            "Cross-Origin-Opener-Policy",
            "X-DNS-Prefetch-Control",
            "X-Permitted-Cross-Domain-Policies",
        ];
        Assert.Equal(expectedNames, headers.Select(h => h.Name).ToArray());
        Assert.DoesNotContain(headers, h => h.Name.StartsWith("Content-Security-Policy", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_HashOnlyAppearsInTheCspWhenGiven()
    {
        var withoutHash = SecurityHeadersRules.Compose(CspMode.Enforce, null);
        var withHash = SecurityHeadersRules.Compose(CspMode.Enforce, "sha256-abc==");

        Assert.DoesNotContain("sha256-", withoutHash.Single(h => h.Name == "Content-Security-Policy").Value);
        Assert.Contains("'sha256-abc=='", withHash.Single(h => h.Name == "Content-Security-Policy").Value);
    }
}
