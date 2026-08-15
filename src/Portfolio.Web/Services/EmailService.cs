using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Portfolio.Web.Services;

/// <summary>
/// Sends notification email via SMTP configured in the environment.
/// Disabled (no-op) when SMTP__HOST is blank so the contact form still works
/// DB-only for self-hosters without a mail account.
/// </summary>
public class EmailService(
    IConfiguration config, SiteConfig site, MarkdownService markdown, ILogger<EmailService> logger)
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

            // The body is visitor markdown — render through the sanitizing UGC
            // pipeline (never ToHtml); everything else the template escapes.
            // Normalized like SeoRules.CanonicalOrigin; the admin link only
            // renders when the base URL parses as an absolute URI.
            var baseUrl = config["PUBLIC_BASE_URL"]?.Trim().TrimEnd('/');
            Uri? origin = null;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                Uri.TryCreate(baseUrl, UriKind.Absolute, out origin);
            }

            var (html, text) = EmailTemplates.ContactNotification(
                visitorName, visitorEmail, subject,
                bodyHtml: markdown.ToSafeHtml(body),
                bodyText: body,
                receivedAtUtc: DateTime.UtcNow,
                siteLabel: origin?.Host ?? site.SiteTitle,
                adminUrl: origin is null ? null : $"{baseUrl}/admin");
            var builder = new BodyBuilder { HtmlBody = html, TextBody = text };
            message.Body = builder.ToMessageBody();

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
