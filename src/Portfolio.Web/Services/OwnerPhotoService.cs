using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Portfolio.Web.Services;

/// <summary>
/// The Owner Photo: a single file at OWNER_PHOTO_FILE, shown on the landing
/// hero and served at /owner-photo. Swappable two ways that end at the same
/// file — copy a file over it on the host, or upload through the admin
/// site-content page (write-through). Unset or missing = the hero renders
/// photo-less.
/// </summary>
public class OwnerPhotoService(SiteConfig site)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxSourceDimension = 12_000;

    /// <summary>Longest-side cap for stored uploads; the hero's CSS frame does
    /// the 4:5 crop, so the image itself keeps its aspect.</summary>
    public const int MaxStoredDimension = 1600;

    /// <summary>Whether OWNER_PHOTO_FILE is set — the admin upload control only
    /// exists when it is, since there is nowhere to write otherwise.</summary>
    public bool IsConfigured => site.OwnerPhotoFile is not null;

    /// <summary>
    /// URL for the current photo, or null when unconfigured or the file is
    /// missing. The version query is the file's write time, so either write
    /// path busts browser caches on the next render.
    /// </summary>
    public string? GetVersionedUrl()
    {
        var path = site.OwnerPhotoFile;
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        return $"/owner-photo?v={File.GetLastWriteTimeUtc(path).Ticks}";
    }

    /// <summary>
    /// Normalizes the upload (auto-orient strips EXIF, downsize, WebP) and
    /// writes it over the OWNER_PHOTO_FILE path atomically so a failed encode
    /// never clobbers the current photo.
    /// </summary>
    public async Task SaveAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var target = site.OwnerPhotoFile
            ?? throw new InvalidOperationException("OWNER_PHOTO_FILE is not configured.");

        await using var buffered = await ImageGuards.BufferWithLimitAsync(
            source, MaxBytes, "Photo", cancellationToken);

        var info = await Image.IdentifyAsync(buffered, cancellationToken);
        ImageGuards.EnsureDecodedSizeAllowed(info, MaxSourceDimension);

        buffered.Position = 0;
        if (Path.GetDirectoryName(target) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        using var image = await Image.LoadAsync(buffered, cancellationToken);
        image.Mutate(x => x.AutoOrient());
        if (image.Width > MaxStoredDimension || image.Height > MaxStoredDimension)
        {
            // Shrink-only: ResizeMode.Max would upscale a smaller source.
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxStoredDimension, MaxStoredDimension),
                Mode = ResizeMode.Max,
            }));
        }

        var temp = $"{target}.tmp";
        await image.SaveAsync(temp, new WebpEncoder(), cancellationToken);
        File.Move(temp, target, overwrite: true);
    }

    /// <summary>Best-effort removal; the hero and endpoint fall back to photo-less.</summary>
    public void Delete()
    {
        if (site.OwnerPhotoFile is null)
        {
            return;
        }

        try
        {
            File.Delete(site.OwnerPhotoFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A stuck file just keeps serving the old photo; the next replace retries.
        }
    }

    /// <summary>
    /// Content type from magic bytes — the host-copy workflow means the file
    /// extension can't be trusted. Null means "not a servable image".
    /// </summary>
    public static string? SniffContentType(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (header.Length >= 4 && header[0] == 0x89 && header[1] == (byte)'P'
            && header[2] == (byte)'N' && header[3] == (byte)'G')
        {
            return "image/png";
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        if (header.Length >= 4 && header[..4].SequenceEqual("GIF8"u8))
        {
            return "image/gif";
        }

        return null;
    }
}
