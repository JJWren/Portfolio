using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class BlogService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<BlogPost>> GetPublishedAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }

    public async Task<BlogPost?> GetPublishedBySlugAsync(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
    }

    public async Task<List<BlogPost>> GetAllForAdminAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != excludeId);
    }

    public async Task<BlogPost> SaveAsync(BlogPost post)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        post.UpdatedAt = now;

        if (post.IsPublished && post.PublishedAt is null)
        {
            post.PublishedAt = now;
        }

        if (post.Id == 0)
        {
            post.CreatedAt = now;
            db.BlogPosts.Add(post);
        }
        else
        {
            db.BlogPosts.Update(post);
        }

        await db.SaveChangesAsync();
        return post;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.BlogPosts.Where(p => p.Id == id).ExecuteDeleteAsync();
    }
}
