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
///
/// Also caches the composed header set: <see cref="SecurityOptions.CspMode"/>
/// is resolved once at startup and never changes, and the theme's style hash
/// changes only on a save (<see cref="ThemeService.SaveAsync"/>), so most
/// requests — static assets included — can reuse the same composed list
/// instead of paying <see cref="SecurityHeadersRules.Compose"/>'s allocation
/// and string-join on every request.
///
/// Reads <see cref="ThemeService.LastKnown"/> in preference to a fresh
/// <see cref="ThemeService.GetSnapshotAsync"/>, so a database outage does not
/// turn every request — this middleware wraps all of them — into a retried
/// database call, and stores the snapshot it used on <c>context.Items</c>
/// under <see cref="ThemeSnapshotItemKey"/> so the root component renders
/// its override block from the very same snapshot this response's CSP hash
/// was computed from.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// <see cref="HttpContext.Items"/> key under which this middleware
    /// stores the exact <see cref="ThemeSnapshot"/> it used to compose this
    /// response's headers, so <c>App.razor</c> can render its override
    /// <c>&lt;style&gt;</c> block from that same snapshot instead of reading
    /// (and possibly loading a different) one — the header's CSP hash and
    /// the rendered block must always agree.
    /// </summary>
    public const string ThemeSnapshotItemKey = "SecurityHeaders.ThemeSnapshot";

    /// <summary>One composition's header/directive list, paired with the style hash it was composed for.</summary>
    private sealed record ComposedHeaders(string? Hash, IReadOnlyList<(string Name, string Value)> Headers);

    // ComposedHeaders is immutable, so _cache holds a single object
    // reference: assigning it is atomic and, marked volatile, visible to
    // every thread immediately, so a concurrent reader always sees a whole
    // (Hash, Headers) pair from one composition — never a torn mix of an old
    // Hash with new Headers (or the reverse), which a two-field ValueTuple
    // field could produce, since writing one is really two separate field
    // writes, not a single reference assignment. Two requests racing a hash
    // change might both miss the cache and recompose, but Compose is pure,
    // so that's just one wasted recomposition, not a correctness problem.
    private volatile ComposedHeaders? _cache;

    public async Task InvokeAsync(HttpContext context, SecurityOptions options, ThemeService themes)
    {
        // Prefer the last-known snapshot over a fresh load: this middleware
        // wraps every request, static assets and /healthz included, and
        // GetSnapshotAsync retries the database on every call while it's
        // unavailable (its blip guard returns the default snapshot without
        // caching it). LastKnown is set by a successful load or a save and
        // survives an outage, so once the app has served one snapshot no
        // request touches the database for headers again; before that first
        // load, LastKnown is null and this falls back to today's per-request
        // retry — the pages cannot render without the database at that point
        // either.
        var snapshot = themes.LastKnown ?? await themes.GetSnapshotAsync();

        // Stashed so App.razor can render its override <style> block from
        // this exact snapshot: the CSP hash below and the block must always
        // come from the same one, or the browser refuses the block under a
        // hash that no longer matches it.
        context.Items[ThemeSnapshotItemKey] = snapshot;

        var headers = ComposeCached(options.CspMode, snapshot.OverrideCssHash);

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

    /// <summary>
    /// Returns the cached header list when <paramref name="hash"/> matches
    /// the one it was last composed for, recomposing only on a miss (the
    /// first request, or the request right after a theme save).
    /// </summary>
    private IReadOnlyList<(string Name, string Value)> ComposeCached(CspMode mode, string? hash)
    {
        var cached = _cache;
        if (cached is not null && cached.Hash == hash)
        {
            return cached.Headers;
        }

        var composed = SecurityHeadersRules.Compose(mode, hash);
        _cache = new ComposedHeaders(hash, composed);
        return composed;
    }
}
