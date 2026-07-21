using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

/// <summary>
/// Populates sample content on first run when SEED_DEMO_DATA=true so a fresh
/// self-hosted instance isn't an empty shell. Never touches non-empty tables.
/// </summary>
public static class DemoSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.BlogPosts.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.BlogPosts.AddRange(
                new BlogPost
                {
                    Slug = "welcome",
                    Title = "Welcome to your new portfolio",
                    Summary = "What this site can do and where to start customizing it.",
                    BodyMarkdown = """
                        # Welcome

                        This site is running from a Docker image configured entirely through
                        environment variables — your name, links, and contact details live in
                        `.env`, not in code.

                        ## Where to go next

                        - Sign in with an account whose email is in `ADMIN_EMAILS`
                        - Open the hidden **Admin** area from the nav
                        - Replace this post and add your projects

                        ```bash
                        docker compose up -d
                        ```
                        """,
                    Tags = ["meta", "getting-started"],
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    PublishedAt = now,
                },
                new BlogPost
                {
                    Slug = "markdown-tour",
                    Title = "A quick markdown tour",
                    Summary = "Tables, code blocks, and everything else posts support.",
                    BodyMarkdown = """
                        Posts are written in markdown with the advanced pipeline enabled.

                        | Feature | Supported |
                        |---------|-----------|
                        | Tables  | Yes       |
                        | Code    | Yes       |

                        ```csharp
                        var greeting = "Syntax highlighting is bundled locally.";
                        Console.WriteLine(greeting);
                        ```

                        > Blockquotes pick up the gold accent from the palette.
                        """,
                    Tags = ["demo"],
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    PublishedAt = now.AddMinutes(-5),
                });
        }

        if (!await db.Projects.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Projects.AddRange(
                new Project
                {
                    Title = "This portfolio",
                    Summary = "Self-hostable Blazor portfolio with a blog, OAuth comments, and an admin area.",
                    RepoUrl = "https://github.com/JJWren/Portfolio",
                    SortOrder = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new Project
                {
                    Title = "Sample project",
                    Summary = "Replace me in the admin area — cards support an image, homepage, and repo link.",
                    HomepageUrl = "https://example.com",
                    SortOrder = 2,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
        }

        await db.SaveChangesAsync();
    }
}
