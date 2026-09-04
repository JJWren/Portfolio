using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Portfolio.Web.Services;

/// <summary>
/// Which portrait slot an <see cref="OwnerPhotoService"/> call targets:
/// <see cref="Primary"/> is the original desk photo (OWNER_PHOTO_FILE,
/// served at /owner-photo); <see cref="Flip"/> is the second, mat photo
/// added in Unit 10 Phase 4 (OWNER_PHOTO_FLIP_FILE, served at
/// /owner-photo-flip). The hero's two-photo switch (domain-entities.md
/// section 4) renders only when both slots resolve to an existing file.
/// </summary>
public enum OwnerPhotoSlot
{
    Primary,
    Flip,
}

/// <summary>
/// The Owner Photo: up to two files (OWNER_PHOTO_FILE and, since Phase 4,
/// OWNER_PHOTO_FLIP_FILE), shown on the landing hero and served at
/// /owner-photo and /owner-photo-flip. Every member takes an
/// <see cref="OwnerPhotoSlot"/> defaulting to <see cref="OwnerPhotoSlot.Primary"/>
/// so pre-Phase-4 callers keep compiling unchanged. Swappable two ways that
/// end at the same file per slot — copy a file over it on the host, or
/// upload through the admin site-content page (write-through). Unset or
/// missing = that slot renders nothing.
/// </summary>
public class OwnerPhotoService(SiteConfig site)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxSourceDimension = 12_000;

    /// <summary>Longest-side cap for stored uploads; the hero's CSS frame does
    /// the 4:5 crop, so the image itself keeps its aspect.</summary>
    public const int MaxStoredDimension = 1600;

    /// <summary>The configured file path for a slot, or null when that slot's
    /// environment variable is unset.</summary>
    private string? PathFor(OwnerPhotoSlot slot) => slot switch
    {
        OwnerPhotoSlot.Primary => site.OwnerPhotoFile,
        OwnerPhotoSlot.Flip => site.OwnerPhotoFlipFile,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
    };

    /// <summary>The .env variable name backing a slot's path, for the
    /// "not configured" exception message.</summary>
    private static string EnvVariableFor(OwnerPhotoSlot slot) => slot switch
    {
        OwnerPhotoSlot.Primary => "OWNER_PHOTO_FILE",
        OwnerPhotoSlot.Flip => "OWNER_PHOTO_FLIP_FILE",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
    };

    /// <summary>The public route a slot's photo is served at.</summary>
    private static string RouteFor(OwnerPhotoSlot slot) => slot switch
    {
        OwnerPhotoSlot.Primary => "/owner-photo",
        OwnerPhotoSlot.Flip => "/owner-photo-flip",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
    };

    /// <summary>Whether a slot's env variable is set — the admin upload
    /// control for that slot only exists when it is, since there is nowhere
    /// to write otherwise.</summary>
    public bool IsConfigured(OwnerPhotoSlot slot = OwnerPhotoSlot.Primary) => PathFor(slot) is not null;

    /// <summary>
    /// URL for a slot's current photo, or null when unconfigured or the file
    /// is missing. The version query is the file's write time, so either
    /// write path busts browser caches on the next render.
    /// </summary>
    public string? GetVersionedUrl(OwnerPhotoSlot slot = OwnerPhotoSlot.Primary)
    {
        var path = PathFor(slot);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        return $"{RouteFor(slot)}?v={File.GetLastWriteTimeUtc(path).Ticks}";
    }

    /// <summary>
    /// Normalizes the upload (auto-orient strips EXIF, downsize, WebP) and
    /// writes it over a slot's configured path atomically so a failed encode
    /// never clobbers the current photo. Never touches the other slot's file.
    /// </summary>
    public async Task SaveAsync(
        Stream source, OwnerPhotoSlot slot = OwnerPhotoSlot.Primary, CancellationToken cancellationToken = default)
    {
        var target = PathFor(slot)
            ?? throw new InvalidOperationException($"{EnvVariableFor(slot)} is not configured.");

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

        // Per-call temp name so overlapping saves can't clobber each other's
        // half-written file; last Move wins either way.
        var temp = $"{target}.{Guid.NewGuid():N}.tmp";
        try
        {
            await image.SaveAsync(temp, new WebpEncoder(), cancellationToken);
            File.Move(temp, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                try
                {
                    File.Delete(temp);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A stranded temp file is harmless clutter.
                }
            }
        }
    }

    /// <summary>Best-effort removal of a slot's file; the hero and endpoint
    /// fall back to that slot rendering nothing.</summary>
    public void Delete(OwnerPhotoSlot slot = OwnerPhotoSlot.Primary)
    {
        var path = PathFor(slot);
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
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

        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (header.Length >= 8 && header[..8].SequenceEqual(pngSignature))
        {
            return "image/png";
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        if (header.Length >= 6
            && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)))
        {
            return "image/gif";
        }

        return null;
    }
}
