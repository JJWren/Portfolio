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
/// </summary>
public class SecurityHeadersMiddlewareTests
{
    private sealed class ThrowingDbFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => throw new InvalidOperationException("No database in this test.");
    }

    private sealed class RecordingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state)
            => _onStarting.Add((callback, state));

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
}
