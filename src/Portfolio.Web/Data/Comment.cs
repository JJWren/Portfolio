namespace Portfolio.Web.Data;

public class Comment
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    /// <summary>Plain text; rendered encoded, never as HTML or markdown.</summary>
    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Moderation soft-hide: kept in the DB, invisible to the public.</summary>
    public bool IsHidden { get; set; }
}
