using Microsoft.AspNetCore.Diagnostics;
using Portfolio.Web.Endpoints;

namespace Portfolio.Web.Services;

/// <summary>
/// Server-side, cookieless page-view recording. Sits after auth (so the
/// Admin-role exclusion works) and records only successful public HTML GETs.
/// </summary>
public class AnalyticsMiddleware(RequestDelegate next, AnalyticsService analytics)
{
    /// <summary>Set by the HEAD-as-GET rewrite in Program.cs so probe
    /// requests aren't counted as page views.</summary>
    public const string RewrittenHeadKey = "Analytics.RewrittenHead";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldConsider(context))
        {
            await next(context);
            return;
        }

        await next(context);

        // Only count what a person actually saw: a fresh 200 HTML response.
        // The status-code re-execute check keeps 404 pages from counting the
        // pipeline's second pass through this middleware.
        if (context.Response.StatusCode != StatusCodes.Status200OK
            || context.Features.Get<IStatusCodeReExecuteFeature>() is not null
            || context.Response.ContentType?.StartsWith("text/html") != true)
        {
            return;
        }

        var visitorKey = await analytics.ComputeVisitorKeyAsync(context);
        var referrer = AnalyticsRules.NormalizeReferrer(
            context.Request.Headers.Referer, context.Request.Host);
        await analytics.RecordPageViewAsync(context.Request.Path, referrer, visitorKey);
    }

    private static bool ShouldConsider(HttpContext context)
        => HttpMethods.IsGet(context.Request.Method)
            && !context.Items.ContainsKey(RewrittenHeadKey)
            && AnalyticsRules.IsCountablePath(context.Request.Path)
            && !AnalyticsRules.IsBot(context.Request.Headers.UserAgent)
            && !AnalyticsRules.OptedOut(context.Request.Headers)
            && !context.User.IsInRole(AuthEndpoints.AdminRole);
}
