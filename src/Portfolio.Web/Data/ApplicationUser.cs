using Microsoft.AspNetCore.Identity;

namespace Portfolio.Web.Data;

public class ApplicationUser : IdentityUser
{
    /// <summary>Display name sourced from the OAuth provider profile (refreshed each sign-in).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Uploaded avatar path under /uploads/avatars; null shows the initial-letter fallback.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>User-chosen handle shown on comments; overrides the provider name when set.</summary>
    public string? CustomDisplayName { get; set; }

    /// <summary>Pre-checks "post anonymously" on the comment form.</summary>
    public bool PostAnonymouslyByDefault { get; set; }

    /// <summary>Banned users can sign in and read, but not comment or report.</summary>
    public bool IsBanned { get; set; }

    public DateTime? BannedAt { get; set; }

    public string? BanReason { get; set; }

    /// <summary>Name shown publicly on comments.</summary>
    public string PublicName => CustomDisplayName ?? DisplayName ?? Email ?? "User";
}
