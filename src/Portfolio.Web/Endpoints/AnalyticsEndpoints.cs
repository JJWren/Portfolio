using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Web.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        // Outbound project links route through here so clicks can be counted
        // without any client-side script. Not an open redirect: the target
        // comes solely from the admin-edited project columns.
        app.MapGet("/go/{id:int}/{kind}", async (
            int id, string kind, HttpContext ctx,
            IDbContextFactory<AppDbContext> dbFactory, AnalyticsService analytics) =>
        {
            if (kind is not ("home" or "repo"))
            {
                return Results.NotFound();
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var project = await db.Projects.AsNoTracking()
                .Where(p => p.Id == id && p.IsVisible)
                .Select(p => new { p.Title, p.HomepageUrl, p.RepoUrl })
                .FirstOrDefaultAsync();
            var url = kind == "home" ? project?.HomepageUrl : project?.RepoUrl;
            if (project is null || !ProjectUrlRules.IsHttp(url))
            {
                return Results.NotFound();
            }

            await analytics.TryRecordEventAsync(
                ctx, AnalyticsRules.ProjectClickEvent, $"{project.Title}|{kind}");
            return Results.Redirect(url!);
        })
        .RequireRateLimiting(RateLimitPolicies.Redirects);

        // Availability-gated through ResumeService.IsAvailable, the one rule
        // shared with the Contact and footer links. The origin is
        // allowlisted (ResumeRules.ParseOrigin) so only "footer" or "contact"
        // ever reach the database as the event's target.
        app.MapGet("/resume", async (
            HttpContext ctx, SiteConfig site, ResumeService resume, AnalyticsService analytics) =>
        {
            if (!resume.IsAvailable)
            {
                return Results.NotFound();
            }

            var origin = ResumeRules.ParseOrigin(ctx.Request.Query[ResumeRules.OriginQueryKey]);
            await analytics.TryRecordEventAsync(ctx, AnalyticsRules.ResumeDownloadEvent, origin);
            return Results.File(
                site.ResumeFile!, "application/pdf",
                fileDownloadName: Path.GetFileName(site.ResumeFile));
        })
        .RequireRateLimiting(RateLimitPolicies.Redirects);
    }
}
