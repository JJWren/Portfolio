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

    public async Task<PagedResult<Comment>> GetAdminPageAsync(
        int page, string? search = null, bool? hidden = null, int? postId = null,
        CommentSortColumn sortColumn = CommentSortColumn.CreatedAt,
        SortDirection sortDirection = SortDirection.Descending)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var comments = db.Comments.AsNoTracking().AsQueryable();

        search = BlogFilters.Normalize(search);
        if (search is not null)
        {
            var pattern = $"%{BlogFilters.EscapeLike(search)}%";
            comments = comments.Where(c =>
                EF.Functions.ILike(c.Body, pattern, "\\")
                || (c.User.DisplayName != null && EF.Functions.ILike(c.User.DisplayName, pattern, "\\"))
                || (c.User.CustomDisplayName != null && EF.Functions.ILike(c.User.CustomDisplayName, pattern, "\\"))
                || (c.User.Email != null && EF.Functions.ILike(c.User.Email, pattern, "\\")));
        }

        if (hidden is not null)
        {
            comments = comments.Where(c => c.IsHidden == hidden.Value);
        }

        if (postId is not null)
        {
            comments = comments.Where(c => c.BlogPostId == postId);
        }

        var total = await comments.CountAsync();
        page = PagedResult<Comment>.ClampPage(page, total, PageSizes.Admin);
        var items = await ApplySort(
                comments.Include(c => c.User).Include(c => c.BlogPost),
                sortColumn, sortDirection)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * PageSizes.Admin)
            .Take(PageSizes.Admin)
            .ToListAsync();
        return new PagedResult<Comment>(items, page, PageSizes.Admin, total);
    }

    private static IOrderedQueryable<Comment> ApplySort(
        IQueryable<Comment> comments, CommentSortColumn column, SortDirection direction)
        => column switch
        {
            // Mirrors the value the Comment cell leads with: DisplayName ?? Email.
            CommentSortColumn.Author => QuerySort.By(comments, c => c.User.DisplayName ?? c.User.Email, direction),
            CommentSortColumn.Post => QuerySort.By(comments, c => c.BlogPost.Title, direction),
            CommentSortColumn.State => QuerySort.By(comments, c => c.IsHidden, direction),
            _ => QuerySort.By(comments, c => c.CreatedAt, direction),
        };

    /// <summary>Posts that have at least one comment — for the admin filter picker.</summary>
    public async Task<List<(int Id, string Title)>> GetPostsWithCommentsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.BlogPosts.AsNoTracking()
            .Where(p => db.Comments.Any(c => c.BlogPostId == p.Id))
            .OrderBy(p => p.Title)
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();
        return rows.Select(r => (r.Id, r.Title)).ToList();
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
