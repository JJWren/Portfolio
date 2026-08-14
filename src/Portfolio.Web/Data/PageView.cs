namespace Portfolio.Web.Data;

/// <summary>
/// One anonymous page view. Contains no personal data: VisitorKey is a
/// one-way daily-rotating hash and the raw IP/User-Agent are never stored.
/// Raw rows live for AnalyticsRollup.RetentionPeriod, then only the daily
/// aggregates remain.
/// </summary>
public class PageView
{
    public long Id { get; set; }

    /// <summary>Request path only — query strings are dropped.</summary>
    public required string Path { get; set; }

    /// <summary>External referrer host; null for direct or internal navigation.</summary>
    public string? ReferrerHost { get; set; }

    public required string VisitorKey { get; set; }

    public DateTime OccurredAt { get; set; }
}
