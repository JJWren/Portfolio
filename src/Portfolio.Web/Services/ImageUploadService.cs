using Microsoft.AspNetCore.Components.Forms;

namespace Portfolio.Web.Services;

/// <summary>
/// Stores uploaded images under the uploads directory (a Docker volume in
/// production) and returns the public /uploads/... path.
/// </summary>
public class ImageUploadService(IConfiguration config, IWebHostEnvironment env)
{
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg"];

    public const int MaxMegabytes = 5;
    public const long MaxBytes = MaxMegabytes * 1024L * 1024L;

    /// <summary>Comma-joined allowlist for file input accept attributes.</summary>
    public static string AcceptList { get; } = string.Join(",", AllowedExtensions);

    /// <summary>Human-readable type list for editor hints.</summary>
    public static string AllowedTypesDisplay { get; } =
        string.Join(", ", AllowedExtensions.Select(e => e.TrimStart('.').ToUpperInvariant()));

    public string RootPath =>
        config["Uploads:Path"] ?? Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Unsupported image type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        Directory.CreateDirectory(RootPath);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(RootPath, fileName);

        await using var target = File.Create(fullPath);
        await file.OpenReadStream(MaxBytes, cancellationToken).CopyToAsync(target, cancellationToken);

        return $"/uploads/{fileName}";
    }
}
