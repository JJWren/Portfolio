using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ReportService(IDbContextFactory<AppDbContext> dbFactory, MessageService messages)
{
    /// <summary>Files a report; returns null and an error message when invalid.</summary>
    public async Task<(Report? Report, string? Error)> CreateAsync(
        string reporterId, int commentId, ReportTargetType targetType,
        string? reason, string? details)
    {
        if (!ReportRules.Validate(reason, details, out var error))
        {
            return (null, error);
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var reporter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reporterId);
        if (reporter is null || reporter.IsBanned)
        {
            return (null, "Your account can't file reports right now.");
        }

        var comment = await db.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null)
        {
            return (null, "That comment no longer exists.");
        }

        if (comment.UserId == reporterId)
        {
            return (null, "You can't report your own comment — you can delete it instead.");
        }

        var duplicate = await db.Reports.AnyAsync(r =>
            r.ReporterId == reporterId
            && r.Status == ReportStatus.Open
            && r.TargetType == targetType
            && (targetType == ReportTargetType.Comment
                ? r.CommentId == commentId
                : r.TargetUserId == comment.UserId));
        if (duplicate)
        {
            return (null, "You already have an open report for this — it's in the queue.");
        }

        var report = new Report
        {
            ReporterId = reporterId,
            TargetUserId = comment.UserId,
            CommentId = commentId,
            CommentExcerpt = ReportRules.Excerpt(comment.Body),
            TargetType = targetType,
            Reason = reason!,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            Status = ReportStatus.Open,
            CreatedAt = DateTime.UtcNow,
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return (report, null);
    }

    public async Task<List<Report>> GetAllForAdminAsync(bool openOnly)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Reports.AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.TargetUser)
            .Include(r => r.Comment!).ThenInclude(c => c.BlogPost)
            .AsQueryable();
        if (openOnly)
        {
            query = query.Where(r => r.Status == ReportStatus.Open);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<int> OpenCountAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Reports.CountAsync(r => r.Status == ReportStatus.Open);
    }

    public async Task<List<Report>> GetMineAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Reports.AsNoTracking()
            .Where(r => r.ReporterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Closes a report; an optional note is delivered to the reporter's inbox.</summary>
    public async Task ResolveAsync(int reportId, ReportStatus status, string adminId, string? noteToReporter)
    {
        if (status == ReportStatus.Open)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is null)
        {
            return;
        }

        report.Status = status;
        report.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(noteToReporter))
        {
            await messages.SendAsync(
                report.ReporterId, adminId, noteToReporter,
                quotedComment: report.CommentExcerpt, reportId: report.Id);
        }
    }
}
