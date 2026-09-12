namespace Portfolio.Web.Services;

/// <summary>
/// File-writing mechanics shared by the owner-supplied files the site serves
/// (the owner photos and the résumé): an atomic replace through a per-call
/// temp file, and a best-effort delete. Both exist so a failed write never
/// clobbers the file visitors are being served.
/// </summary>
public static class FileWrites
{
    /// <summary>
    /// Writes through a per-call temp file beside the target, then moves it
    /// over the target with overwrite, so the target is always either the old
    /// file or the complete new one, never a half-written one. The directory
    /// is created when missing; a stranded temp file is removed on the way
    /// out, whether the write succeeded or threw.
    /// </summary>
    public static async Task WriteAtomicallyAsync(
        string target,
        Func<string, CancellationToken, Task> writeToTemp,
        CancellationToken cancellationToken = default)
    {
        if (Path.GetDirectoryName(target) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        // Per-call temp name so overlapping saves can't clobber each other's
        // half-written file; last Move wins either way.
        var temp = $"{target}.{Guid.NewGuid():N}.tmp";
        try
        {
            await writeToTemp(temp, cancellationToken);
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

    /// <summary>
    /// Best-effort removal: a file that cannot be deleted right now just keeps
    /// being served until the next replace retries, which beats surfacing an
    /// error for a file the site can live without.
    /// </summary>
    public static void DeleteBestEffort(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A stuck file just keeps serving the old content; the next replace retries.
        }
    }
}
