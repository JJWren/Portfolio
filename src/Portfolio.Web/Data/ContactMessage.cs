namespace Portfolio.Web.Data;

public class ContactMessage
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string Subject { get; set; }

    /// <summary>Markdown source; rendered via MarkdownService.ToSafeHtml in
    /// the admin inbox.</summary>
    public required string Body { get; set; }

    public DateTime ReceivedAt { get; set; }

    public bool IsRead { get; set; }

    /// <summary>Quarantined by a soft spam signal: stored and admin-reviewable,
    /// but no SMTP notification and excluded from unread counts.</summary>
    public bool IsFlagged { get; set; }

    /// <summary>Comma-joined ContactSpamRules reasons; null when not flagged.</summary>
    public string? FlagReason { get; set; }
}
