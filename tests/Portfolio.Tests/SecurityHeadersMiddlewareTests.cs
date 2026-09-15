using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// Drives SecurityHeadersMiddleware over a bare DefaultHttpContext — the
/// project deliberately has no test host (tech-stack-decisions.md). A
/// throwing IDbContextFactory backs a real ThemeService, so
/// GetSnapshotAsync's DB-blip guard always returns ThemeRules.DefaultSnapshot
/// (OverrideCssHash null) without touching a database; the hash-present CSP
/// value itself is pinned separately by SecurityHeadersRulesTests and
/// ThemeRulesTests. RecordingResponseFeature stands in for the server: the
/// framework's own default HttpResponseFeature treats OnStarting as a no-op
/// (a real server overrides it), so this fake records the registered
/// callback and fires it on FireOnStartingAsync — the way Kestrel would,
/// just before the first byte of the response leaves.
///
/// Two more Unit 12a review facts live here: the middleware now prefers
/// ThemeService.LastKnown over a fresh GetSnapshotAsync (NewThemeServiceWithLastKnown
/// seeds it, the same reflection seam NewThemeServiceWithHash already uses
/// for the private _cache field, and ThrowingDbFactory.Calls proves the
/// database is never touched once LastKnown is set), and it stashes the
/// snapshot it used under ThemeSnapshotItemKey for App.razor to read back.
/// </summary>
public class SecurityHeadersMiddlewareTests
{
    private sealed class ThrowingDbFactory : IDbContextFactory<AppDbContext>
    {
        public int Calls { get; private set; }

        public AppDbContext CreateDbContext()
        {
            Calls++;
            throw new InvalidOperationException("No database in this test.");
        }
    }

    private sealed class RecordingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => false;

        /// <summary>
        /// The most recent OnStarting registration's state object — the same
        /// (HttpResponse, IReadOnlyList&lt;(string, string)&gt;) pair
        /// SecurityHeadersMiddleware itself unpacks, exposed so a test can
        /// inspect the composed list's identity without firing the callback.
        /// </summary>
        public object? LastOnStartingState { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStarting.Add((callback, state));
            LastOnStartingState = state;
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            // Not exercised by these tests.
        }

