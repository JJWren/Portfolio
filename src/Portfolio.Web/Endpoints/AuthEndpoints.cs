using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Data;
using Portfolio.Web.Services;

namespace Portfolio.Web.Endpoints;

public static class AuthEndpoints
{
    public const string AdminRole = "Admin";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapGet("/login/{provider}", (
            string provider,
            string? returnUrl,
            SignInManager<ApplicationUser> signInManager,
            OAuthProviders providers) =>
        {
            var known = providers.Enabled.FirstOrDefault(
                p => p.Scheme.Equals(provider, StringComparison.OrdinalIgnoreCase));
            if (known is null)
            {
                return Results.NotFound();
            }

            var callback = $"/auth/external-callback?returnUrl={Uri.EscapeDataString(Sanitize(returnUrl))}";
            var props = signInManager.ConfigureExternalAuthenticationProperties(known.Scheme, callback);
            return Results.Challenge(props, [known.Scheme]);
        });

        group.MapGet("/external-callback", async (
            string? returnUrl,
            HttpContext httpContext,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AdminEmails adminEmails,
            ILogger<Program> logger) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return Results.Redirect("/signin?error=external");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    logger.LogWarning("{Provider} sign-in returned no email claim", info.LoginProvider);
                    return Results.Redirect("/signin?error=noemail");
                }

                // Same email from a different provider signs into the same account.
                user = await userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                    };
                    var created = await userManager.CreateAsync(user);
                    if (!created.Succeeded)
                    {
                        logger.LogError("Failed to create user for {Provider}: {Errors}",
                            info.LoginProvider, string.Join("; ", created.Errors.Select(e => e.Description)));
                        return Results.Redirect("/signin?error=create");
                    }
                }

                await userManager.AddLoginAsync(user, info);
            }

            // Refresh profile details and admin membership on every sign-in.
            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? user.Email;
            if (user.DisplayName != displayName)
            {
                user.DisplayName = displayName;
                await userManager.UpdateAsync(user);
            }

            var shouldBeAdmin = adminEmails.IsAdmin(user.Email);
            var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);
            if (shouldBeAdmin && !isAdmin)
            {
                await userManager.AddToRoleAsync(user, AdminRole);
            }
            else if (!shouldBeAdmin && isAdmin)
            {
                await userManager.RemoveFromRoleAsync(user, AdminRole);
            }

            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await signInManager.SignInAsync(user, isPersistent: true, info.LoginProvider);
            return Results.LocalRedirect(Sanitize(returnUrl));
        });

        group.MapPost("/logout", async (
            [FromForm] string? returnUrl,
            SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect(Sanitize(returnUrl));
        });
    }

    /// <summary>Keeps redirect targets on this site (open-redirect guard).</summary>
    private static string Sanitize(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl
            : "/";
}
