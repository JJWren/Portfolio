namespace Portfolio.Web.Services;

/// <summary>A résumé file's metadata for the admin editor: the configured
/// path's filename, its size in bytes, and its last-write time in UTC.</summary>
public record ResumeInfo(string FileName, long Bytes, DateTime LastWriteUtc);

/// <summary>
/// The Résumé: one PDF at the RESUME_FILE path, served at /resume. Swappable
/// two ways that end at the same file — copy a file over it on the host, or
/// upload through the admin site-content page (write-through). Unset or
/// missing = no résumé link renders anywhere.
/// </summary>
public class ResumeService(SiteConfig site)
{
    /// <summary>Whether RESUME_FILE is set — the admin upload control only
    /// exists when it is, since there is nowhere to write otherwise.</summary>
    public bool IsConfigured => site.ResumeFile is not null;

    /// <summary>Whether the configured path is set and the file exists — the
    /// one availability rule shared by the endpoint and both links. One stat
    /// call per render, deliberately uncached: the file may be replaced on the
    /// host at any moment (write-through), and a stat costs microseconds.</summary>
    public bool IsAvailable => site.ResumeFile is { } path && File.Exists(path);

    /// <summary>The current file's name, size and write time, or null when unavailable.</summary>
    public ResumeInfo? GetInfo()
    {
        if (site.ResumeFile is not { } path || !File.Exists(path))
        {
            return null;
        }

        var info = new FileInfo(path);
        return new ResumeInfo(info.Name, info.Length, info.LastWriteTimeUtc);
    }

    /// <summary>
    /// Writes the upload over the configured path atomically so a failed or
    /// oversized upload never touches the current file: buffers with the
    /// shared size guard, rejects anything that isn't a PDF by its leading
    /// bytes, then writes a per-call temp file beside the target and moves it
    /// over with overwrite, cleaning up a stranded temp file either way.
    /// </summary>
    public async Task SaveAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var target = site.ResumeFile
            ?? throw new InvalidOperationException("RESUME_FILE is not configured.");

        await using var buffered = await ImageGuards.BufferWithLimitAsync(
            source, ResumeRules.MaxBytes, "Résumé", cancellationToken);

        var header = new byte[5];
        var read = await buffered.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (!ResumeRules.IsPdf(header.AsSpan(0, read)))
        {
            throw new InvalidDataException("That file isn't a PDF.");
        }

        buffered.Position = 0;
        if (Path.GetDirectoryName(target) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        // Per-call temp name so overlapping saves can't clobber each other's
        // half-written file; last Move wins either way.
        var temp = $"{target}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var file = File.Create(temp))
            {
                await buffered.CopyToAsync(file, cancellationToken);
            }

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

    /// <summary>Best-effort removal of the file; the endpoint and links fall
    /// back to no résumé being available.</summary>
    public void Delete()
    {
        if (site.ResumeFile is not { } path)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A stuck file just keeps serving the old résumé; the next replace retries.
        }
    }
}
