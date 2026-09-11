using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ResumeRulesTests
{
    [Theory]
    // "%PDF-1.7"
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1', (byte)'.', (byte)'7' }, true)]
    // Four bytes: too short to carry the full "%PDF-" signature.
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F' }, false)]
    [InlineData(new byte[0], false)]
    // A PNG signature, not a PDF one.
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, false)]
    public void IsPdf_ChecksTheLeadingMagicBytes(byte[] header, bool expected)
        => Assert.Equal(expected, ResumeRules.IsPdf(header));

    [Theory]
    [InlineData("footer", "footer")]
    [InlineData("contact", "contact")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Footer", null)]
    [InlineData("other", null)]
    [InlineData("footer ", null)]
    public void ParseOrigin_ExactMatchOnly(string? value, string? expected)
        => Assert.Equal(expected, ResumeRules.ParseOrigin(value));

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(16115, "16 KB")]
    [InlineData(1_572_864, "1.5 MB")]
    public void FormatSize_FormatsByMagnitude(long bytes, string expected)
        => Assert.Equal(expected, ResumeRules.FormatSize(bytes));
}
