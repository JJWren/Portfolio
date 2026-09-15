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
    private static string ProgramCs()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Program.cs");
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/Link item in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

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

    [Fact]
    public void SecurityOptions_RegisteredAsASingletonRightAfterSiteConfig()
    {
        var program = ProgramCs();

        var siteConfigIndex = program.IndexOf("AddSingleton(SiteConfig.FromConfiguration", StringComparison.Ordinal);
        var securityOptionsIndex = program.IndexOf("AddSingleton(SecurityOptions.FromConfiguration", StringComparison.Ordinal);

        Assert.True(siteConfigIndex >= 0, "Expected the SiteConfig singleton registration.");
        Assert.True(securityOptionsIndex >= 0, "Expected the SecurityOptions singleton registration.");
        Assert.True(securityOptionsIndex > siteConfigIndex, "Expected SecurityOptions to be registered right after SiteConfig.");
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
}
