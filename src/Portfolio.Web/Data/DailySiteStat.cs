namespace Portfolio.Web.Data;

/// <summary>
/// Site-wide totals for one UTC day. A row exists for every rolled-up day —
/// including zero-traffic days — so the latest Day doubles as the rollup
/// watermark.
/// </summary>
public class DailySiteStat
{
    public DateOnly Day { get; set; }

    public int Views { get; set; }

    /// <summary>Distinct visitor keys that day. Keys rotate daily by design,
    /// so cross-day uniques intentionally don't exist.</summary>
    public int Visitors { get; set; }
}
