namespace Portfolio.Web.Services;

/// <summary>
/// Sets the composed security header set (FR-D1, FR-D2, FR-D5) on every
/// response. Placed beside <see cref="AnalyticsMiddleware"/> in the
/// pipeline, right after <c>UseForwardedHeaders</c>, so static assets, the
/// <c>/uploads</c> static files, the exception handler's <c>/Error</c>
/// re-execution, the re-executed 404, the health check, the Blazor negotiate
/// and circuit responses, the feeds and every Razor component all pass
/// through it.
///
/// Registers <see cref="HttpResponse.OnStarting"/> rather than setting the
/// headers inline: that callback runs exactly once, immediately before the
/// first byte of the response leaves, whatever path the request took, and —
/// unlike setting headers here directly — it survives
/// <c>HttpResponse.Clear()</c> inside <c>UseExceptionHandler</c>'s
/// re-execution. Each header is set only when the response does not already
/// carry it, so a response that set its own policy earlier (the
/// <c>/uploads</c> static-file middleware's <c>OnPrepareResponse</c>, with
/// <see cref="SecurityHeadersRules.UploadsCsp"/>) keeps it — the page policy
/// below never overwrites it.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SecurityOptions options, ThemeService themes)
    {
        // The snapshot is cached in ThemeService's process memory (a
        // database read only on a cache miss, e.g. right after a save), so
        // reading it per request is cheap and always current.
        var snapshot = await themes.GetSnapshotAsync();
        var headers = SecurityHeadersRules.Compose(options.CspMode, snapshot.OverrideCssHash);

        context.Response.OnStarting(static state =>
        {
            var (response, composed) = ((HttpResponse Response, IReadOnlyList<(string Name, string Value)> Headers))state;
            foreach (var (name, value) in composed)
            {
                if (!response.Headers.ContainsKey(name))
                {
                    response.Headers[name] = value;
                }
            }

            return Task.CompletedTask;
        }, (context.Response, headers));

        await next(context);
    }
}
