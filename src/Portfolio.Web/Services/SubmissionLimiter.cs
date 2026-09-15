using System.Collections.Concurrent;

namespace Portfolio.Web.Services;

/// <summary>
/// Per-key fixed-window limiter (in-memory): the contact form's original
/// algorithm generalized so every caller-scoped submission limit —
/// <see cref="ContactRateLimiter"/>, <see cref="CommentLimiter"/> and
/// <see cref="ReportLimiter"/> — can reuse it with its own numbers.
/// </summary>
/// <remarks>
/// Concurrency protocol for <c>_hits</c>: a key's entry is only ever
/// removed via a value-checked <c>TryRemove</c> taken while still holding
/// that entry's own list lock (see <see cref="Sweep"/> and the removal
/// branch of <see cref="RetryAfter"/>) — never a bare <c>TryRemove(key)</c>.
/// That makes "the dictionary still maps this key to the list I just
/// locked" a fact that cannot change while the lock is held. <see
/// cref="Allow"/> and <see cref="RetryAfter"/> both obtain a list reference
/// outside any lock (<c>GetOrAdd</c>/<c>TryGetValue</c>), then re-check that
/// fact via <see cref="IsCurrentEntry"/> immediately after locking it: if a
/// concurrent <see cref="Sweep"/> or the other method's removal branch
/// swapped or removed that exact list in the meantime, acting on it anyway
/// would record a hit into a list <c>_hits</c> no longer points at — the
/// same key silently split across two lists, each separately under the
/// limit but together over it. On a failed check both loop and acquire the
/// (now current) list again instead.
/// </remarks>
public class SubmissionLimiter(TimeProvider timeProvider, int maxPerWindow, TimeSpan window)
{
    /// <summary>How often (in <see cref="Allow"/> calls) an automatic <see cref="Sweep"/> runs.</summary>
    private const int SweepEveryNthCall = 256;

    public int MaxPerWindow { get; } = maxPerWindow;

    public TimeSpan Window { get; } = window;

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _hits = new();

    private int _callCount;

    /// <summary>How many keys are currently tracked — memory held by this limiter grows with this (tests only).</summary>
    public int TrackedKeys => _hits.Count;

    /// <summary>Prunes hits at or older than the window; refuses when the count is already at the limit, else records this one and allows it.</summary>
    public bool Allow(string key)
    {
        if (Interlocked.Increment(ref _callCount) % SweepEveryNthCall == 0)
        {
            Sweep();
        }

        var now = timeProvider.GetUtcNow();
        while (true)
        {
            var list = _hits.GetOrAdd(key, _ => []);
            lock (list)
            {
                if (!IsCurrentEntry(key, list))
                {
                    continue;
                }

                list.RemoveAll(t => now - t >= Window);
                if (list.Count >= MaxPerWindow)
                {
                    return false;
                }

                list.Add(now);
                return true;
            }
        }
    }

    /// <summary>
    /// The time until the oldest hit still inside the window ages out —
    /// what a caller should wait before <see cref="Allow"/> would say yes
    /// again. <see cref="TimeSpan.Zero"/> when a call right now would be
    /// allowed (an unseen key, or one still under the limit). Removes the
    /// key when pruning leaves it with no hits, the same bookkeeping
    /// <see cref="Sweep"/> does.
    /// </summary>
    public TimeSpan RetryAfter(string key)
    {
        var now = timeProvider.GetUtcNow();
        while (true)
        {
            if (!_hits.TryGetValue(key, out var list))
            {
                return TimeSpan.Zero;
            }

            lock (list)
            {
                if (!IsCurrentEntry(key, list))
                {
                    continue;
                }

                list.RemoveAll(t => now - t >= Window);
                if (list.Count == 0)
                {
                    // Value-checked, while still holding this list's lock —
                    // see the class remarks: this is the removal half of the
                    // protocol Allow's (and this method's own) re-check
                    // against IsCurrentEntry guards against.
                    _hits.TryRemove(new KeyValuePair<string, List<DateTimeOffset>>(key, list));
                    return TimeSpan.Zero;
                }

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

    /// <summary>
    /// Prunes every tracked key's hit list and drops any key left empty
    /// afterwards, so a key that stops posting stops occupying memory
    /// instead of keeping an entry for the container's whole lifetime.
    /// Runs automatically every <see cref="SweepEveryNthCall"/>th
    /// <see cref="Allow"/> call; also public so tests (and callers wanting
    /// an eager collection) can invoke it directly.
    /// </summary>
    public void Sweep()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in _hits)
        {
            lock (entry.Value)
            {
                entry.Value.RemoveAll(t => now - t >= Window);
                if (entry.Value.Count == 0)
                {
                    // Value-checked, while still holding this list's lock —
                    // see the class remarks. A concurrent Allow call blocked
                    // on this same lock, waiting to add a hit for this key,
                    // either already added it before we got here (the list
                    // is then non-empty and this branch isn't taken) or is
                    // still waiting and will find this exact (key, list)
                    // pair gone once it gets the lock, and retry via
                    // IsCurrentEntry rather than adding to an orphaned list.
                    _hits.TryRemove(entry);
                }
            }
        }
    }

    /// <summary>
    /// True when <paramref name="list"/> — already locked by the caller —
    /// is still the exact list <c>_hits</c> maps <paramref name="key"/> to.
    /// See the class remarks: <see cref="Allow"/> and <see cref="RetryAfter"/>
    /// must both check this before acting on a list obtained outside a lock,
    /// and loop to reacquire rather than act on it when it is false.
    /// </summary>
    private bool IsCurrentEntry(string key, List<DateTimeOffset> list)
        => _hits.TryGetValue(key, out var current) && ReferenceEquals(current, list);
}
