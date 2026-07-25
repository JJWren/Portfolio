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
}
