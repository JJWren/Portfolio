namespace Portfolio.Web.Services;

/// <summary>
/// Per-key fixed-window limiter for contact submissions (in-memory): a thin
/// derivation of <see cref="SubmissionLimiter"/> keeping its original name
/// and numbers so <c>Contact.razor</c> and <c>ContactRateLimiterTests</c>
/// compile and pass unchanged. <c>new</c> on <see cref="MaxPerWindow"/> and
/// <see cref="Window"/> is required: they intentionally hide the base
/// class's instance members of the same name with this type's own
/// compile-time constants, the shape every existing caller already depends
/// on. Unlike <see cref="CommentLimiter"/> and <see cref="ReportLimiter"/>,
/// which pass their numbers straight to the base constructor and add no
/// members of their own, this type alone keeps that static shape because it
/// is the one pre-existing caller-visible surface this generalization had
/// to leave untouched.
/// </summary>
public sealed class ContactRateLimiter(TimeProvider timeProvider)
    : SubmissionLimiter(timeProvider, MaxPerWindow, Window)
{
    public new const int MaxPerWindow = 3;
    public new static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
}
