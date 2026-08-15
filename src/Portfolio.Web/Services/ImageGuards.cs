using SixLabors.ImageSharp;

namespace Portfolio.Web.Services;

/// <summary>Upload guards shared by the image-processing services.</summary>
public static class ImageGuards
{
    /// <summary>
    /// Buffers the stream, failing once it exceeds the limit — not every
    /// caller is a Blazor InputFile stream with its own cap.
    /// </summary>
    public static async Task<MemoryStream> BufferWithLimitAsync(
        Stream source, long maxBytes, string what, CancellationToken cancellationToken)
    {
        var buffered = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffered.Length + read > maxBytes)
            {
                await buffered.DisposeAsync();
                throw new IOException($"{what} uploads are limited to {maxBytes / (1024 * 1024)} MB.");
            }

            buffered.Write(chunk, 0, read);
        }

        buffered.Position = 0;
        return buffered;
    }

    /// <summary>Decoded-dimension cap: rejects decompression bombs before full decode.</summary>
    public static void EnsureDecodedSizeAllowed(ImageInfo info, int maxDimension)
    {
        if (info.Width > maxDimension || info.Height > maxDimension)
        {
            throw new IOException($"Images larger than {maxDimension}px on a side aren't supported.");
        }
    }
}
