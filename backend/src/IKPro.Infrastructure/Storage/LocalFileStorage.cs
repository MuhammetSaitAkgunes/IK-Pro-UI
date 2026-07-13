using IKPro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IKPro.Infrastructure.Storage;

/// <summary>
/// Yerel disk dosya deposu. Kök dizin "Storage:Root" ayarından gelir (göreli ise
/// çalışma dizinine göre). Dosyalar {kategori}/{guid}{uzantı} olarak benzersiz
/// adlandırılır; orijinal ad DB'de meta olarak tutulur.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:Root"] ?? Path.Combine("App_Data", "storage");
        _root = Path.GetFullPath(configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content, string fileName, string category, CancellationToken cancellationToken)
    {
        var safeCategory = SanitizeSegment(category);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var relativePath = Path.Combine(safeCategory, $"{Guid.NewGuid():N}{extension}");

        var fullPath = ResolveSafe(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);

        // Yol ayracı platformdan bağımsız saklanır.
        return new StoredFile(relativePath.Replace('\\', '/'), fileName, target.Length);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = ResolveSafe(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Depodaki dosya bulunamadı.", path);
        }

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = ResolveSafe(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Depo kökü dışına çıkan yolları (../ vb.) reddeder.</summary>
    private string ResolveSafe(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Geçersiz dosya yolu.");
        }

        return fullPath;
    }

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(segment.Where(c => !invalid.Contains(c) && c != '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "misc" : cleaned;
    }
}
