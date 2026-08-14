namespace Portfolio.Web.Data;

/// <summary>Per-referrer-host view totals for one UTC day.</summary>
public class DailyReferrerStat
{
    public int Id { get; set; }

    public DateOnly Day { get; set; }

    public required string ReferrerHost { get; set; }

    public int Views { get; set; }
}
