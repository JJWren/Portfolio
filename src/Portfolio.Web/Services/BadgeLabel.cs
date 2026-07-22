namespace Portfolio.Web.Services;

public static class BadgeLabel
{
    /// <summary>Compact count label for nav badges: the number itself, capped at "9+".</summary>
    public static string Format(int count) => count > 9 ? "9+" : count.ToString();
}
