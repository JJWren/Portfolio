using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ContactRateLimiterTests
{
    [Fact]
    public void Allow_PermitsUpToMaxPerWindow()
    {
        var limiter = new ContactRateLimiter(new FakeTimeProvider());

        for (var i = 0; i < ContactRateLimiter.MaxPerWindow; i++)
        {
            Assert.True(limiter.Allow("1.2.3.4"));
        }

        Assert.False(limiter.Allow("1.2.3.4"));
    }

    [Fact]
    public void Allow_TracksKeysIndependently()
    {
        var limiter = new ContactRateLimiter(new FakeTimeProvider());

        for (var i = 0; i < ContactRateLimiter.MaxPerWindow; i++)
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
        var limiter = new ContactRateLimiter(time);

        for (var i = 0; i < ContactRateLimiter.MaxPerWindow; i++)
        {
            limiter.Allow("x");
        }

        Assert.False(limiter.Allow("x"));

        time.Advance(ContactRateLimiter.Window + TimeSpan.FromSeconds(1));
        Assert.True(limiter.Allow("x"));
    }
}
