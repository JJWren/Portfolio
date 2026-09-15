namespace Portfolio.Web.Services;

/// <summary>
/// Per-key fixed-window limiter for report submission over the circuit
/// (FR-D12): 3 per 10 minutes, keyed by <see cref="SubmissionRules.Key"/>.
/// A typed derivation of <see cref="SubmissionLimiter"/>, distinct from
/// <see cref="CommentLimiter"/>, so DI can tell the two apart.
/// </summary>
public sealed class ReportLimiter(TimeProvider timeProvider)
    : SubmissionLimiter(timeProvider, maxPerWindow: 3, window: TimeSpan.FromMinutes(10))
{
}
