using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Portfolio.Web.Services;

/// <summary>
/// Normalizes profile pictures: any reasonable image in, a small square
/// WebP out. Auto-orients (and thereby strips EXIF), center-crops, and
/// downsizes so a phone photo becomes a few-KB avatar.
/// </summary>
public class AvatarService(IConfiguration config, IWebHostEnvironment env)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int Size = 128;

    private string UploadsRoot =>
        config["Uploads:Path"] ?? Path.Combine(env.ContentRootPath, "uploads");

    private string AvatarsRoot => Path.Combine(UploadsRoot, "avatars");

    /// <summary>Saves the avatar and returns its public path; deletes the user's previous one.</summary>
    public async Task<string> SaveAsync(Stream source, string userId, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(AvatarsRoot);

        using var image = await Image.LoadAsync(source, cancellationToken);
        image.Mutate(x => x
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Size = new Size(Size, Size),
                Mode = ResizeMode.Crop,
            }));

        var fileName = $"{userId}-{DateTime.UtcNow.Ticks}.webp";
        var fullPath = Path.Combine(AvatarsRoot, fileName);
        await image.SaveAsync(fullPath, new WebpEncoder(), cancellationToken);

        Delete(userId, exceptFileName: fileName);
        return $"/uploads/avatars/{fileName}";
    }

    /// <summary>Removes the user's stored avatar file(s).</summary>
    public void Delete(string userId, string? exceptFileName = null)
    {
        if (!Directory.Exists(AvatarsRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(AvatarsRoot, $"{userId}-*.webp"))
        {
            if (Path.GetFileName(file) != exceptFileName)
            {
                File.Delete(file);
            }
        }
    }
}
