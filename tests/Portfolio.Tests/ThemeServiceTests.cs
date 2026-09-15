using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins <see cref="ThemeService.LastKnown"/> (Unit 12a Copilot review
/// remediation): set on a successful <c>GetSnapshotAsync</c> load, and left
/// untouched by a later database blip, so <c>SecurityHeadersMiddleware</c>'s
/// <c>LastKnown ?? GetSnapshotAsync()</c> preference never regresses to null
/// once the app has served one snapshot. The project has no test host and no
/// in-memory EF provider (tech-stack-decisions.md), so a "successful load"
/// is simulated by overriding the <c>virtual GetOverridesAsync</c> seam
/// instead of a real database — <c>GetSnapshotAsync</c> itself, its version
/// guard and the <c>LastKnown</c> assignment all still run as real
/// production code. The database-blip half instead uses the same throwing
/// <c>IDbContextFactory</c> idiom as <c>AnalyticsServiceTests</c>.
/// </summary>
public class ThemeServiceTests
{
    private sealed class ThrowingDbFactory : IDbContextFactory<AppDbContext>
    {
        public int Calls { get; private set; }

        public AppDbContext CreateDbContext()
        {
            Calls++;
            throw new InvalidOperationException("No database in this test.");
        }
    }

    /// <summary>Bypasses the database entirely: GetOverridesAsync returns a canned row without ever touching the (unused) factory.</summary>
    private sealed class StubThemeService(ThemeSettings? row) : ThemeService(new ThrowingDbFactory())
    {
        public override Task<ThemeSettings?> GetOverridesAsync() => Task.FromResult(row);
    }

    /// <summary>Seeds the private LastKnown property directly — the same kind of reflection seam SecurityHeadersMiddlewareTests uses for ThemeService's private cache field.</summary>
    private static void SetLastKnown(ThemeService service, ThemeSnapshot snapshot)
        => typeof(ThemeService)
            .GetProperty(nameof(ThemeService.LastKnown))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(service, [snapshot]);

    [Fact]
    public async Task GetSnapshotAsync_SuccessfulLoad_SetsLastKnownToTheSameSnapshotItReturns()
    {
        var row = new ThemeSettings
        {
            Id = ThemeSettings.SingletonId,
            Overrides = new Dictionary<string, string> { ["dark-bg"] = "#0a5c36" },
        };
        var service = new StubThemeService(row);

        var snapshot = await service.GetSnapshotAsync();

        Assert.NotNull(snapshot.OverrideCssHash);
        Assert.Same(snapshot, service.LastKnown);
    }

    [Fact]
    public async Task GetSnapshotAsync_DatabaseBlip_ReturnsDefaultSnapshotButLeavesLastKnownAtItsPreviousValue()
    {
        var priorSnapshot = ThemeRules.DefaultSnapshot with { OverrideCssHash = "sha256-prior==" };
        var factory = new ThrowingDbFactory();
        var service = new ThemeService(factory);
        SetLastKnown(service, priorSnapshot);

        var result = await service.GetSnapshotAsync();

        Assert.Equal(1, factory.Calls);
        Assert.Same(ThemeRules.DefaultSnapshot, result);
        Assert.Same(priorSnapshot, service.LastKnown);
    }
}
