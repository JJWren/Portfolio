namespace Portfolio.Web.Data;

public class BlogPost
{
    public int Id { get; set; }

    /// <summary>URL segment, unique among all posts.</summary>
    public required string Slug { get; set; }

    public required string Title { get; set; }

    /// <summary>Short plain-text teaser shown in lists and feeds.</summary>
    public string Summary { get; set; } = string.Empty;

    public string BodyMarkdown { get; set; } = string.Empty;

    /// <summary>Optional header image path or URL.</summary>
    public string? HeaderImagePath { get; set; }

    public List<string> Tags { get; set; } = [];

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Set the first time the post is published; drives ordering.</summary>
    public DateTime? PublishedAt { get; set; }
}
