using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Portfolio.Web.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Portfolio.Tests;

public class AvatarServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"avatar-tests-{Guid.NewGuid():N}");

    private AvatarService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Path"] = _tempDir })
            .Build();
        return new AvatarService(config, new FakeEnvironment());
    }

    private static MemoryStream PngImage(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(150, 60, 64));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task SaveAsync_ProducesSquareWebpAtTargetSize()
    {
        var service = CreateService();

        await using var source = PngImage(1200, 800);
        var path = await service.SaveAsync(source, "user-1");

        Assert.StartsWith("/uploads/avatars/user-1-", path);
        Assert.EndsWith(".webp", path);

        var file = Path.Combine(_tempDir, "avatars", Path.GetFileName(path));
        using var saved = Image.Load(file);
        Assert.Equal(AvatarService.Size, saved.Width);
        Assert.Equal(AvatarService.Size, saved.Height);
        Assert.IsType<WebpFormat>(saved.Metadata.DecodedImageFormat);
    }

    [Fact]
    public async Task DeleteWithException_RemovesAllButTheKeptFile()
    {
        // SaveAsync intentionally keeps old files; the caller deletes them
        // after its own bookkeeping succeeds (see ProfileService.SetAvatarAsync).
        var service = CreateService();

        await using (var first = PngImage(300, 300))
        {
            await service.SaveAsync(first, "user-2");
        }

        await using var second = PngImage(300, 300);
        var newPath = await service.SaveAsync(second, "user-2");

        service.Delete("user-2", exceptFileName: Path.GetFileName(newPath));

        var files = Directory.GetFiles(Path.Combine(_tempDir, "avatars"), "*.webp")
            .Where(f => Path.GetFileName(f).StartsWith("user-2-", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(files);
        Assert.Equal(Path.GetFileName(newPath), Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task SaveAsync_RejectsStreamsOverTheSizeLimit()
    {
        var service = CreateService();

        await using var oversized = new MemoryStream(new byte[AvatarService.MaxBytes + 1]);
        await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(oversized, "user-5"));
    }

    [Fact]
    public async Task SaveAsync_RejectsNonImageData()
    {
        var service = CreateService();

        await using var garbage = new MemoryStream("not an image"u8.ToArray());
        await Assert.ThrowsAnyAsync<Exception>(() => service.SaveAsync(garbage, "user-3"));
    }

    [Fact]
    public async Task Delete_RemovesStoredAvatars()
    {
        var service = CreateService();

        await using (var source = PngImage(200, 200))
        {
            await service.SaveAsync(source, "user-4");
        }

        service.Delete("user-4");

        Assert.Empty(Directory.GetFiles(Path.Combine(_tempDir, "avatars"), "user-4-*.webp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }
}
