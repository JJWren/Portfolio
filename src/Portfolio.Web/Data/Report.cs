namespace Portfolio.Web.Data;

public enum ReportTargetType
{
    Comment = 0,
    User = 1,
}

public enum ReportStatus
{
    Open = 0,
    Resolved = 1,
    Dismissed = 2,
}

public class Report
{
    public int Id { get; set; }

    public required string ReporterId { get; set; }

    public ApplicationUser Reporter { get; set; } = null!;

    public required string TargetUserId { get; set; }

    public ApplicationUser TargetUser { get; set; } = null!;

    /// <summary>The comment that prompted the report; null once it's deleted.</summary>
    public int? CommentId { get; set; }

    public Comment? Comment { get; set; }

    /// <summary>Snapshot of the offending comment so context survives deletion.</summary>
    public string? CommentExcerpt { get; set; }

    public ReportTargetType TargetType { get; set; }

    public required string Reason { get; set; }

    public string? Details { get; set; }

    public ReportStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
