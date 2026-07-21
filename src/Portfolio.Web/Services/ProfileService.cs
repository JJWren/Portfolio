using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ProfileService(IDbContextFactory<AppDbContext> dbFactory, AvatarService avatars)
{
    public async Task<ApplicationUser?> GetAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task UpdateAsync(string userId, string? customDisplayName, bool postAnonymouslyByDefault)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.CustomDisplayName, customDisplayName)
                .SetProperty(u => u.PostAnonymouslyByDefault, postAnonymouslyByDefault));
    }

    public async Task<string> SetAvatarAsync(string userId, Stream image, CancellationToken cancellationToken = default)
    {
        // Save new file → point the DB at it → only then clean up old files,
        // so a failed DB update never leaves the record referencing a deleted file.
        var path = await avatars.SaveAsync(image, userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.AvatarUrl, path), cancellationToken);
        avatars.Delete(userId, exceptFileName: Path.GetFileName(path));
        return path;
    }

    public async Task RemoveAvatarAsync(string userId)
    {
        // Clear the reference first; file deletion is best-effort afterwards.
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.AvatarUrl, (string?)null));
        avatars.Delete(userId);
    }
}
