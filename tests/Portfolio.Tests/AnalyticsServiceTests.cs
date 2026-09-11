using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Data;
using Portfolio.Web.Endpoints;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins the shared event entry point's exclusions without a database. The
/// context factory throws the moment it is asked for a context, so an
/// excluded caller is proven never to reach the secret lookup or the insert,
/// while an ordinary visitor is proven to get that far (and stops at the
/// sentinel). Removing the admin check from TryRecordEventAsync fails the
/// first test even though AnalyticsRules.IsExcludedUser still passes its own.
/// </summary>
public class AnalyticsServiceTests
{
    private sealed class SentinelException : Exception
    {
    }

    private sealed class ThrowingDbFactory : IDbContextFactory<AppDbContext>
    {
        public int Calls { get; private set; }

        public AppDbContext CreateDbContext()
        {
            Calls++;
            throw new SentinelException();
        }
    }

    private static (AnalyticsService Service, ThrowingDbFactory Factory) Build()
    {
        var factory = new ThrowingDbFactory();
        var service = new AnalyticsService(
            factory, new FakeTimeProvider(), NullLogger<AnalyticsService>.Instance);
        return (service, factory);
    }

    /// <summary>A browser-like request that passes the bot and opt-out checks,
    /// so only the principal decides whether the event is recorded.</summary>
    private static DefaultHttpContext VisitorContext(ClaimsPrincipal user)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:130.0) Gecko/20100101 Firefox/130.0";
        context.User = user;
        return context;
    }

    private static ClaimsPrincipal PrincipalWithRole(string role)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

    [Fact]
    public async Task TryRecordEventAsync_AdminSession_NeverTouchesTheDatabase()
    {
        var (service, factory) = Build();
        var context = VisitorContext(PrincipalWithRole(AuthEndpoints.AdminRole));

        await service.TryRecordEventAsync(context, AnalyticsRules.ResumeDownloadEvent, "footer");

        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task TryRecordEventAsync_OrdinaryVisitor_ReachesTheDatabase()
    {
        var (service, factory) = Build();
        var context = VisitorContext(new ClaimsPrincipal(new ClaimsIdentity()));

        await Assert.ThrowsAsync<SentinelException>(
            () => service.TryRecordEventAsync(context, AnalyticsRules.ResumeDownloadEvent, "footer"));

        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task TryRecordEventAsync_SignedInNonAdmin_ReachesTheDatabase()
    {
        var (service, factory) = Build();
        var context = VisitorContext(PrincipalWithRole("User"));

        await Assert.ThrowsAsync<SentinelException>(
            () => service.TryRecordEventAsync(context, AnalyticsRules.ProjectClickEvent, "Sample|home"));

        Assert.Equal(1, factory.Calls);
    }
}
