namespace Portfolio.Web.Data;

/// <summary>
/// A message delivered to a user's on-site inbox — moderation warnings,
/// report outcomes, and similar. Senderless messages read as system notices.
/// </summary>
public class UserMessage
{
    public int Id { get; set; }

    public required string RecipientId { get; set; }

    public ApplicationUser Recipient { get; set; } = null!;

    /// <summary>Admin who sent it; null reads as a system notice.</summary>
    public string? SenderId { get; set; }

    public ApplicationUser? Sender { get; set; }

    public required string Body { get; set; }

    /// <summary>Snapshot of the offending comment being referenced, if any.</summary>
    public string? QuotedComment { get; set; }

    /// <summary>Related report, when the message is a report outcome/response.</summary>
    public int? ReportId { get; set; }

    public Report? Report { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}
