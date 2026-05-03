namespace FurkanTural_API.Helpers;

public static class ImageUploadHelper
{
    private static readonly HashSet<string> _allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    /// <param name="imageData">Ham görsel verisi (byte dizisi).</param>
    /// <param name="imageName">Orijinal dosya adı — sadece uzantıyı belirlemek için kullanılır. Örn: "photo.jpg"</param>
    /// <param name="userId">Görseli yükleyen kullanıcının ID'si.</param>
    /// <param name="wwwrootPath">Uygulamanın wwwroot dizin yolu.</param>
    /// <returns>Kaydedilen dosyanın adı (örn. userid-1-{guid}.jpg).</returns>
    public static async Task<string> SaveAsync(byte[] imageData, string imageName, int userId, string wwwrootPath)
    {
        var extension = Path.GetExtension(imageName);
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Desteklenmeyen dosya uzantısı: {extension}. İzin verilenler: jpg, jpeg, png, webp, gif");

        var uploadsPath = Path.Combine(wwwrootPath, "images", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"userid-{userId}-{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await File.WriteAllBytesAsync(filePath, imageData);

        return fileName;
    }
}