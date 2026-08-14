namespace Portfolio.Web.Data;

/// <summary>Per-event totals for one UTC day. Surrogate key because Target is nullable.</summary>
public class DailyEventStat
{
    public int Id { get; set; }

    public DateOnly Day { get; set; }

    public required string Name { get; set; }

    public string? Target { get; set; }

    public int Count { get; set; }
}
