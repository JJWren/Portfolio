using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class EmailTemplatesTests
{
    private static readonly DateTime ReceivedAt = new(2026, 8, 15, 14, 30, 0, DateTimeKind.Utc);

    private static (string Html, string Text) Build(
        string name = "Jane Visitor",
        string email = "jane@example.com",
        string subject = "Hello there",
        string bodyHtml = "<p>Nice site!</p>",
        string bodyText = "Nice site!",
        string siteLabel = "example.dev",
        string? adminUrl = "https://example.dev/admin")
        => EmailTemplates.ContactNotification(
            name, email, subject, bodyHtml, bodyText, ReceivedAt, siteLabel, adminUrl);

    [Fact]
    public void Html_CarriesTheRibbonStripAndCardStructure()
    {
        var (html, _) = Build();

        foreach (var color in new[] { "#a63d40", "#e9b872", "#90a959", "#6494aa" })
        {
            Assert.Contains($"background:{color}", html);
        }

        Assert.Contains("New contact message", html);
        Assert.Contains("max-width:560px", html);
    }

    [Fact]
    public void Html_CarriesMetadataAndSanitizedBody()
    {
        var (html, _) = Build();

        Assert.Contains("Jane Visitor", html);
        Assert.Contains("mailto:jane@example.com", html);
        Assert.Contains("2026-08-15 14:30 UTC", html);
        Assert.Contains("Hello there", html);
        // Pre-sanitized body HTML is embedded unmodified.
        Assert.Contains("<p>Nice site!</p>", html);
    }

    [Fact]
    public void Html_EscapesHostileNameAndSubject()
    {
        var (html, _) = Build(
            name: "<script>alert(1)</script>",
            subject: "\"quoted\" & <b>bold</b>");

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<b>bold</b>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Text_KeepsTheRawBodyAndSenderLine()
    {
        var (_, text) = Build(
            name: "<script>alert(1)</script>",
            bodyText: "Line one\n\n**markdown** stays raw");

        // The plain part is not HTML — hostile input stays literal.
        Assert.StartsWith("From: <script>alert(1)</script> <jane@example.com>", text);
        Assert.Contains("Received: 2026-08-15 14:30 UTC", text);
        Assert.EndsWith("**markdown** stays raw", text);
    }

    [Fact]
    public void Footer_LinksTheAdminInboxOnlyWhenConfigured()
    {
        var (withLink, _) = Build(adminUrl: "https://example.dev/admin");
        var (without, _) = Build(adminUrl: null);

        Assert.Contains("https://example.dev/admin", withLink);
        Assert.DoesNotContain("admin inbox", without);
        Assert.Contains("example.dev", without);
    }
}
