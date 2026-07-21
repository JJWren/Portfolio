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

    /// <summary>Decoded-dimension cap: rejects decompression bombs before full decode.</summary>
    public const int MaxSourceDimension = 12_000;

    private string UploadsRoot =>
        config["Uploads:Path"] ?? Path.Combine(env.ContentRootPath, "uploads");

    private string AvatarsRoot => Path.Combine(UploadsRoot, "avatars");

    /// <summary>
    /// Saves the avatar and returns its public path. The caller is responsible
    /// for deleting superseded files once its own bookkeeping succeeds (see
    /// <see cref="Delete"/>), so a failed follow-up never orphans the DB.
    /// </summary>
    public async Task<string> SaveAsync(Stream source, string userId, CancellationToken cancellationToken = default)
    {
        // Enforce the size limit here too — not every caller is a Blazor
        // InputFile stream with its own cap.
        await using var buffered = await BufferWithLimitAsync(source, cancellationToken);

        var info = await Image.IdentifyAsync(buffered, cancellationToken);
        if (info.Width > MaxSourceDimension || info.Height > MaxSourceDimension)
        {
            throw new IOException(
                $"Images larger than {MaxSourceDimension}px on a side aren't supported.");
        }

        buffered.Position = 0;
        Directory.CreateDirectory(AvatarsRoot);

        using var image = await Image.LoadAsync(buffered, cancellationToken);
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
        return $"/uploads/avatars/{fileName}";
    }

    /// <summary>Best-effort removal of the user's stored avatar file(s).</summary>
    public void Delete(string userId, string? exceptFileName = null)
    {
        if (!Directory.Exists(AvatarsRoot))
        {
            return;
        }

        // Filter by strict prefix rather than putting userId into a search
        // pattern, and never let filesystem hiccups break the request.
        var prefix = $"{userId}-";
        foreach (var file in Directory.EnumerateFiles(AvatarsRoot, "*.webp"))
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name == exceptFileName)
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Orphaned files are harmless; the next replace retries.
            }
        }
    }

    private static async Task<MemoryStream> BufferWithLimitAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffered = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffered.Length + read > MaxBytes)
            {
                await buffered.DisposeAsync();
                throw new IOException($"Avatar uploads are limited to {MaxBytes / (1024 * 1024)} MB.");
            }

            buffered.Write(chunk, 0, read);
        }

        buffered.Position = 0;
        return buffered;
    }
}
