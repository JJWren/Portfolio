namespace Portfolio.Tests;

/// <summary>
/// Pins the résumé link markup (Unit 13: FR-R6, FR-R9, FR-R10) the way
/// ThemeToggleTooltipTests and NoInlineOnClickTests pin other Razor markup:
/// by scanning the linked source files copied into the test output, rather
/// than driving a browser or a component renderer.
/// </summary>
public class ResumeLinksTests
{
    private static string Linked(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "RazorComponents", relativePath);
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/LinkBase item in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }

    [Fact]
    public void MainLayout_FooterNav_AriaLabelIsLinksNotSocial()
    {
        var layout = Linked(Path.Combine("Layout", "MainLayout.razor"));

        Assert.Contains(@"<nav class=""footer-links"" aria-label=""Links"">", layout, StringComparison.Ordinal);
        Assert.DoesNotContain(@"aria-label=""Social""", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void MainLayout_ResumeAnchor_SitsBetweenEmailAndSponsorLink_GatedByAvailability()
    {
        var layout = Linked(Path.Combine("Layout", "MainLayout.razor"));

        var emailIndex = layout.IndexOf(@"<a href=""mailto:@Site.ContactEmail"">", StringComparison.Ordinal);
        var resumeIndex = layout.IndexOf("/resume?from=footer", StringComparison.Ordinal);
        var sponsorIndex = layout.IndexOf(@"class=""sponsor-link""", StringComparison.Ordinal);

        Assert.True(emailIndex >= 0, "Expected the Email anchor.");
        Assert.True(resumeIndex >= 0, "Expected the /resume?from=footer anchor.");
        Assert.True(sponsorIndex >= 0, "Expected the sponsor-link anchor.");
        Assert.True(emailIndex < resumeIndex, "The résumé anchor should follow the Email anchor.");
        Assert.True(resumeIndex < sponsorIndex, "The résumé anchor should precede the sponsor-link anchor.");

        var guardIndex = layout.LastIndexOf("@if (Resume.IsAvailable)", resumeIndex, StringComparison.Ordinal);
        Assert.True(guardIndex >= 0, "The résumé anchor should sit inside an @if (Resume.IsAvailable) block.");
        Assert.Contains(@"data-enhance-nav=""false""", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Contact_ResumeAnchor_GatedByAvailability_AndNoLongerReferencesSiteResumeFile()
    {
        var contact = Linked(Path.Combine("Pages", "Contact.razor"));

        Assert.Contains("/resume?from=contact", contact, StringComparison.Ordinal);
        Assert.DoesNotContain("Site.ResumeFile", contact, StringComparison.Ordinal);

        var resumeIndex = contact.IndexOf("/resume?from=contact", StringComparison.Ordinal);
        var guardIndex = contact.LastIndexOf("@if (Resume.IsAvailable)", resumeIndex, StringComparison.Ordinal);
        Assert.True(guardIndex >= 0, "The Contact résumé anchor should sit inside an @if (Resume.IsAvailable) block.");
    }

    [Fact]
    public void SiteContentEditor_ContainsTheResumeBlockCopyAndControls()
    {
        var editor = Linked(Path.Combine("Admin", "SiteContentEditor.razor"));

        Assert.Contains(
            "Set RESUME_FILE in .env (and mount its folder read-write)",
            editor, StringComparison.Ordinal);
        Assert.Contains("to enable the résumé.", editor, StringComparison.Ordinal);
        Assert.Contains(
            "No résumé yet. Upload a PDF here, or copy one onto",
            editor, StringComparison.Ordinal);
        Assert.Contains(
            "the PDF served at /resume and linked from the footer and Contact page",
            editor, StringComparison.Ordinal);
        Assert.Contains(@"accept=""application/pdf,.pdf""", editor, StringComparison.Ordinal);
        Assert.Contains(@"href=""/resume"" download", editor, StringComparison.Ordinal);
        Assert.Contains("Download current", editor, StringComparison.Ordinal);
        Assert.Contains("Upload PDF", editor, StringComparison.Ordinal);
        Assert.Contains(">Remove<", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_SiteContentCard_MentionsResume()
    {
        var dashboard = Linked(Path.Combine("Admin", "Dashboard.razor"));

        Assert.Contains(
            "Override the landing-page copy and swap the photos and résumé.",
            dashboard, StringComparison.Ordinal);
    }
}
