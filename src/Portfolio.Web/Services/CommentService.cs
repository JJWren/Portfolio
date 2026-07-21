using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class CommentService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Comment>> GetVisibleForPostAsync(int postId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Comments.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.BlogPostId == postId && !c.IsHidden)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
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
    public async Task<(Comment? Comment, string? Error)> AddAsync(int postId, string userId, string? body)
    {
        var normalized = CommentRules.Validate(body, out var error);
        if (normalized is null)
        {
            return (null, error);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var comment = new Comment
        {
            BlogPostId = postId,
            UserId = userId,
            Body = normalized,
            CreatedAt = DateTime.UtcNow,
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
