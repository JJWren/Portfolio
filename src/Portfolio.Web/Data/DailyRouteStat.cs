namespace Portfolio.Web.Data;

/// <summary>Per-route totals for one UTC day.</summary>
public class DailyRouteStat
{
    public int Id { get; set; }

    public DateOnly Day { get; set; }

    public required string Path { get; set; }

    public int Views { get; set; }

    public int Visitors { get; set; }
}
