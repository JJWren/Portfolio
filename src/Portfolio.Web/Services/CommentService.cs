using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class CommentService(IDbContextFactory<AppDbContext> dbFactory)
{
    /// <summary>Oldest-first window for incremental "show more" loading.</summary>
    public async Task<(List<Comment> Items, int TotalCount)> GetVisibleForPostWindowAsync(int postId, int take)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var visible = db.Comments.AsNoTracking()
            .Where(c => c.BlogPostId == postId && !c.IsHidden);
        var total = await visible.CountAsync();
        var items = await visible
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .Take(take)
            .ToListAsync();
        return (items, total);
    }

    public async Task<List<Comment>> GetAllForAdminAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Comments.AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.BlogPost)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Adds a comment; returns null and an error message when the body is invalid.</summary>
    public async Task<(Comment? Comment, string? Error)> AddAsync(
        int postId, string userId, string? body, bool isAnonymous = false)
    {
        var normalized = CommentRules.Validate(body, out var error);
        if (normalized is null)
        {
            return (null, error);
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var banned = await db.Users.AnyAsync(u => u.Id == userId && u.IsBanned);
        if (banned)
        {
            return (null, "Your account is currently restricted from commenting.");
        }

        var comment = new Comment
        {
            BlogPostId = postId,
            UserId = userId,
            Body = normalized,
            CreatedAt = DateTime.UtcNow,
            IsAnonymous = isAnonymous,
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return (comment, null);
    }

    /// <summary>Deletes a comment when the requester owns it or is an admin.</summary>
    public async Task<bool> DeleteAsync(int commentId, string requestingUserId, bool isAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var deleted = await db.Comments
            .Where(c => c.Id == commentId && (isAdmin || c.UserId == requestingUserId))
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    public async Task SetHiddenAsync(int commentId, bool hidden)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Comments
            .Where(c => c.Id == commentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsHidden, hidden));
    }
}
