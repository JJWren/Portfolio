using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Data;
using Portfolio.Web.Endpoints;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Database-free coverage of <c>CreateAsync</c>'s rate-limit wiring
/// (FR-D12), mirroring <c>CommentServiceTests</c>: the ban check (a database
/// round trip) runs before the limiter and the admin exemption, so the
/// database-free proof available here is that invalid input never reaches
/// the database and valid input reaches exactly the ban check, for both an
/// ordinary caller and an admin. <see cref="ReportLimiter"/>'s
/// allow/deny/retry-after behaviour and <see cref="SubmissionRules"/>'s
/// exemption and key shape are covered directly by
/// <c>SubmissionLimiterTests</c> and <c>SubmissionRulesTests</c>.
/// </summary>
public class ReportServiceTests
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

    private static (ReportService Service, ThrowingDbFactory Factory) Build()
    {
        var factory = new ThrowingDbFactory();
        var service = new ReportService(factory, new MessageService(factory), new ReportLimiter(new FakeTimeProvider()));
        return (service, factory);
    }

    private static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());

    private static ClaimsPrincipal AdminUser()
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, AuthEndpoints.AdminRole)], "test"));

    [Fact]
    public async Task CreateAsync_NoReason_NeverTouchesTheDatabase()
    {
        var (service, factory) = Build();

        var (report, error) = await service.CreateAsync(
            "user-1", 1, ReportTargetType.Comment, null, null, AnonymousUser(), "203.0.113.9");

        Assert.Null(report);
        Assert.NotNull(error);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_OrdinaryCaller_ReachesTheDatabaseForTheBanCheck()
    {
        var (service, factory) = Build();

        await Assert.ThrowsAsync<SentinelException>(
            () => service.CreateAsync(
                "user-1", 1, ReportTargetType.Comment, ReportRules.Reasons[0], null, AnonymousUser(), "203.0.113.9"));

        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ExemptAdmin_StillReachesTheDatabaseForTheBanCheck()
    {
        // Same reasoning as CommentServiceTests: the admin exemption only
        // skips the limiter, which sits after the ban check, so an admin
        // still reaches the database here.
        var (service, factory) = Build();

        await Assert.ThrowsAsync<SentinelException>(
            () => service.CreateAsync(
                "admin-1", 1, ReportTargetType.Comment, ReportRules.Reasons[0], null, AdminUser(), "203.0.113.9"));

        Assert.Equal(1, factory.Calls);
    }
}
