using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ImageUploadServiceTests
{
    [Fact]
    public void AcceptList_IsCommaJoinedDotExtensions()
    {
        var entries = ImageUploadService.AcceptList.Split(',');

        Assert.All(entries, e => Assert.StartsWith(".", e));
        Assert.Contains(".png", entries);
        Assert.Contains(".svg", entries);
    }

    [Fact]
    public void AllowedTypesDisplay_IsUppercaseWithoutDots()
    {
        Assert.Equal("PNG, JPG, JPEG, WEBP, GIF, SVG", ImageUploadService.AllowedTypesDisplay);
    }

    [Fact]
    public void MaxBytes_DerivesFromMaxMegabytes()
    {
        Assert.Equal(ImageUploadService.MaxMegabytes * 1024L * 1024L, ImageUploadService.MaxBytes);
    }
}
