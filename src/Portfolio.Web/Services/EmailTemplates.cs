using System.Text.Encodings.Web;

namespace Portfolio.Web.Services;

/// <summary>
/// Branded HTML + plain-text bodies for outgoing notification email. Pure
/// string building so the output is unit-testable without SMTP; EmailService
/// stays the thin transport shell. Colors are the site's light-theme defaults
/// hardcoded — email clients can't follow the runtime theme editor, and light
/// backgrounds survive dark-mode client rewriting where dark ones don't.
/// </summary>
public static class EmailTemplates
{
    private const string Red = "#a63d40";
    private const string Gold = "#e9b872";
    private const string Green = "#90a959";
    private const string Blue = "#6494aa";
    private const string Bg = "#f6f3ec";
    private const string Surface = "#fdfbf6";
    private const string Surface2 = "#ece7db";
    private const string Border = "#d8d1c2";
    private const string Text = "#24211c";
    private const string Muted = "#6b6459";
    private const string Accent = "#8a6520";

    private const string SerifFont = "Georgia, 'Times New Roman', serif";
    private const string BodyFont = "'Segoe UI', system-ui, sans-serif";
    private const string MonoFont = "Consolas, 'Cascadia Mono', monospace";

    /// <summary>
    /// The contact-form notification. <paramref name="bodyHtml"/> must already
    /// be sanitized (MarkdownService.ToSafeHtml — the UGC pipeline); every
    /// other visitor-supplied value is entity-escaped here. The text part
    /// carries the raw markdown so multipart/alternative degrades to what the
    /// plain email said before.
    /// </summary>
    public static (string Html, string Text) ContactNotification(
        string visitorName,
        string visitorEmail,
        string subject,
        string bodyHtml,
        string bodyText,
        DateTime receivedAtUtc,
        string siteLabel,
        string? adminUrl)
    {
        var received = $"{receivedAtUtc:yyyy-MM-dd HH:mm} UTC";
        var inner = $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 18px;">
              <tr>
                <td style="background:{Surface2};border-radius:6px;padding:12px 16px;font-family:{MonoFont};font-size:13px;line-height:1.6;color:{Text};">
                  From: <b>{E(visitorName)}</b> &lt;<a href="mailto:{E(visitorEmail)}" style="color:{Accent};">{E(visitorEmail)}</a>&gt;<br>
                  Received: {received}
                </td>
              </tr>
            </table>
            <p style="margin:0 0 10px;font-family:{SerifFont};font-size:18px;font-weight:700;color:{Text};">{E(subject)}</p>
            <div style="font-family:{BodyFont};font-size:15px;line-height:1.55;color:{Text};">{bodyHtml}</div>
            """;

        var text = $"From: {visitorName} <{visitorEmail}>\nReceived: {received}\n\n{bodyText}";
        return (Layout("New contact message", inner, siteLabel, adminUrl), text);
    }

    /// <summary>Shared shell — ribbon strip, card, footer — for any future
    /// notification type to reuse.</summary>
    private static string Layout(string title, string innerHtml, string siteLabel, string? adminUrl)
    {
        var footer = adminUrl is null
            ? $"Sent by the contact form at {E(siteLabel)}"
            : $"""Sent by the contact form at {E(siteLabel)} &middot; <a href="{E(adminUrl)}" style="color:{Accent};">admin inbox</a>""";

        return $"""
            <!doctype html>
            <html>
            <body style="margin:0;padding:0;background:{Bg};">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Bg};">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;max-width:560px;">
                      <tr>
                        <td>
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td width="25%" height="4" style="background:{Red};font-size:0;line-height:0;">&nbsp;</td>
                              <td width="25%" height="4" style="background:{Gold};font-size:0;line-height:0;">&nbsp;</td>
                              <td width="25%" height="4" style="background:{Green};font-size:0;line-height:0;">&nbsp;</td>
                              <td width="25%" height="4" style="background:{Blue};font-size:0;line-height:0;">&nbsp;</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="background:{Surface};border:1px solid {Border};border-top:none;padding:28px 32px;">
                          <h1 style="margin:0 0 16px;font-family:{SerifFont};font-size:22px;font-weight:700;color:{Text};">{E(title)}</h1>
                          {innerHtml}
                        </td>
                      </tr>
                      <tr>
                        <td align="center" style="padding:14px 8px;font-family:{MonoFont};font-size:12px;color:{Muted};">
                          {footer}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string E(string value) => HtmlEncoder.Default.Encode(value);
}
