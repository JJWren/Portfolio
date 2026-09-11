using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ResumeServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"resume-tests-{Guid.NewGuid():N}");

    private string ResumePath => Path.Combine(_tempDir, "resume", "sample-resume.pdf");

    private ResumeService CreateService(bool configured = true)
        => new(BuildConfig(configured ? ResumePath : null));

    private static SiteConfig BuildConfig(string? resumeFile)
        => new(
            OwnerName: "Jane Developer",
            SiteTitle: "Jane Developer — Portfolio",
            Tagline: string.Empty,
            MetaDescription: null,
            ContactEmail: "jane@example.com",
            ContactPhone: null,
            LinkedInUrl: null,
            GitHubUrl: null,
            About: null,
            Skills: [],
            SponsorUrl: null,
            SponsorText: "Buy me a coffee",
            ResumeFile: resumeFile);

    private static byte[] SamplePdfBytes() => "%PDF-1.7\n%mock resume content for tests\n"u8.ToArray();

    private static MemoryStream PdfStream() => new(SamplePdfBytes());

    [Fact]
    public void IsConfigured_PathSet_ReturnsTrue()
        => Assert.True(CreateService().IsConfigured);

    [Fact]
    public void IsConfigured_PathUnset_ReturnsFalse()
        => Assert.False(CreateService(configured: false).IsConfigured);

    [Fact]
    public void IsAvailable_Unconfigured_ReturnsFalse()
        => Assert.False(CreateService(configured: false).IsAvailable);

    [Fact]
    public void IsAvailable_ConfiguredButMissingFile_ReturnsFalse()
        => Assert.False(CreateService().IsAvailable);

    [Fact]
    public void GetInfo_Unconfigured_ReturnsNull()
        => Assert.Null(CreateService(configured: false).GetInfo());

    [Fact]
    public void GetInfo_MissingFile_ReturnsNull()
        => Assert.Null(CreateService().GetInfo());

    [Fact]
    public async Task GetInfo_ExistingFile_ReturnsNameLengthAndWriteTime()
    {
        var service = CreateService();
        await using (var source = PdfStream())
        {
            await service.SaveAsync(source);
        }

        var info = service.GetInfo();

        Assert.NotNull(info);
        Assert.Equal("sample-resume.pdf", info!.FileName);
        Assert.Equal(new FileInfo(ResumePath).Length, info.Bytes);
        Assert.Equal(File.GetLastWriteTimeUtc(ResumePath), info.LastWriteUtc);
    }

    [Fact]
    public async Task SaveAsync_WritesTheExactBytesOverThePath()
    {
        var service = CreateService();
        var bytes = SamplePdfBytes();

        await service.SaveAsync(new MemoryStream(bytes));

        Assert.Equal(bytes, await File.ReadAllBytesAsync(ResumePath));
    }

    [Fact]
    public async Task SaveAsync_CreatesTheDirectoryWhenMissing()
    {
        var service = CreateService();
        Assert.False(Directory.Exists(Path.GetDirectoryName(ResumePath)));

        await using var source = PdfStream();
        await service.SaveAsync(source);

        Assert.True(File.Exists(ResumePath));
    }

    [Fact]
    public async Task SaveAsync_NonPdfStream_ThrowsInvalidDataException()
    {
        var service = CreateService();

        await using var notPdf = new MemoryStream("<html>not a pdf</html>"u8.ToArray());
        await Assert.ThrowsAsync<InvalidDataException>(() => service.SaveAsync(notPdf));
    }

    [Fact]
    public async Task SaveAsync_NonPdfStream_LeavesTheExistingFileUntouched()
    {
        var service = CreateService();
        await using (var source = PdfStream())
        {
            await service.SaveAsync(source);
        }
        var before = await File.ReadAllBytesAsync(ResumePath);

        await using var notPdf = new MemoryStream("<html>not a pdf</html>"u8.ToArray());
        await Assert.ThrowsAsync<InvalidDataException>(() => service.SaveAsync(notPdf));

        Assert.Equal(before, await File.ReadAllBytesAsync(ResumePath));
    }

    [Fact]
    public async Task SaveAsync_StreamOverTheCap_ThrowsIOException()
    {
        var service = CreateService();

        await using var oversized = new MemoryStream(new byte[ResumeRules.MaxBytes + 1]);
        await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(oversized));
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTempFileBehind()
    {
        var service = CreateService();

        await using var source = PdfStream();
        await service.SaveAsync(source);

        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(ResumePath)!, "*.tmp"));
    }

    [Fact]
    public async Task Delete_RemovesTheFile()
    {
        var service = CreateService();
        await using (var source = PdfStream())
        {
            await service.SaveAsync(source);
        }

        service.Delete();

        Assert.False(File.Exists(ResumePath));
    }

    [Fact]
    public async Task Delete_CalledTwice_DoesNotThrow()
    {
        var service = CreateService();
        await using (var source = PdfStream())
        {
            await service.SaveAsync(source);
        }

        service.Delete();
        service.Delete();
    }

    [Fact]
    public void Delete_Unconfigured_DoesNotThrow()
        => CreateService(configured: false).Delete();

    [Fact]
    public async Task SaveAsync_Unconfigured_ThrowsNamingTheEnvironmentVariable()
    {
        var service = CreateService(configured: false);

        await using var source = PdfStream();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(source));

        Assert.Contains("RESUME_FILE", ex.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
