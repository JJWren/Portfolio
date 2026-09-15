using System.Collections.Concurrent;

namespace Portfolio.Web.Services;

/// <summary>
/// Per-key fixed-window limiter (in-memory): the contact form's original
/// algorithm generalized so every caller-scoped submission limit —
/// <see cref="ContactRateLimiter"/>, <see cref="CommentLimiter"/> and
/// <see cref="ReportLimiter"/> — can reuse it with its own numbers.
/// </summary>
public class SubmissionLimiter(TimeProvider timeProvider, int maxPerWindow, TimeSpan window)
{
    public int MaxPerWindow { get; } = maxPerWindow;

    public TimeSpan Window { get; } = window;

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _hits = new();

    /// <summary>Prunes hits older than the window; refuses when the count is already at the limit, else records this one and allows it.</summary>
    public bool Allow(string key)
    {
        var now = timeProvider.GetUtcNow();
        var list = _hits.GetOrAdd(key, _ => []);
        lock (list)
        {
            list.RemoveAll(t => now - t > Window);
            if (list.Count >= MaxPerWindow)
            {
                return false;
            }

            list.Add(now);
            return true;
        }
    }

    /// <summary>
    /// The time until the oldest hit still inside the window ages out —
    /// what a caller should wait before <see cref="Allow"/> would say yes
    /// again. <see cref="TimeSpan.Zero"/> when a call right now would be
    /// allowed (an unseen key, or one still under the limit).
    /// </summary>
    public TimeSpan RetryAfter(string key)
    {
        if (!_hits.TryGetValue(key, out var list))
        {
            return TimeSpan.Zero;
        }

        var now = timeProvider.GetUtcNow();
        lock (list)
        {
            list.RemoveAll(t => now - t > Window);
            if (list.Count < MaxPerWindow)
            {
                return TimeSpan.Zero;
            }

            var oldest = list.Min();
            var retryAfter = Window - (now - oldest);
            return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
        }
    }
}
