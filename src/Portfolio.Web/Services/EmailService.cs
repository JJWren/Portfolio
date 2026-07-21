using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Portfolio.Web.Services;

/// <summary>
/// Sends notification email via SMTP configured in the environment.
/// Disabled (no-op) when SMTP__HOST is blank so the contact form still works
/// DB-only for self-hosters without a mail account.
/// </summary>
public class EmailService(IConfiguration config, SiteConfig site, ILogger<EmailService> logger)
{
    public bool Enabled => !string.IsNullOrWhiteSpace(config["SMTP:HOST"]);

    public async Task<bool> TrySendContactNotificationAsync(
        string visitorName, string visitorEmail, string subject, string body)
    {
        var host = config["SMTP:HOST"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        try
        {
            var message = new MimeMessage();
            var from = config["SMTP:FROM"] ?? config["SMTP:USER"] ?? site.ContactEmail;
            message.From.Add(new MailboxAddress(site.SiteTitle, from));
            message.To.Add(new MailboxAddress(site.OwnerName, site.ContactEmail));
            message.ReplyTo.Add(new MailboxAddress(visitorName, visitorEmail));
            message.Subject = $"[Portfolio contact] {subject}";
            message.Body = new TextPart("plain")
            {
                Text = $"From: {visitorName} <{visitorEmail}>\n\n{body}",
            };

            using var client = new SmtpClient();
            var port = int.TryParse(config["SMTP:PORT"], out var p) ? p : 587;
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
            var user = config["SMTP:USER"];
            if (!string.IsNullOrWhiteSpace(user))
            {
                await client.AuthenticateAsync(user, config["SMTP:PASSWORD"] ?? string.Empty);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
            return true;
        }
        catch (Exception ex)
        {
            // The message is already stored in the DB; email is best-effort.
            logger.LogError(ex, "Failed to send contact notification email");
            return false;
        }
    }
}
