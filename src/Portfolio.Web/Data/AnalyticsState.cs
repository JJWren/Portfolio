namespace Portfolio.Web.Data;

/// <summary>
/// Single-row (Id = 1) holder of the per-install analytics secret that
/// salts visitor-key hashes. Persisted so keys stay stable across restarts
/// within a day while remaining unlinkable across installs.
/// </summary>
public class AnalyticsState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Base64 of 32 random bytes, generated on first use.</summary>
    public required string Secret { get; set; }
}
