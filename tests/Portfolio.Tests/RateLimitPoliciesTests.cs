using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Pins the policy names and numbers, the pure retry-after/body helpers
/// (callable without a live limiter), and exercises <c>Configure</c> against
/// a fresh <c>RateLimiterOptions</c>: <c>RateLimiterOptions.AddPolicy</c>
/// stores each named policy as an opaque delegate with no public way to
/// invoke it in isolation, so the three named policies' numbers are pinned
/// through the shared <see cref="RateLimitPolicies.WindowOptions"/> helper
/// they're built from, while the wiring itself is exercised end to end
/// through <c>GlobalLimiter</c> (a real, directly usable limiter) and
/// <c>OnRejected</c> fed a real rejected lease obtained by exhausting it —
/// the same numbers the <c>Auth</c> policy uses. The three named policies'
/// actual runtime behaviour is what the local 429 walk-through against
/// <c>/feed.xml</c> (security-verification-instructions.md) covers instead.
/// </summary>
public class RateLimitPoliciesTests
{
    // ---- Names, numbers, window -------------------------------------------

    [Fact]
    public void PolicyNamesAndNumbers_MatchTheRequirementsTable()
    {
        Assert.Equal("auth", RateLimitPolicies.Auth);
        Assert.Equal("feeds", RateLimitPolicies.Feeds);
        Assert.Equal("redirects", RateLimitPolicies.Redirects);
        Assert.Equal(10, RateLimitPolicies.AuthPermitLimit);
        Assert.Equal(30, RateLimitPolicies.FeedsPermitLimit);
        Assert.Equal(30, RateLimitPolicies.RedirectsPermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), RateLimitPolicies.Window);
        Assert.Equal("/signin-", RateLimitPolicies.SignInPathPrefix);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    public void WindowOptions_UsesTheGivenLimitAndTheSharedWindowNoQueueAutoReplenish(int permitLimit)
    {
        var options = RateLimitPolicies.WindowOptions(permitLimit);

        Assert.Equal(permitLimit, options.PermitLimit);
        Assert.Equal(RateLimitPolicies.Window, options.Window);
        Assert.Equal(0, options.QueueLimit);
        Assert.True(options.AutoReplenishment);
    }

    // ---- ClientKey ----------------------------------------------------------

    [Fact]
    public void ClientKey_PlainAddress_ReturnsItsString()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

        Assert.Equal("203.0.113.9", RateLimitPolicies.ClientKey(context));
    }

    [Fact]
    public void ClientKey_IPv4MappedAddress_MapsToIPv4()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.9");

        Assert.Equal("203.0.113.9", RateLimitPolicies.ClientKey(context));
    }

    [Fact]
    public void ClientKey_NoRemoteAddress_ReturnsUnknown()
        => Assert.Equal("unknown", RateLimitPolicies.ClientKey(new DefaultHttpContext()));

    // ---- IsSignInHandlerPath --------------------------------------------

    [Theory]
    [InlineData("/signin-github", true)]
    [InlineData("/signin-google", true)]
    [InlineData("/signin-discord", true)]
    [InlineData("/signin", false)]
    [InlineData("/auth/login/GitHub", false)]
    [InlineData("/", false)]
    [InlineData("/feed.xml", false)]
    public void IsSignInHandlerPath_MatchesOnlyTheSignInPrefix(string path, bool expected)
        => Assert.Equal(expected, RateLimitPolicies.IsSignInHandlerPath(new PathString(path)));

    // ---- RetryAfterSeconds --------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0.4, 1)]
    [InlineData(1, 1)]
    [InlineData(1.2, 2)]
    [InlineData(59.9, 60)]
    [InlineData(-5, 1)]
    public void RetryAfterSeconds_RoundsUpToWholeSecondsAtLeastOne(double seconds, int expected)
        => Assert.Equal(expected, RateLimitPolicies.RetryAfterSeconds(TimeSpan.FromSeconds(seconds)));

    // ---- RejectionBody ------------------------------------------------------

    [Fact]
    public void RejectionBody_IsTheExactSentenceWithATrailingNewline()
        => Assert.Equal("Too many requests. Try again in 42 seconds.\n", RateLimitPolicies.RejectionBody(42));

    // ---- Configure ------------------------------------------------------

    [Fact]
    public void Configure_SetsThe429RejectionStatusCode()
    {
        var options = new RateLimiterOptions();

        RateLimitPolicies.Configure(options);

        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
    }

    private static DefaultHttpContext ContextFor(string path, string address)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        return context;
    }

    [Fact]
    public void Configure_GlobalLimiter_AppliesTheAuthNumbersToASignInPath()
    {
        var options = new RateLimiterOptions();
        RateLimitPolicies.Configure(options);
        var limiter = options.GlobalLimiter;
        Assert.NotNull(limiter);

        for (var i = 0; i < RateLimitPolicies.AuthPermitLimit; i++)
        {
            using var lease = limiter!.AttemptAcquire(ContextFor("/signin-github", "198.51.100.7"));
            Assert.True(lease.IsAcquired);
        }

        using var rejected = limiter!.AttemptAcquire(ContextFor("/signin-github", "198.51.100.7"));
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void Configure_GlobalLimiter_KeysTheSignInPartitionByAddress()
    {
        var options = new RateLimiterOptions();
        RateLimitPolicies.Configure(options);
        var limiter = options.GlobalLimiter!;

        for (var i = 0; i < RateLimitPolicies.AuthPermitLimit; i++)
        {
            limiter.AttemptAcquire(ContextFor("/signin-google", "198.51.100.7")).Dispose();
        }

        // A different address gets its own bucket even for the same path.
        using var otherAddress = limiter.AttemptAcquire(ContextFor("/signin-google", "198.51.100.8"));
        Assert.True(otherAddress.IsAcquired);
    }

    [Fact]
    public void Configure_GlobalLimiter_NeverLimitsNonSignInPaths()
    {
        var options = new RateLimiterOptions();
        RateLimitPolicies.Configure(options);
        var limiter = options.GlobalLimiter!;

        for (var i = 0; i < RateLimitPolicies.AuthPermitLimit + 5; i++)
        {
            using var lease = limiter.AttemptAcquire(ContextFor("/feed.xml", "198.51.100.7"));
            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public async Task Configure_OnRejected_WritesRetryAfterHeaderContentTypeAndBody()
    {
        var options = new RateLimiterOptions();
        RateLimitPolicies.Configure(options);
        var limiter = options.GlobalLimiter!;
        Assert.NotNull(options.OnRejected);

        for (var i = 0; i < RateLimitPolicies.AuthPermitLimit; i++)
        {
            limiter.AttemptAcquire(ContextFor("/signin-discord", "203.0.113.5")).Dispose();
        }

        using var rejectedLease = limiter.AttemptAcquire(ContextFor("/signin-discord", "203.0.113.5"));
        Assert.False(rejectedLease.IsAcquired);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var rejectedContext = new OnRejectedContext { HttpContext = httpContext, Lease = rejectedLease };

        await options.OnRejected!(rejectedContext, CancellationToken.None);

        Assert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
        Assert.True(int.TryParse(httpContext.Response.Headers["Retry-After"], out var seconds));
        Assert.True(seconds >= 1);

        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Equal(RateLimitPolicies.RejectionBody(seconds), body);
    }
}
