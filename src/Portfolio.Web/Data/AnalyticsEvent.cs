namespace Portfolio.Web.Data;

/// <summary>A named engagement action (project-click, resume-download,
/// contact-submit). Same anonymity guarantees as PageView.</summary>
public class AnalyticsEvent
{
    public long Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Event detail, e.g. "ProjectTitle|repo" for project clicks.</summary>
    public string? Target { get; set; }

    public required string VisitorKey { get; set; }

    public DateTime OccurredAt { get; set; }
}
