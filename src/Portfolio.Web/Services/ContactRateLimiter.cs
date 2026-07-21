using System.Collections.Concurrent;

namespace Portfolio.Web.Services;

/// <summary>Per-key fixed-window limiter for contact submissions (in-memory).</summary>
public class ContactRateLimiter(TimeProvider timeProvider)
{
    public const int MaxPerWindow = 3;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _hits = new();

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
}
