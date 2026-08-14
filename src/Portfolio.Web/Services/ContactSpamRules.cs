using System.Text.RegularExpressions;

namespace Portfolio.Web.Services;

/// <summary>
/// Pure spam heuristics for the contact form. Hard signals (honeypot, a
/// submit faster than <see cref="MinSubmitTime"/>) mean "provably a bot" and
/// get a silent fake success; soft signals mean "suspicious but possibly
/// human" and quarantine the message for admin review instead of dropping it.
/// </summary>
public static partial class ContactSpamRules
{
    /// <summary>No human reads the form and writes a message this fast.</summary>
    public static readonly TimeSpan MinSubmitTime = TimeSpan.FromSeconds(4);

    /// <summary>More links than this in the body is a soft signal.</summary>
    public const int MaxBodyLinks = 1;

    public const int FlagReasonMaxLength = 200;

    /// <summary>Flag reason recorded when the render-timestamp token can't be
    /// read — expected after a DataProtection key reset, so it must never
    /// silently drop mail.</summary>
    public const string InvalidTokenReason = "invalid-token";

    [GeneratedRegex(@"https?://", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern { get; }

    /// <summary>Counts links: bare http(s) URLs plus markdown link targets.</summary>
    public static int CountLinks(string body)
    {
        var bare = UrlPattern.Matches(body).Count;
        // Markdown targets without a scheme, e.g. [x](www.spam.example).
        var markdown = Regex.Matches(body, @"\]\((?!https?://)[^)\s]+\)").Count;
        return bare + markdown;
    }

    public static bool SubjectHasUrl(string subject)
        => UrlPattern.IsMatch(subject) || subject.Contains("www.", StringComparison.OrdinalIgnoreCase);

    public static string? DomainOf(string email)
    {
        var at = email.LastIndexOf('@');
        return at < 0 || at == email.Length - 1 ? null : email[(at + 1)..].Trim();
    }

    /// <summary>Returns comma-joined soft-signal reasons, or null when clean.</summary>
    public static string? SoftFlagReason(
        string email, string subject, string body, DisposableEmailDomains disposable)
    {
        var reasons = new List<string>();
        if (DomainOf(email) is { } domain && disposable.Contains(domain))
        {
            reasons.Add("disposable-domain");
        }

        if (CountLinks(body) > MaxBodyLinks)
        {
            reasons.Add("body-links");
        }

        if (SubjectHasUrl(subject))
        {
            reasons.Add("subject-url");
        }

        return reasons.Count == 0 ? null : string.Join(",", reasons);
    }

    /// <summary>Merges independently-collected reasons into one flag value.</summary>
    public static string? CombineReasons(string? first, string? second)
        => (first, second) switch
        {
            (null, null) => null,
            (null, _) => second,
            (_, null) => first,
            _ => $"{first},{second}",
        };
}
