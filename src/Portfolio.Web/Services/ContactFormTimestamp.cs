using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace Portfolio.Web.Services;

/// <summary>
/// Issues a tamper-proof "form rendered at" token that rides the static-SSR
/// contact form in a hidden field, so the server can tell how long the
/// visitor took between seeing the form and submitting it.
/// </summary>
public class ContactFormTimestamp(IDataProtectionProvider dataProtection, TimeProvider timeProvider)
{
    private readonly IDataProtector _protector =
        dataProtection.CreateProtector("Portfolio.ContactForm.RenderedAt");

    public string Issue()
        => _protector.Protect(
            timeProvider.GetUtcNow().UtcTicks.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// False when the token is missing, tampered with, undecipherable (e.g.
    /// the DataProtection key rotated since render), or from the future.
    /// </summary>
    public bool TryGetElapsed(string? token, out TimeSpan elapsed)
    {
        elapsed = default;
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        long renderedTicks;
        try
        {
            var plaintext = _protector.Unprotect(token);
            if (!long.TryParse(plaintext, NumberStyles.None, CultureInfo.InvariantCulture, out renderedTicks))
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        elapsed = TimeSpan.FromTicks(timeProvider.GetUtcNow().UtcTicks - renderedTicks);
        return elapsed >= TimeSpan.Zero;
    }
}