        public async Task FireOnStartingAsync()
        {
            foreach (var (callback, state) in _onStarting)
            {
                await callback(state);
            }
        }
    }

    private static (DefaultHttpContext Context, RecordingResponseFeature Response) NewContext()
    {
        var context = new DefaultHttpContext();
        var response = new RecordingResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        return (context, response);
    }

    private static ThemeService NewThemeService() => new(new ThrowingDbFactory());

    /// <summary>
    /// A ThemeService whose GetSnapshotAsync returns DefaultSnapshot with
    /// <paramref name="hash"/> substituted for OverrideCssHash, without a
    /// database: reflection seeds the private in-process cache field
    /// GetSnapshotAsync already checks first, the same field a real save
    /// populates.
    /// </summary>
    private static ThemeService NewThemeServiceWithHash(string hash)
    {
        var service = new ThemeService(new ThrowingDbFactory());
        var snapshot = ThemeRules.DefaultSnapshot with { OverrideCssHash = hash };
        typeof(ThemeService)
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, snapshot);
        return service;
    }

    /// <summary>
    /// A ThemeService whose LastKnown returns DefaultSnapshot with
    /// <paramref name="hash"/> substituted for OverrideCssHash, without a
    /// database: reflection seeds the private LastKnown property (a real
    /// value only a prior successful GetSnapshotAsync or SaveAsync can
    /// produce), the same seam NewThemeServiceWithHash uses for _cache.
    /// The backing ThrowingDbFactory is returned too, so a test can assert
    /// InvokeAsync never reached it.
    /// </summary>
    private static (ThemeService Service, ThrowingDbFactory Factory) NewThemeServiceWithLastKnown(string hash)
    {
        var factory = new ThrowingDbFactory();
        var service = new ThemeService(factory);
        var snapshot = ThemeRules.DefaultSnapshot with { OverrideCssHash = hash };
        typeof(ThemeService)
            .GetProperty(nameof(ThemeService.LastKnown))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(service, [snapshot]);
        return (service, factory);
    }

    /// <summary>Unpacks the composed header list from a captured OnStarting state, the same cast the middleware itself does.</summary>
    private static IReadOnlyList<(string Name, string Value)> ComposedHeaders(RecordingResponseFeature response)
    {
        var (_, headers) = ((HttpResponse Response, IReadOnlyList<(string Name, string Value)> Headers))response.LastOnStartingState!;
        return headers;
    }

    private static async Task<IHeaderDictionary> RunAsync(CspMode mode)
    {
        var (context, response) = NewContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, new SecurityOptions(mode), NewThemeService());
        await response.FireOnStartingAsync();

        return response.Headers;
    }

    [Fact]
    public async Task InvokeAsync_Enforce_SetsEveryHeaderWithTheEnforcedCspName()
    {
        var headers = await RunAsync(CspMode.Enforce);

        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
        Assert.Equal(SecurityHeadersRules.PermissionsPolicy, headers["Permissions-Policy"].ToString());
        Assert.Equal("same-origin", headers["Cross-Origin-Opener-Policy"].ToString());
        Assert.Equal("off", headers["X-DNS-Prefetch-Control"].ToString());
        Assert.Equal("none", headers["X-Permitted-Cross-Domain-Policies"].ToString());
        // No override in this test's ThemeService (DB-blip fallback), so no hash.
        Assert.Equal(SecurityHeadersRules.BuildCsp(null), headers["Content-Security-Policy"].ToString());
        Assert.False(headers.ContainsKey("Content-Security-Policy-Report-Only"));
    }

    [Fact]
    public async Task InvokeAsync_ReportOnly_UsesTheReportOnlyHeaderName()
    {
        var headers = await RunAsync(CspMode.ReportOnly);

        Assert.Equal(SecurityHeadersRules.BuildCsp(null), headers["Content-Security-Policy-Report-Only"].ToString());
        Assert.False(headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task InvokeAsync_Off_SendsNoCspHeaderButKeepsEveryOtherHeader()
    {
        var headers = await RunAsync(CspMode.Off);

        Assert.False(headers.ContainsKey("Content-Security-Policy"));
        Assert.False(headers.ContainsKey("Content-Security-Policy-Report-Only"));
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
        Assert.Equal("off", headers["X-DNS-Prefetch-Control"].ToString());
        Assert.Equal("none", headers["X-Permitted-Cross-Domain-Policies"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_FillIfAbsent_NeverOverwritesAHeaderTheResponseAlreadyCarries()
    {
        // Simulates the /uploads static-file middleware's OnPrepareResponse,
        // which runs — and sets its own policy — before this middleware's
        // OnStarting callback fires.
        var (context, response) = NewContext();
        response.Headers["Content-Security-Policy"] = SecurityHeadersRules.UploadsCsp;
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, new SecurityOptions(CspMode.Enforce), NewThemeService());
        await response.FireOnStartingAsync();

        // The policy set earlier wins...
        Assert.Equal(SecurityHeadersRules.UploadsCsp, response.Headers["Content-Security-Policy"].ToString());
        // ...but every header this middleware found absent is still added.
        Assert.Equal("DENY", response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("nosniff", response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ComposedHeaders_AreCachedByHashAndRecomposedOnlyWhenItChanges()
    {
        // One middleware instance, so its cache field persists across calls.
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var options = new SecurityOptions(CspMode.Enforce);

        var (context1, response1) = NewContext();
        await middleware.InvokeAsync(context1, options, NewThemeService()); // hash null
        var (context2, response2) = NewContext();
        await middleware.InvokeAsync(context2, options, NewThemeService()); // hash null again

        Assert.Same(ComposedHeaders(response1), ComposedHeaders(response2));

        var (context3, response3) = NewContext();
        await middleware.InvokeAsync(context3, options, NewThemeServiceWithHash("sha256-changed=="));

        Assert.NotSame(ComposedHeaders(response1), ComposedHeaders(response3));
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var (context, _) = NewContext();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new SecurityOptions(CspMode.Enforce), NewThemeService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_StashesTheSnapshotItUsed_MatchingTheComposedCspHash()
    {
        var (context, response) = NewContext();
        var themes = NewThemeServiceWithHash("sha256-item-test==");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, new SecurityOptions(CspMode.Enforce), themes);
        await response.FireOnStartingAsync();

        // The request item is the exact snapshot the header's hash came from.
        var stashed = Assert.IsType<ThemeSnapshot>(context.Items[SecurityHeadersMiddleware.ThemeSnapshotItemKey]);
        Assert.Equal("sha256-item-test==", stashed.OverrideCssHash);
        Assert.Equal(
            SecurityHeadersRules.BuildCsp("sha256-item-test=="),
            response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_PrefersLastKnownOverAFreshLoad_AndNeverTouchesTheDatabase()
    {
        var (context, response) = NewContext();
        var (themes, factory) = NewThemeServiceWithLastKnown("sha256-last-known==");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, new SecurityOptions(CspMode.Enforce), themes);

        // GetSnapshotAsync (hence GetOverridesAsync and the factory) is never
        // reached: LastKnown short-circuits the "??" before it's evaluated.
        Assert.Equal(0, factory.Calls);

        await response.FireOnStartingAsync();
        Assert.Equal(
            SecurityHeadersRules.BuildCsp("sha256-last-known=="),
            response.Headers["Content-Security-Policy"].ToString());
        var stashed = Assert.IsType<ThemeSnapshot>(context.Items[SecurityHeadersMiddleware.ThemeSnapshotItemKey]);
        Assert.Equal("sha256-last-known==", stashed.OverrideCssHash);
    }
}
