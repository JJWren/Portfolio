using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Portfolio.Web.Services;

/// <summary>
/// Framework rate limiting (FR-D9, FR-D10): the three named,
/// per-client-address fixed-window policies attached to endpoints, the
/// path-scoped global limiter that covers the OAuth handler callback paths
/// (not endpoints, so <c>RequireRateLimiting</c> has nothing to attach to),
/// and the 429 rejection response. Registered with
/// <c>builder.Services.AddRateLimiter(RateLimitPolicies.Configure)</c> in
/// <c>Program.cs</c>.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Feeds = "feeds";
    public const string Redirects = "redirects";

    public const int AuthPermitLimit = 10;
    public const int FeedsPermitLimit = 30;
    public const int RedirectsPermitLimit = 30;

    /// <summary>Shared by every policy and by the global limiter's sign-in partition.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The OAuth handlers' own callback paths (<c>/signin-github</c>,
    /// <c>/signin-google</c>, <c>/signin-discord</c>) — registered by each
    /// provider's own middleware, not this app's endpoints, so the global
    /// limiter reaches them by path instead of <c>RequireRateLimiting</c>.
    /// </summary>
    public const string SignInPathPrefix = "/signin-";

    /// <summary>
    /// The connecting address (already the trusted forwarded address once
    /// <c>TRUSTED_PROXIES</c> applies), mapped to IPv4 when it arrives as an
    /// IPv4-mapped IPv6 address so the same client never keys two different
    /// partitions; <c>"unknown"</c> when the connection has none.
    /// </summary>
    public static string ClientKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }

    /// <summary>True for the OAuth handler callback paths the global limiter's sign-in partition covers.</summary>
    public static bool IsSignInHandlerPath(PathString path)
        => path.Value?.StartsWith(SignInPathPrefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// The fixed-window options every named policy and the global limiter's
    /// sign-in partition share except for their permit count: no queue (a
    /// refused request is rejected immediately, never held), automatic
    /// replenishment on <see cref="Window"/>.
    /// </summary>
    public static FixedWindowRateLimiterOptions WindowOptions(int permitLimit) => new()
    {
        PermitLimit = permitLimit,
        Window = Window,
        QueueLimit = 0,
        AutoReplenishment = true,
    };

    /// <summary>Whole seconds until the window resets, at least 1 — never zero, so a client is never told to retry immediately.</summary>
    public static int RetryAfterSeconds(TimeSpan retryAfter)
        => Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

    /// <summary>
    /// The 429 response body (FR-D10): a short plain sentence, so
    /// <c>UseStatusCodePagesWithReExecute</c> (which only re-executes
    /// body-less error responses) never turns this rejection into the 404
    /// page.
    /// </summary>
    public static string RejectionBody(int retryAfterSeconds)
        => $"Too many requests. Try again in {retryAfterSeconds} seconds.\n";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(Auth, context =>
            RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => WindowOptions(AuthPermitLimit)));
        options.AddPolicy(Feeds, context =>
            RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => WindowOptions(FeedsPermitLimit)));
        options.AddPolicy(Redirects, context =>
            RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => WindowOptions(RedirectsPermitLimit)));

        // The OAuth handler callback paths are not endpoints, so this
        // path-scoped global limiter is the only way to reach them: the
        // auth numbers for a sign-in path, no limit at all for every other
        // path (the three named policies above already cover the endpoints
        // that need one).
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            IsSignInHandlerPath(context.Request.Path)
                ? RateLimitPartition.GetFixedWindowLimiter("signin:" + ClientKey(context), _ => WindowOptions(AuthPermitLimit))
                : RateLimitPartition.GetNoLimiter("none"));

        options.OnRejected = async (context, cancellationToken) =>
        {
            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
                ? metadata
                : TimeSpan.Zero;
            var seconds = RetryAfterSeconds(retryAfter);

            context.HttpContext.Response.Headers["Retry-After"] = seconds.ToString();
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(RejectionBody(seconds), cancellationToken);
        };
    }
}
