using Microsoft.AspNetCore.Identity;

namespace Portfolio.Web.Data;

public class ApplicationUser : IdentityUser
{
    /// <summary>Display name sourced from the OAuth provider profile.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Avatar URL sourced from the OAuth provider profile.</summary>
    public string? AvatarUrl { get; set; }
}
