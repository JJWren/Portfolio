using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins the parts of Program.cs's pipeline order and host configuration
/// that Unit 12a's security depends on, by scanning the linked source copied
/// into the test output — the project deliberately has no test host
/// (tech-stack-decisions.md), so a text scan is the cheapest guard against a
/// silent reorder that would leave some response path unprotected.
/// </summary>
public class ProgramPipelineTests
{
    private static string ProgramCs() => LinkedSource.Read("Program.cs");

    [Fact]
    public void SecurityHeadersMiddleware_RunsRightAfterUseForwardedHeaders_AndBeforeUseRouting()
    {
        var program = ProgramCs();

        var forwardedIndex = program.IndexOf("app.UseForwardedHeaders();", StringComparison.Ordinal);
        var middlewareIndex = program.IndexOf("app.UseMiddleware<SecurityHeadersMiddleware>();", StringComparison.Ordinal);
        var routingIndex = program.IndexOf("app.UseRouting();", StringComparison.Ordinal);

        Assert.True(forwardedIndex >= 0, "Expected app.UseForwardedHeaders(); in Program.cs.");
        Assert.True(middlewareIndex >= 0, "Expected app.UseMiddleware<SecurityHeadersMiddleware>(); in Program.cs.");
        Assert.True(routingIndex >= 0, "Expected app.UseRouting(); in Program.cs.");
        Assert.True(middlewareIndex > forwardedIndex, "Expected SecurityHeadersMiddleware to run after UseForwardedHeaders.");
        Assert.True(routingIndex > middlewareIndex, "Expected SecurityHeadersMiddleware to run before UseRouting.");

        // Nothing else sits between UseForwardedHeaders and the middleware —
        // every response, including the HEAD-as-GET rewrite that follows,
        // passes through it.
        var between = program[(forwardedIndex + "app.UseForwardedHeaders();".Length)..middlewareIndex];
        Assert.DoesNotContain("app.", between, StringComparison.Ordinal);
    }

    [Fact]
    public void Kestrel_ServerHeaderIsDisabled()
        => Assert.Contains("options.AddServerHeader = false", ProgramCs(), StringComparison.Ordinal);

    /// <summary>
    /// OnStarting fires last-registered-first, so both framework defaults —
    /// antiforgery's unconditional X-Frame-Options: SAMEORIGIN and the
    /// interactive render mode's Content-Security-Policy: frame-ancestors
    /// 'self' — register after, and so run before, SecurityHeadersMiddleware's
    /// own OnStarting callback and win under fill-if-absent (commit 837a6fb).
    /// Losing either suppression line reinstates that weaker default; losing
    /// the second is worse than losing the first, because it leaves the
    /// framework's own Content-Security-Policy header already present under
    /// that exact name, so fill-if-absent skips this app's fuller policy
    /// entirely on every interactive response.
    /// </summary>
    [Fact]
    public void FrameworkAntiClickjackingDefaults_AreBothSuppressed()
    {
        var program = ProgramCs();
        Assert.Contains("SuppressXFrameOptionsHeader = true", program, StringComparison.Ordinal);
        Assert.Contains("ContentSecurityFrameAncestorsPolicy = null", program, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityOptions_RegisteredAsASingletonRightAfterSiteConfig()
    {
        var program = ProgramCs();

        const string siteConfigStatement = "builder.Services.AddSingleton(SiteConfig.FromConfiguration(builder.Configuration));";
        const string securityOptionsStatement = "builder.Services.AddSingleton(SecurityOptions.FromConfiguration(builder.Configuration));";

        var siteConfigIndex = program.IndexOf(siteConfigStatement, StringComparison.Ordinal);
        var securityOptionsIndex = program.IndexOf(securityOptionsStatement, StringComparison.Ordinal);

        Assert.True(siteConfigIndex >= 0, "Expected the SiteConfig singleton registration.");
        Assert.True(securityOptionsIndex >= 0, "Expected the SecurityOptions singleton registration.");
        Assert.True(securityOptionsIndex > siteConfigIndex, "Expected SecurityOptions to be registered right after SiteConfig.");

        // Nothing but whitespace and comment lines sits between the two
        // registrations — the same "between" check the sibling test above
        // uses for the middleware's placement.
        var between = program[(siteConfigIndex + siteConfigStatement.Length)..securityOptionsIndex];
        var betweenLines = between.Split('\n').Select(line => line.Trim());
        Assert.True(
            betweenLines.All(line => line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)),
            $"Expected only whitespace and comments between SiteConfig and SecurityOptions, found: \"{between}\".");
    }

    [Fact]
    public void UploadsStaticFiles_OnPrepareResponse_SetsTheUploadsPolicyAndNosniffNextToCacheControl()
    {
        var program = ProgramCs();

        var onPrepareIndex = program.IndexOf("OnPrepareResponse = static ctx =>", StringComparison.Ordinal);
        Assert.True(onPrepareIndex >= 0, "Expected the /uploads OnPrepareResponse block in Program.cs.");

        var closeIndex = program.IndexOf("});", onPrepareIndex, StringComparison.Ordinal);
        Assert.True(closeIndex > onPrepareIndex, "Expected the OnPrepareResponse block to close.");
        var block = program[onPrepareIndex..closeIndex];

        Assert.Contains("CacheControl", block, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersRules.UploadsCsp", block, StringComparison.Ordinal);
        Assert.Contains(
            @"Response.Headers[""X-Content-Type-Options""] = ""nosniff""",
            block, StringComparison.Ordinal);
    }

    // ---- Unit 12b: rate limiting -------------------------------------------

    [Fact]
    public void UseRateLimiter_RunsRightAfterUseRouting_AndBeforeUseAuthentication()
    {
        var program = ProgramCs();

        var routingIndex = program.IndexOf("app.UseRouting();", StringComparison.Ordinal);
        var rateLimiterIndex = program.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal);
        var authenticationIndex = program.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);

        Assert.True(routingIndex >= 0, "Expected app.UseRouting(); in Program.cs.");
        Assert.True(rateLimiterIndex >= 0, "Expected app.UseRateLimiter(); in Program.cs.");
        Assert.True(authenticationIndex >= 0, "Expected app.UseAuthentication(); in Program.cs.");
        Assert.True(rateLimiterIndex > routingIndex, "Expected UseRateLimiter to run after UseRouting.");
        Assert.True(authenticationIndex > rateLimiterIndex, "Expected UseRateLimiter to run before UseAuthentication.");
    }

