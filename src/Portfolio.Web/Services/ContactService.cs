using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ContactService(IDbContextFactory<AppDbContext> dbFactory, EmailService email)
{
    /// <summary>Stores the message, then attempts email notification (best-effort).</summary>
    public async Task SubmitAsync(string name, string senderEmail, string subject, string body)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.ContactMessages.Add(new ContactMessage
        {
            Name = name.Trim(),
            Email = senderEmail.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            ReceivedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await email.TrySendContactNotificationAsync(name, senderEmail, subject, body);
    }

    public async Task<List<ContactMessage>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync();
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
