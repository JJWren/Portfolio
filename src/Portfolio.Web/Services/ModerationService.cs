using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Endpoints;

namespace Portfolio.Web.Services;

public class ModerationService(IDbContextFactory<AppDbContext> dbFactory)
{
    /// <summary>Bans a user; admins cannot be banned. Returns false when refused.</summary>
    public async Task<bool> BanAsync(string userId, string? reason)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var isAdmin = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == AuthEndpoints.AdminRole);
        if (isAdmin)
        {
            return false;
        }

        var updated = await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsBanned, true)
                .SetProperty(u => u.BannedAt, DateTime.UtcNow)
                .SetProperty(u => u.BanReason, ReportRules.Excerpt(reason)));
        return updated > 0;
    }

    public async Task UnbanAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsBanned, false)
                .SetProperty(u => u.BannedAt, (DateTime?)null)
                .SetProperty(u => u.BanReason, (string?)null));
    }
}
