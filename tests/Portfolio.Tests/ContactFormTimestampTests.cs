using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ContactFormTimestampTests
{
    private static ContactFormTimestamp Create(FakeTimeProvider time)
        => new(new EphemeralDataProtectionProvider(), time);

    [Fact]
    public void TryGetElapsed_RoundTripsElapsedTime()
    {
        var time = new FakeTimeProvider();
        var timestamps = Create(time);

        var token = timestamps.Issue();
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.True(timestamps.TryGetElapsed(token, out var elapsed));
        Assert.Equal(TimeSpan.FromSeconds(10), elapsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public void TryGetElapsed_RejectsMissingOrGarbageTokens(string? token)
    {
        var timestamps = Create(new FakeTimeProvider());
        Assert.False(timestamps.TryGetElapsed(token, out _));
    }

    [Fact]
    public void TryGetElapsed_RejectsTokenFromDifferentKeyRing()
    {
        // Simulates a DataProtection key reset between render and submit.
        var time = new FakeTimeProvider();
        var issued = Create(time).Issue();
        var other = Create(time);

        Assert.False(other.TryGetElapsed(issued, out _));
    }

    [Fact]
    public void TryGetElapsed_RejectsFutureDatedToken()
    {
        // Same key ring, but the issuing clock runs ahead of the reading clock.
        var keyRing = new EphemeralDataProtectionProvider();
        var aheadClock = new FakeTimeProvider();
        aheadClock.Advance(TimeSpan.FromMinutes(5));
        var token = new ContactFormTimestamp(keyRing, aheadClock).Issue();

        var reader = new ContactFormTimestamp(keyRing, new FakeTimeProvider());
        Assert.False(reader.TryGetElapsed(token, out _));
    }
}
