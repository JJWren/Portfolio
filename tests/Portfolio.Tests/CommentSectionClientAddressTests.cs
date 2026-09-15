using Portfolio.Tests.Support;

namespace Portfolio.Tests;

/// <summary>
/// Pins the <c>ClientAddress</c> parameter path (FR-D12, FR-D14) the way
/// <c>ResumeLinksTests</c> and <c>ThemeToggleTooltipTests</c> pin other
/// Razor markup: by scanning the linked source copied into the test output,
/// rather than driving a browser or a component renderer (the project
/// deliberately has no test host, tech-stack-decisions.md).
/// </summary>
public class CommentSectionClientAddressTests
{
    private static string Linked(string relativePath) => LinkedSource.Read("RazorComponents", relativePath);

    [Fact]
    public void BlogPostPage_DeclaresACascadingHttpContext_AndPassesItsAddressToTheIsland()
    {
        var page = Linked(Path.Combine("Pages", "BlogPostPage.razor"));

        var cascadingIndex = page.IndexOf("[CascadingParameter]", StringComparison.Ordinal);
        var httpContextIndex = page.IndexOf("public HttpContext? HttpContext { get; set; }", StringComparison.Ordinal);
        Assert.True(cascadingIndex >= 0, "Expected a [CascadingParameter] HttpContext.");
        Assert.True(httpContextIndex > cascadingIndex, "Expected the HttpContext property right after its [CascadingParameter] attribute.");

        Assert.Contains(
            @"ClientAddress=""@(HttpContext?.Connection.RemoteIpAddress?.ToString())""",
            page, StringComparison.Ordinal);
    }

    [Fact]
    public void CommentSection_DeclaresTheClientAddressParameter_AndForwardsItToBothServiceCalls()
    {
        var section = Linked("CommentSection.razor");

        Assert.Contains("public string? ClientAddress { get; set; }", section, StringComparison.Ordinal);

        var addAsyncIndex = section.IndexOf("Comments.AddAsync(", StringComparison.Ordinal);
        var createAsyncIndex = section.IndexOf("Reports.CreateAsync(", StringComparison.Ordinal);
        Assert.True(addAsyncIndex >= 0, "Expected the AddAsync call site.");
        Assert.True(createAsyncIndex >= 0, "Expected the CreateAsync call site.");

        var addAsyncCall = section[addAsyncIndex..section.IndexOf(';', addAsyncIndex)];
        var createAsyncCall = section[createAsyncIndex..section.IndexOf(';', createAsyncIndex)];
        Assert.Contains("ClientAddress", addAsyncCall, StringComparison.Ordinal);
        Assert.Contains("ClientAddress", createAsyncCall, StringComparison.Ordinal);
    }
}
