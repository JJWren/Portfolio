using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class BlogService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<PagedResult<BlogPost>> GetPublishedPageAsync(
        int page, string? query = null, string? month = null, string? tag = null,
        int pageSize = PageSizes.Posts)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var posts = db.BlogPosts.AsNoTracking().Where(p => p.IsPublished);

        query = BlogFilters.Normalize(query);
        if (query is not null)
        {
            var pattern = $"%{BlogFilters.EscapeLike(query)}%";
            posts = posts.Where(p =>
                EF.Functions.ILike(p.Title, pattern, "\\")
                || EF.Functions.ILike(p.Summary, pattern, "\\"));
        }

        if (BlogFilters.TryParseMonth(month, out var monthStart, out var monthEnd))
        {
            posts = posts.Where(p => p.PublishedAt >= monthStart && p.PublishedAt < monthEnd);
        }

        tag = BlogFilters.Normalize(tag);
        if (tag is not null)
        {
            posts = posts.Where(p => p.Tags.Contains(tag));
        }

        var total = await posts.CountAsync();
        page = PagedResult<BlogPost>.ClampPage(page, total, pageSize);
        var items = await posts
            .OrderByDescending(p => p.PublishedAt)
            .ThenByDescending(p => p.Id) // deterministic tie-breaker keeps page boundaries stable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<BlogPost>(items, page, pageSize, total);
    }

    /// <summary>Latest published posts without the paging COUNT — for the RSS feed.</summary>
    public async Task<List<BlogPost>> GetLatestPublishedAsync(int count)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .ThenByDescending(p => p.Id)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>Distinct publication months (yyyy-MM, newest first) for the filter dropdown.</summary>
    public async Task<List<string>> GetPublishedMonthsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var months = await db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished && p.PublishedAt != null)
            .Select(p => new { p.PublishedAt!.Value.Year, p.PublishedAt.Value.Month })
            .Distinct()
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync();
        return months.Select(m => $"{m.Year:D4}-{m.Month:D2}").ToList();
    }

    /// <summary>Lightweight slug listing for the sitemap.</summary>
    public async Task<List<(string Slug, DateTime UpdatedAt)>> GetPublishedSlugsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync();
        return rows.Select(r => (r.Slug, r.UpdatedAt)).ToList();
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
