namespace Portfolio.Web.Services;

/// <summary>
/// Per-key fixed-window limiter for comment posting over the circuit
/// (FR-D12): 5 per 10 minutes, keyed by <see cref="SubmissionRules.Key"/>.
/// A typed derivation of <see cref="SubmissionLimiter"/>, distinct from
/// <see cref="ReportLimiter"/>, so DI can tell the two apart.
/// </summary>
public sealed class CommentLimiter(TimeProvider timeProvider)
    : SubmissionLimiter(timeProvider, MaxPerWindow, Window)
{
    public new const int MaxPerWindow = 5;
    public new static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
}
