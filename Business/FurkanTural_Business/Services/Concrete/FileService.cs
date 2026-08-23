using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Göreli yol <c>modül/ortamTürü/dosyaAdı</c> biçiminde kurulur. Modül klasörü relatedTableName'e göre seçilir; eşleşmeyen ad "misc" altına düşer, hata vermez. Ortam türü uzantıdan çıkarılır ve <c>.webm</c> ile <c>.ogg</c> ses sayılır: bu uzantılar hem sesle hem videoyla gelebilir, ama buradaki üreticileri tarayıcının ses kaydı olduğu için ses kümesi önceliklidir.<para>Dosya adı her yüklemede benzersiz üretilir; aynı ada sahip iki yükleme birbirinin üzerine yazmaz.</para></summary>
public sealed class FileService : IFileService
{
    private static readonly HashSet<string> _imageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private static readonly HashSet<string> _audioExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".webm", ".mp3", ".m4a", ".ogg", ".wav" };

    private static readonly HashSet<string> _videoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".m4v", ".avi", ".mkv" };

    private static readonly Dictionary<string, string> _moduleFolders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["User"] = "users",
            ["Project"] = "projects",
            ["Music"] = "musics",
            ["ChatMessage"] = "chats",
            ["Blog"] = "blogs",
        };

    private const string LegacyFolder = "images/uploads";

    private readonly string _webRootPath;
    private readonly IClock _clock;

    public FileService(FileStorageSettings settings, IClock clock)
    {
        _webRootPath = settings.WebRootPath;
        _clock = clock;
    }

    public async Task<string> SaveAsync(byte[] imageData, string imageName, string relatedTableName, int relatedRecordId, int userId, long? maxBytes = null)
    {
        var extension = Path.GetExtension(imageName);
        var isImage = _imageExtensions.Contains(extension);
        var isAudio = _audioExtensions.Contains(extension);
        var isVideo = _videoExtensions.Contains(extension);

        if (!isImage && !isAudio && !isVideo)
            throw new InvalidOperationException(
                $"Desteklenmeyen dosya uzantısı: {extension}. İzin verilenler: jpg, jpeg, png, webp, gif, webm, mp3, m4a, ogg, wav, mp4, mov, m4v, avi, mkv");

        if (maxBytes is > 0 && imageData.LongLength > maxBytes.Value)
            throw new InvalidOperationException(
                $"Dosya boyutu sınırı aşıldı. En fazla {maxBytes.Value / (1024 * 1024)} MB yükleyebilirsiniz.");

        var module = _moduleFolders.TryGetValue(relatedTableName, out var folder) ? folder : "misc";
        var mediaType = isImage ? "images" : isAudio ? "voices" : "videos";

        var targetDir = Path.Combine(_webRootPath, module, mediaType);
        Directory.CreateDirectory(targetDir);

        var fileName = $"{relatedTableName}-{relatedRecordId}-user-{userId}-{Guid.NewGuid():N}-{_clock.UtcNow:yyyyMMdd}{extension}";
        var filePath = Path.Combine(targetDir, fileName);

        await File.WriteAllBytesAsync(filePath, imageData);

        return $"{module}/{mediaType}/{fileName}";
    }

    public Task DeleteAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.CompletedTask;

        var relative = fileName.Contains('/') ? fileName : $"{LegacyFolder}/{fileName}";
        var filePath = Path.Combine(_webRootPath, relative.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath))
            return Task.CompletedTask;

        File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string? GetPhysicalPath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var relative = fileName.Contains('/') ? fileName : $"{LegacyFolder}/{fileName}";
        var filePath = Path.GetFullPath(Path.Combine(_webRootPath, relative.Replace('/', Path.DirectorySeparatorChar)));

        var root = Path.GetFullPath(_webRootPath);
        if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(filePath) ? filePath : null;
    }
}
