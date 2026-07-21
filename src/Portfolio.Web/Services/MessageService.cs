using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class MessageService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<(UserMessage? Message, string? Error)> SendAsync(
        string recipientId, string? senderId, string? body,
        string? quotedComment = null, int? reportId = null)
    {
        var normalized = CommentRules.Validate(body, out var error);
        if (normalized is null)
        {
            return (null, error);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var message = new UserMessage
        {
            RecipientId = recipientId,
            SenderId = senderId,
            Body = normalized,
            QuotedComment = ReportRules.Excerpt(quotedComment),
            ReportId = reportId,
            CreatedAt = DateTime.UtcNow,
        };
        db.UserMessages.Add(message);
        await db.SaveChangesAsync();
        return (message, null);
    }

    public async Task<PagedResult<UserMessage>> GetForUserPageAsync(string userId, int page)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var inbox = db.UserMessages.AsNoTracking().Where(m => m.RecipientId == userId);
        var total = await inbox.CountAsync();
        page = PagedResult<UserMessage>.ClampPage(page, total, PageSizes.Admin);
        var items = await inbox
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * PageSizes.Admin)
            .Take(PageSizes.Admin)
            .ToListAsync();
        return new PagedResult<UserMessage>(items, page, PageSizes.Admin, total);
    }

    public async Task<List<UserMessage>> GetForReportAsync(int reportId, string recipientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserMessages.AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ReportId == reportId && m.RecipientId == recipientId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> UnreadCountAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserMessages.CountAsync(m => m.RecipientId == userId && !m.IsRead);
    }

    public async Task MarkReadAsync(int messageId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.UserMessages
            .Where(m => m.Id == messageId && m.RecipientId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }
}
