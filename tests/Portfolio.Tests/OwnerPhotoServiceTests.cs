using Portfolio.Web.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Portfolio.Tests;

public class OwnerPhotoServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"owner-photo-tests-{Guid.NewGuid():N}");

    private string PhotoPath => Path.Combine(_tempDir, "photo", "owner-photo.webp");

    private OwnerPhotoService CreateService(bool configured = true)
        => new(BuildConfig(configured ? PhotoPath : null));

    private static SiteConfig BuildConfig(string? ownerPhotoFile)
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
            OwnerPhotoFile: ownerPhotoFile);

    private static MemoryStream PngImage(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(150, 60, 64));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void GetVersionedUrl_Unconfigured_ReturnsNull()
        => Assert.Null(CreateService(configured: false).GetVersionedUrl());

    [Fact]
    public void GetVersionedUrl_MissingFile_ReturnsNull()
        => Assert.Null(CreateService().GetVersionedUrl());

    [Fact]
    public async Task GetVersionedUrl_ExistingFile_CarriesTheWriteTimeTicks()
    {
        var service = CreateService();
        await using (var source = PngImage(100, 100))
        {
            await service.SaveAsync(source);
        }

        var url = service.GetVersionedUrl();

        Assert.NotNull(url);
        Assert.Equal($"/owner-photo?v={File.GetLastWriteTimeUtc(PhotoPath).Ticks}", url);
    }

    [Fact]
    public async Task SaveAsync_WritesWebpOverTheConfiguredPath()
    {
        var service = CreateService();

        await using var source = PngImage(1200, 800);
        await service.SaveAsync(source);

        using var saved = Image.Load(PhotoPath);
        Assert.IsType<WebpFormat>(saved.Metadata.DecodedImageFormat);
        // Under the cap: dimensions and aspect are preserved (the CSS frame crops).
        Assert.Equal(1200, saved.Width);
        Assert.Equal(800, saved.Height);
        Assert.False(File.Exists($"{PhotoPath}.tmp"));
    }

    [Fact]
    public async Task SaveAsync_ShrinksTheLongestSideToTheCap()
    {
        var service = CreateService();

        await using var source = PngImage(3200, 1600);
        await service.SaveAsync(source);

        using var saved = Image.Load(PhotoPath);
        Assert.Equal(OwnerPhotoService.MaxStoredDimension, saved.Width);
        Assert.Equal(OwnerPhotoService.MaxStoredDimension / 2, saved.Height);
    }

    [Fact]
    public async Task SaveAsync_Unconfigured_Throws()
    {
        var service = CreateService(configured: false);

        await using var source = PngImage(100, 100);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(source));
    }

    [Fact]
    public async Task SaveAsync_RejectsStreamsOverTheSizeLimit()
    {
        var service = CreateService();

        await using var oversized = new MemoryStream(new byte[OwnerPhotoService.MaxBytes + 1]);
        await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(oversized));
    }

    [Fact]
    public async Task Delete_RemovesThePhoto()
    {
        var service = CreateService();
        await using (var source = PngImage(100, 100))
        {
            await service.SaveAsync(source);
        }

        service.Delete();

        Assert.False(File.Exists(PhotoPath));
        Assert.Null(service.GetVersionedUrl());
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A }, "image/png")]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, "image/gif")]
    public void SniffContentType_KnownSignatures_AreIdentified(byte[] header, string expected)
        => Assert.Equal(expected, OwnerPhotoService.SniffContentType(header));

    [Fact]
    public void SniffContentType_Webp_NeedsRiffAndWebpMarkers()
    {
        var header = "RIFF\0\0\0\0WEBP"u8.ToArray();

        Assert.Equal("image/webp", OwnerPhotoService.SniffContentType(header));
    }

    [Fact]
    public void SniffContentType_UnknownOrShortHeader_ReturnsNull()
    {
        Assert.Null(OwnerPhotoService.SniffContentType("<svg xmlns=48"u8.ToArray()));
        Assert.Null(OwnerPhotoService.SniffContentType([0xFF]));
        Assert.Null(OwnerPhotoService.SniffContentType([]));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
