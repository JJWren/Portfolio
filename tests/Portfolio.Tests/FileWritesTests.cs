using Portfolio.Web.Services;

namespace Portfolio.Tests;

/// <summary>
/// The shared atomic write and best-effort delete behind the owner photos and
/// the résumé. The service fixtures (OwnerPhotoServiceTests, ResumeServiceTests)
/// pin the end-to-end behaviour; these pin the helper on its own.
/// </summary>
public class FileWritesTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"file-writes-tests-{Guid.NewGuid():N}");

    private sealed class WriteFailedException : Exception
    {
    }

    [Fact]
    public async Task WriteAtomicallyAsync_CreatesTheDirectoryAndWritesTheBytes()
    {
        var target = Path.Combine(_tempDir, "nested", "target.bin");

        await FileWrites.WriteAtomicallyAsync(
            target, (temp, ct) => File.WriteAllBytesAsync(temp, [1, 2, 3], ct));

        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(target));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)!, "*.tmp"));
    }

    [Fact]
    public async Task WriteAtomicallyAsync_ReplacesAnExistingFile()
    {
        Directory.CreateDirectory(_tempDir);
        var target = Path.Combine(_tempDir, "target.bin");
        await File.WriteAllBytesAsync(target, [9, 9]);

        await FileWrites.WriteAtomicallyAsync(
            target, (temp, ct) => File.WriteAllBytesAsync(temp, [4, 5], ct));

        Assert.Equal(new byte[] { 4, 5 }, await File.ReadAllBytesAsync(target));
        Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
    }

    [Fact]
    public async Task WriteAtomicallyAsync_FailedWriteLeavesTheTargetAndNoTempFile()
    {
        Directory.CreateDirectory(_tempDir);
        var target = Path.Combine(_tempDir, "target.bin");
        await File.WriteAllBytesAsync(target, [9, 9]);

        await Assert.ThrowsAsync<WriteFailedException>(() => FileWrites.WriteAtomicallyAsync(
            target,
            async (temp, ct) =>
            {
                // A half-written temp file, then the failure.
                await File.WriteAllBytesAsync(temp, [1], ct);
                throw new WriteFailedException();
            }));

        Assert.Equal(new byte[] { 9, 9 }, await File.ReadAllBytesAsync(target));
        Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
    }

    [Fact]
    public async Task DeleteBestEffort_RemovesAnExistingFile()
    {
        Directory.CreateDirectory(_tempDir);
        var target = Path.Combine(_tempDir, "target.bin");
        await File.WriteAllBytesAsync(target, [1]);

        FileWrites.DeleteBestEffort(target);

        Assert.False(File.Exists(target));
    }

    [Fact]
    public void DeleteBestEffort_MissingFile_DoesNotThrow()
    {
        var exception = Record.Exception(
            () => FileWrites.DeleteBestEffort(Path.Combine(_tempDir, "missing.bin")));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
