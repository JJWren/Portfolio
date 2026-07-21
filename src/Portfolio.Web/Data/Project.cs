namespace Portfolio.Web.Data;

public class Project
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string Summary { get; set; } = string.Empty;

    /// <summary>Card image; a palette placeholder renders when unset.</summary>
    public string? HeaderImagePath { get; set; }

    public string? HomepageUrl { get; set; }

    public string? RepoUrl { get; set; }

    /// <summary>Carousel position, ascending.</summary>
    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
