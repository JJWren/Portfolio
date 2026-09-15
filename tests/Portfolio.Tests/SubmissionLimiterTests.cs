using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Covers the generalized limiter directly (allow, deny, window roll-over,
/// key isolation, retry-after) so <see cref="ContactRateLimiter"/>,
/// <see cref="CommentLimiter"/> and <see cref="ReportLimiter"/> don't each
/// need their own copy of the same algorithm test — those three add only
/// their own numbers, pinned by <c>ContactRateLimiterTests</c> and the
/// derived-type facts below.
/// </summary>
public class SubmissionLimiterTests
{
    private static SubmissionLimiter NewLimiter(FakeTimeProvider time, int maxPerWindow = 3, int windowMinutes = 10)
        => new(time, maxPerWindow, TimeSpan.FromMinutes(windowMinutes));

    // ---- Allow ------------------------------------------------------------

    [Fact]
    public void Allow_PermitsUpToMaxPerWindow()
    {
        var limiter = NewLimiter(new FakeTimeProvider());

        for (var i = 0; i < 3; i++)
        {
            Assert.True(limiter.Allow("1.2.3.4"));
        }

        Assert.False(limiter.Allow("1.2.3.4"));
    }

    [Fact]
    public void Allow_TracksKeysIndependently()
    {
        var limiter = NewLimiter(new FakeTimeProvider());

        for (var i = 0; i < 3; i++)
        {
            limiter.Allow("a");
        }

        Assert.False(limiter.Allow("a"));
        Assert.True(limiter.Allow("b"));
    }

    [Fact]
    public void Allow_ResetsAfterWindowPasses()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time);

        for (var i = 0; i < 3; i++)
        {
            limiter.Allow("x");
        }

        Assert.False(limiter.Allow("x"));

        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        Assert.True(limiter.Allow("x"));
    }

    [Fact]
    public void Constructor_ExposesMaxPerWindowAndWindow()
    {
        var limiter = new SubmissionLimiter(new FakeTimeProvider(), 5, TimeSpan.FromMinutes(7));

        Assert.Equal(5, limiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(7), limiter.Window);
    }

    // ---- RetryAfter -------------------------------------------------------

    [Fact]
    public void RetryAfter_UnknownKey_IsZero()
        => Assert.Equal(TimeSpan.Zero, NewLimiter(new FakeTimeProvider()).RetryAfter("never-seen"));

    [Fact]
    public void RetryAfter_BelowTheLimit_IsZero()
    {
        var limiter = NewLimiter(new FakeTimeProvider());

        limiter.Allow("k");

        Assert.Equal(TimeSpan.Zero, limiter.RetryAfter("k"));
    }

    [Fact]
    public void RetryAfter_AtTheLimit_IsTheTimeUntilTheOldestHitAgesOut()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time, maxPerWindow: 2, windowMinutes: 10);

        limiter.Allow("k"); // t = 0
        time.Advance(TimeSpan.FromMinutes(2));
        limiter.Allow("k"); // t = 2

        // The oldest hit (t=0) ages out at t=10 — 8 minutes from now (t=2).
        Assert.Equal(TimeSpan.FromMinutes(8), limiter.RetryAfter("k"));
    }

    [Fact]
    public void RetryAfter_NeverGoesNegativeOnceTheWindowHasFullyElapsed()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);

        limiter.Allow("k");
        time.Advance(TimeSpan.FromMinutes(20));

        Assert.Equal(TimeSpan.Zero, limiter.RetryAfter("k"));
    }

    [Fact]
    public void RetryAfter_TracksKeysIndependently()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);

        limiter.Allow("a");

        Assert.True(limiter.RetryAfter("a") > TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, limiter.RetryAfter("b"));
    }

    // ---- The three derived limiters keep their own pinned numbers --------

    [Fact]
    public void ContactRateLimiter_KeepsItsOriginalNumbers()
    {
        Assert.Equal(3, ContactRateLimiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(10), ContactRateLimiter.Window);
    }

    [Fact]
    public void CommentLimiter_UsesFiveOverTenMinutes()
    {
        Assert.Equal(5, CommentLimiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(10), CommentLimiter.Window);

        var limiter = new CommentLimiter(new FakeTimeProvider());
        for (var i = 0; i < 5; i++)
        {
            Assert.True(limiter.Allow("k"));
        }

        Assert.False(limiter.Allow("k"));
    }

    [Fact]
    public void ReportLimiter_UsesThreeOverTenMinutes()
    {
        Assert.Equal(3, ReportLimiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(10), ReportLimiter.Window);

        var limiter = new ReportLimiter(new FakeTimeProvider());
        for (var i = 0; i < 3; i++)
        {
            Assert.True(limiter.Allow("k"));
        }

        Assert.False(limiter.Allow("k"));
    }
}
