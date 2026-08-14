using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ContactService(IDbContextFactory<AppDbContext> dbFactory, EmailService email)
{
    /// <summary>Stores the message, then attempts email notification (best-effort).
    /// A non-null <paramref name="flagReason"/> quarantines it: stored for
    /// admin review, but no notification email is sent.</summary>
    public async Task SubmitAsync(
        string name, string senderEmail, string subject, string body, string? flagReason = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.ContactMessages.Add(new ContactMessage
        {
            Name = name.Trim(),
            Email = senderEmail.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            ReceivedAt = DateTime.UtcNow,
            IsFlagged = flagReason is not null,
            FlagReason = flagReason,
        });
        await db.SaveChangesAsync();

        if (flagReason is null)
        {
            await email.TrySendContactNotificationAsync(name, senderEmail, subject, body);
        }
    }

    public async Task<PagedResult<ContactMessage>> GetAdminPageAsync(
        int page, string? search = null, bool? isRead = null, bool? isFlagged = null,
        MessageSortColumn sortColumn = MessageSortColumn.ReceivedAt,
        SortDirection sortDirection = SortDirection.Descending)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var messages = db.ContactMessages.AsNoTracking().AsQueryable();

        if (isFlagged is not null)
        {
            messages = messages.Where(m => m.IsFlagged == isFlagged.Value);
        }

        search = BlogFilters.Normalize(search);
        if (search is not null)
        {
            var pattern = $"%{BlogFilters.EscapeLike(search)}%";
            messages = messages.Where(m =>
                EF.Functions.ILike(m.Name, pattern, "\\")
                || EF.Functions.ILike(m.Email, pattern, "\\")
                || EF.Functions.ILike(m.Subject, pattern, "\\"));
        }

        if (isRead is not null)
        {
            messages = messages.Where(m => m.IsRead == isRead.Value);
        }

        var total = await messages.CountAsync();
        page = PagedResult<ContactMessage>.ClampPage(page, total, PageSizes.Admin);
        var items = await ApplySort(messages, sortColumn, sortDirection)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * PageSizes.Admin)
            .Take(PageSizes.Admin)
            .ToListAsync();
        return new PagedResult<ContactMessage>(items, page, PageSizes.Admin, total);
    }

    private static IOrderedQueryable<ContactMessage> ApplySort(
        IQueryable<ContactMessage> messages, MessageSortColumn column, SortDirection direction)
        => column switch
        {
            MessageSortColumn.From => QuerySort.By(messages, m => m.Name, direction),
            MessageSortColumn.Subject => QuerySort.By(messages, m => m.Subject, direction),
            MessageSortColumn.State => QuerySort.By(messages, m => m.IsRead, direction),
            _ => QuerySort.By(messages, m => m.ReceivedAt, direction),
        };

    /// <summary>Quarantined messages are excluded — this count feeds the
    /// nav attention badge and quarantine shouldn't ping like real mail.</summary>
    public async Task<int> UnreadCountAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ContactMessages.CountAsync(m => !m.IsRead && !m.IsFlagged);
    }

    public async Task<int> FlaggedCountAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ContactMessages.CountAsync(m => m.IsFlagged);
    }

    /// <summary>Admin "Not spam" action; clearing the flag also clears the reason.</summary>
    public async Task SetFlaggedAsync(int id, bool flagged)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.ContactMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsFlagged, flagged)
                .SetProperty(m => m.FlagReason, m => flagged ? m.FlagReason : null));
    }

    public async Task SetReadAsync(int id, bool read)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.ContactMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, read));
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.ContactMessages.Where(m => m.Id == id).ExecuteDeleteAsync();
    }
}
