using Microsoft.AspNetCore.Http;

namespace Travyle.Api.Services;

/// <summary>
/// Component 4 (Support &amp; Quality): storage for ticket evidence photos.
/// Implementations: local disk (default) and, later, Supabase Storage.
/// </summary>
public interface ISupportAttachmentStorage
{
    /// <summary>Validates and stores the image, returning a URL the clients can load.</summary>
    /// <exception cref="ArgumentException">The file failed validation (type, size, content).</exception>
    Task<string> SaveAsync(IFormFile file, HttpRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Deterministic validation for support evidence photos (no trust in client-supplied metadata).</summary>
public static class SupportAttachmentValidator
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Returns the normalized, allow-listed extension (".jpg" when absent) or throws.</summary>
    public static async Task<string> ValidateAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded or file is empty.");
        }

        if (file.Length > MaxBytes)
        {
            throw new ArgumentException($"File is too large. Maximum allowed size is {MaxBytes / (1024 * 1024)} MB.");
        }

        var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg"; // camera captures may omit the extension

        if (!AllowedExtensions.Contains(ext))
        {
            throw new ArgumentException("Unsupported file type. Only JPG, PNG and WEBP images are allowed.");
        }

        // Check the real content (magic bytes) so a renamed non-image cannot be stored.
        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        }

        if (!LooksLikeImage(header, read))
        {
            throw new ArgumentException("The uploaded file is not a valid JPG, PNG or WEBP image.");
        }

        return ext;
    }

    private static bool LooksLikeImage(byte[] h, int length)
    {
        if (length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true; // JPEG
        if (length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 &&
            h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return true; // PNG
        if (length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
            h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return true; // WEBP (RIFF....WEBP)
        return false;
    }
}

/// <summary>Stores validated images under wwwroot/support-attachments with a server-generated file name.</summary>
public class LocalSupportAttachmentStorage : ISupportAttachmentStorage
{
    private readonly string _rootPath;

    public LocalSupportAttachmentStorage(string? rootPath = null)
    {
        _rootPath = rootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task<string> SaveAsync(IFormFile file, HttpRequest request, CancellationToken cancellationToken = default)
    {
        var ext = await SupportAttachmentValidator.ValidateAsync(file, cancellationToken);

        var attachmentsPath = Path.Combine(_rootPath, "support-attachments");
        Directory.CreateDirectory(attachmentsPath);

        // Never use the client file name: server-generated name prevents path traversal / overwrites.
        var fileName = $"evidence_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(attachmentsPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var baseUrl = $"{request.Scheme}://{request.Host.Value}";
        return $"{baseUrl}/support-attachments/{fileName}";
    }
}
