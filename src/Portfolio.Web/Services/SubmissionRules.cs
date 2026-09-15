using System.Security.Claims;
using Portfolio.Web.Endpoints;

namespace Portfolio.Web.Services;

/// <summary>
/// Pure rules shared by comment posting and report submission's
/// application-level rate limits (FR-D12): who is exempt, the per-caller
/// key, and the friendly wait message shown inline when a limiter refuses.
/// </summary>
public static class SubmissionRules
{
    /// <summary>Admins are never limited.</summary>
    public static bool IsExempt(ClaimsPrincipal user) => user.IsInRole(AuthEndpoints.AdminRole);

    /// <summary>
    /// A signed-in caller keys on their user id (<c>user:&lt;id&gt;</c>); an
    /// anonymous caller keys on the client address (<c>ip:&lt;address&gt;</c>),
    /// or <c>ip:unknown</c> when neither is available. Comments and reports
    /// both require sign-in today (the form only renders for a signed-in
    /// user), so the <c>ip:</c> branch is dormant in production — it exists
    /// for a future anonymous submission path and goes unused for now.
    /// </summary>
    public static string Key(string? userId, string? clientAddress)
        => !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : !string.IsNullOrWhiteSpace(clientAddress)
                ? $"ip:{clientAddress}"
                : "ip:unknown";

    /// <summary>
    /// "You've posted a few {what} in a row. Try again in about {N}
    /// minute(s)." — minutes rounded up and at least 1, singular for 1.
    /// <paramref name="what"/> is "comments" or "reports".
    /// </summary>
    public static string WaitMessage(TimeSpan retryAfter, string what)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
        var unit = minutes == 1 ? "minute" : "minutes";
        return $"You've posted a few {what} in a row. Try again in about {minutes} {unit}.";
    }
}
