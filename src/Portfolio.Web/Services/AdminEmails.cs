namespace Portfolio.Web.Services;

/// <summary>
/// Owners listed in ADMIN_EMAILS (comma-separated) receive the Admin role
/// when they sign in with a matching OAuth email.
/// </summary>
public class AdminEmails
{
    private readonly HashSet<string> _emails;

    public AdminEmails(IConfiguration config)
        : this(config["ADMIN_EMAILS"]) { }

    public AdminEmails(string? commaSeparated)
    {
        _emails = (commaSeparated ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAdmin(string? email)
        => !string.IsNullOrWhiteSpace(email) && _emails.Contains(email);
}
