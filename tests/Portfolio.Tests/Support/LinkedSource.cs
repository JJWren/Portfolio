namespace Portfolio.Tests.Support;

/// <summary>
/// Reads a source file linked into the test output next to the test
/// assembly (a <c>None</c>/<c>Link</c> or <c>None</c>/<c>LinkBase</c> item in
/// Portfolio.Tests.csproj — the project deliberately has no test host,
/// tech-stack-decisions.md), so a fixture can text-scan production source
/// without a parser package. Every fixture that scanned a linked file used
/// to carry its own copy of this same three-line block; this is that block,
/// once.
/// </summary>
internal static class LinkedSource
{
    /// <summary>Combines <see cref="AppContext.BaseDirectory"/> with <paramref name="relativePathSegments"/> and returns that file's text, failing with a message that names the csproj link if it is missing.</summary>
    public static string Read(params string[] relativePathSegments)
    {
        var path = Path.Combine([AppContext.BaseDirectory, .. relativePathSegments]);
        Assert.True(File.Exists(path),
            $"Expected the linked source at {path}; check the None/LinkBase item in Portfolio.Tests.csproj.");
        return File.ReadAllText(path);
    }
}
