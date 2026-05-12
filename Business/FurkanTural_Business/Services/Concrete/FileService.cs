using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;

namespace FurkanTural_Business.Services.Concrete;

public sealed class FileService : IFileService
{
    private static readonly HashSet<string> _allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly string _uploadsPath;

    public FileService(FileStorageSettings settings)
    {
        _uploadsPath = settings.UploadsPath;
    }

    public async Task<string> SaveAsync(byte[] imageData, string imageName, string relatedTableName, int relatedRecordId, int userId)
    {
        var extension = Path.GetExtension(imageName);
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException(
                $"Desteklenmeyen dosya uzantısı: {extension}. İzin verilenler: jpg, jpeg, png, webp, gif");

        Directory.CreateDirectory(_uploadsPath);

        var fileName = $"{relatedTableName}-{relatedRecordId}-user-{userId}-{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMdd}{extension}";
        var filePath = Path.Combine(_uploadsPath, fileName);

        await File.WriteAllBytesAsync(filePath, imageData);

        return fileName;
    }

    public Task DeleteAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.CompletedTask;

        var filePath = Path.Combine(_uploadsPath, fileName);
        if (!File.Exists(filePath))
            return Task.CompletedTask;

        File.Delete(filePath);
        return Task.CompletedTask;
    }
}
