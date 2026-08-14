using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class VisitorKeyTests
{
    private static readonly byte[] Secret = new byte[32];
    private static readonly DateOnly Day = new(2026, 8, 14);

    [Fact]
    public void Compute_IsDeterministic()
        => Assert.Equal(
            VisitorKey.Compute(Secret, Day, "1.2.3.4", "UA"),
            VisitorKey.Compute(Secret, Day, "1.2.3.4", "UA"));

    [Fact]
    public void Compute_Produces64LowercaseHexChars()
    {
        var key = VisitorKey.Compute(Secret, Day, "1.2.3.4", "UA");
        Assert.Equal(AnalyticsRules.VisitorKeyLength, key.Length);
        Assert.Matches("^[0-9a-f]{64}$", key);
    }

    [Fact]
    public void Compute_ChangesWithEachInput()
    {
        var baseline = VisitorKey.Compute(Secret, Day, "1.2.3.4", "UA");

        Assert.NotEqual(baseline, VisitorKey.Compute(Secret, Day.AddDays(1), "1.2.3.4", "UA"));
        Assert.NotEqual(baseline, VisitorKey.Compute(Secret, Day, "1.2.3.5", "UA"));
        Assert.NotEqual(baseline, VisitorKey.Compute(Secret, Day, "1.2.3.4", "UA2"));

        var otherSecret = new byte[32];
        otherSecret[0] = 1;
        Assert.NotEqual(baseline, VisitorKey.Compute(otherSecret, Day, "1.2.3.4", "UA"));
    }
}
