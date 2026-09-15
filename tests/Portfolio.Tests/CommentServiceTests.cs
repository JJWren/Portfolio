using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Data;
using Portfolio.Web.Endpoints;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Database-free coverage of <c>AddAsync</c>'s rate-limit wiring (FR-D12), in
/// the <c>AnalyticsServiceTests</c> style: the context factory throws the
/// moment it is asked for a context, so a validation failure that never
/// reaches the database is proven by a clean return, and reaching the
/// database is proven by the sentinel exception.
///
/// Unlike <c>AnalyticsServiceTests</c>'s admin short-circuit, there is no
/// database-free way here to prove "a limited key is refused before any
/// database access" or "an admin never touches the database": the design
/// (nfr-design-patterns.md section 5) puts the limiter check after the ban
/// check, and the ban check itself needs a database round trip before the
/// limiter or the admin exemption is ever consulted. So every call with a
/// valid body reaches the ban check's database access first, regardless of
/// the caller's role or limiter state — this fixture pins exactly that.
/// <see cref="CommentLimiter"/>'s allow/deny/retry-after behaviour and
/// <see cref="SubmissionRules"/>'s exemption and key shape are covered
/// directly, without a database, by <c>SubmissionLimiterTests</c> and
/// <c>SubmissionRulesTests</c>.
/// </summary>
public class CommentServiceTests
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

    private static (CommentService Service, ThrowingDbFactory Factory) Build()
    {
        var factory = new ThrowingDbFactory();
        var service = new CommentService(factory, new CommentLimiter(new FakeTimeProvider()));
        return (service, factory);
    }

    private static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());

    private static ClaimsPrincipal AdminUser()
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, AuthEndpoints.AdminRole)], "test"));

    [Fact]
    public async Task AddAsync_EmptyBody_NeverTouchesTheDatabase()
    {
        var (service, factory) = Build();

        var (comment, error) = await service.AddAsync(1, "user-1", "   ", AnonymousUser(), "203.0.113.9");

        Assert.Null(comment);
        Assert.NotNull(error);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task AddAsync_TooLongBody_NeverTouchesTheDatabase()
    {
        var (service, factory) = Build();

        var (comment, error) = await service.AddAsync(
            1, "user-1", new string('x', CommentRules.MaxLength + 1), AnonymousUser(), "203.0.113.9");

        Assert.Null(comment);
        Assert.NotNull(error);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task AddAsync_ValidBody_OrdinaryCaller_ReachesTheDatabaseForTheBanCheck()
    {
        var (service, factory) = Build();

        await Assert.ThrowsAsync<SentinelException>(
            () => service.AddAsync(1, "user-1", "Nice post!", AnonymousUser(), "203.0.113.9"));

        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task AddAsync_ValidBody_ExemptAdmin_StillReachesTheDatabaseForTheBanCheck()
    {
        // SubmissionRules.IsExempt only skips the limiter, which sits after
        // the ban check — so an admin reaches the database exactly like
        // anyone else here. This pins that the exemption was not
        // (mis-)implemented as a bypass of AddAsync itself.
        var (service, factory) = Build();

        await Assert.ThrowsAsync<SentinelException>(
            () => service.AddAsync(1, "admin-1", "Nice post!", AdminUser(), "203.0.113.9"));

        Assert.Equal(1, factory.Calls);
    }
}