    [Fact]
    public void AddRateLimiter_IsRegisteredWithRateLimitPoliciesConfigure()
        => Assert.Contains(
            "builder.Services.AddRateLimiter(RateLimitPolicies.Configure);",
            ProgramCs(), StringComparison.Ordinal);

    /// <summary>
    /// Combined with the ordering fact above (UseRateLimiter before
    /// UseAuthentication), this pins the full chain a rate-limit rejection
    /// depends on never reaching: rejected before authentication, and
    /// AnalyticsMiddleware sits after authentication (FR-D10, NFR-15).
    /// </summary>
    [Fact]
    public void AnalyticsMiddleware_RunsAfterUseAuthentication()
    {
        var program = ProgramCs();

        var authenticationIndex = program.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var analyticsIndex = program.IndexOf("app.UseMiddleware<AnalyticsMiddleware>();", StringComparison.Ordinal);

        Assert.True(authenticationIndex >= 0, "Expected app.UseAuthentication(); in Program.cs.");
        Assert.True(analyticsIndex >= 0, "Expected app.UseMiddleware<AnalyticsMiddleware>(); in Program.cs.");
        Assert.True(analyticsIndex > authenticationIndex, "Expected AnalyticsMiddleware to run after UseAuthentication.");
    }

    [Fact]
    public void TrustedProxies_ParsedFromConfiguration_AndAppliedToForwardedHeadersOptions()
    {
        var program = ProgramCs();

        Assert.Contains(
            @"TrustedProxies.Parse(builder.Configuration[""TRUSTED_PROXIES""])",
            program, StringComparison.Ordinal);
        Assert.Contains("options.KnownProxies.Add(", program, StringComparison.Ordinal);
        Assert.Contains("options.KnownIPNetworks.Add(", program, StringComparison.Ordinal);
    }

    /// <summary>
    /// A configured-but-empty TRUSTED_PROXIES (every token junk) must throw
    /// before the host builds, rather than let the forwarded-headers
    /// middleware silently trust every peer with both lists empty — see
    /// <see cref="TrustedProxyList.ConfiguredButEmpty"/>. Pinned here (a
    /// text scan, like every other fact in this fixture) because the throw
    /// itself can't run through this text-only host-free project.
    /// </summary>
    [Fact]
    public void TrustedProxies_ConfiguredButEmpty_ThrowsBeforeTheHostBuilds()
    {
        var program = ProgramCs();

        var parseIndex = program.IndexOf(
            @"TrustedProxies.Parse(builder.Configuration[""TRUSTED_PROXIES""])",
            StringComparison.Ordinal);
        var checkIndex = program.IndexOf("trusted.ConfiguredButEmpty", StringComparison.Ordinal);
        var throwIndex = program.IndexOf("throw new InvalidOperationException", StringComparison.Ordinal);
        var buildIndex = program.IndexOf("builder.Build()", StringComparison.Ordinal);

        Assert.True(parseIndex >= 0, "Expected TrustedProxies.Parse in Program.cs.");
        Assert.True(checkIndex >= 0, "Expected a trusted.ConfiguredButEmpty check in Program.cs.");
        Assert.True(throwIndex >= 0, "Expected a throw new InvalidOperationException in Program.cs.");
        Assert.True(buildIndex >= 0, "Expected builder.Build() in Program.cs.");
        Assert.True(checkIndex > parseIndex, "Expected the ConfiguredButEmpty check after TrustedProxies.Parse.");
        Assert.True(throwIndex > checkIndex, "Expected the throw inside the ConfiguredButEmpty check.");
        Assert.True(throwIndex < buildIndex, "Expected the throw to run before builder.Build().");
    }
}
