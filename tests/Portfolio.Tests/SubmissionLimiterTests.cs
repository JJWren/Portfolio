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

    // ---- Expiry boundary: a hit exactly Window old is expired ------------

    /// <summary>
    /// A hit ages out the instant it turns exactly <c>Window</c> old (the
    /// <c>&gt;=</c> comparison in <c>Allow</c>/<c>RetryAfter</c>/<c>Sweep</c>),
    /// not one tick later. Two independent limiters sharing one clock so
    /// <see cref="SubmissionLimiter.Allow"/>'s own bookkeeping (adding a
    /// fresh hit) can't change what <see cref="SubmissionLimiter.RetryAfter"/>
    /// sees for the original one.
    /// </summary>
    [Fact]
    public void Boundary_ExactlyAtTheWindow_AllowSucceedsAndRetryAfterIsZero()
    {
        var time = new FakeTimeProvider();
        var allowLimiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);
        allowLimiter.Allow("k"); // t = 0

        var retryLimiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);
        retryLimiter.Allow("k"); // t = 0

        time.Advance(TimeSpan.FromMinutes(10)); // now = last hit + Window, exactly

        Assert.True(allowLimiter.Allow("k"));
        Assert.Equal(TimeSpan.Zero, retryLimiter.RetryAfter("k"));
    }

    /// <summary>One tick short of the window, the same hit still counts against the limit.</summary>
    [Fact]
    public void Boundary_OneTickBeforeTheWindow_AllowIsDeniedAndRetryAfterIsThatTick()
    {
        var time = new FakeTimeProvider();
        var allowLimiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);
        allowLimiter.Allow("k"); // t = 0

        var retryLimiter = NewLimiter(time, maxPerWindow: 1, windowMinutes: 10);
        retryLimiter.Allow("k"); // t = 0

        time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));

        Assert.False(allowLimiter.Allow("k"));
        Assert.Equal(TimeSpan.FromTicks(1), retryLimiter.RetryAfter("k"));
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
        var limiter = new CommentLimiter(new FakeTimeProvider());

        Assert.Equal(5, limiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(10), limiter.Window);

        for (var i = 0; i < 5; i++)
        {
            Assert.True(limiter.Allow("k"));
        }

        Assert.False(limiter.Allow("k"));
    }

    [Fact]
    public void ReportLimiter_UsesThreeOverTenMinutes()
    {
        var limiter = new ReportLimiter(new FakeTimeProvider());

        Assert.Equal(3, limiter.MaxPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(10), limiter.Window);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(limiter.Allow("k"));
        }

        Assert.False(limiter.Allow("k"));
    }

    // ---- Sweep / TrackedKeys (bounded memory) ------------------------------

    [Fact]
    public void Sweep_RemovesKeysAgedOutOfTheWindow_KeepsLiveOnes()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time);

        limiter.Allow("stale");
        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        limiter.Allow("live");

        limiter.Sweep();

        Assert.Equal(1, limiter.TrackedKeys);
    }

    [Fact]
    public void Sweep_RunsAutomaticallyEvery256thAllowCall()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time);

        limiter.Allow("stale"); // call 1 of the 256-call cadence

        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));

        // Calls 2 through 256: the 256th call's automatic Sweep() prunes
        // "stale" (aged out and now empty) while "fresh" (repopulated by
        // this very loop) stays, since Sweep only drops empty lists.
        for (var i = 0; i < 255; i++)
        {
            limiter.Allow("fresh");
        }

        Assert.Equal(1, limiter.TrackedKeys);
    }

    [Fact]
    public void RetryAfter_OnAnAgedOutKey_RemovesIt()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time);

        limiter.Allow("k");
        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.Zero, limiter.RetryAfter("k"));
        Assert.Equal(0, limiter.TrackedKeys);
    }

    /// <summary>
    /// Guards the outcome the Sweep/Allow lock-and-verify protocol exists
    /// for (see the class remarks on <see cref="SubmissionLimiter"/>): once
    /// a key's list has been swept away for being empty, reusing the same
    /// key must start a clean count — up to the limit, then denied — with
    /// exactly one tracked entry, never a leftover or a split across two
    /// lists that together let more than <c>MaxPerWindow</c> through.
    /// </summary>
    [Fact]
    public void Allow_KeyRemovedBySweepThenReused_CountsCorrectlyAndTracksOneEntry()
    {
        var time = new FakeTimeProvider();
        var limiter = NewLimiter(time, maxPerWindow: 3, windowMinutes: 10);

        limiter.Allow("k");
        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        limiter.Sweep();

        Assert.Equal(0, limiter.TrackedKeys);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(limiter.Allow("k"));
        }

        Assert.False(limiter.Allow("k"));
        Assert.Equal(1, limiter.TrackedKeys);
    }
}
