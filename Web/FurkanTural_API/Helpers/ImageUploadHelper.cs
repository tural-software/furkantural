namespace FurkanTural_API.Helpers;

/// <summary>Çözümde bu sınıfa hiçbir çağrı yoktur; yerini <see cref="FurkanTural_Application.Services.Abstract.IFileService"/> almıştır. Farkı da tam olarak budur: buradaki kayıt her dosyayı <c>images/uploads</c> altına düz biçimde yazar, yani dosya servisinin yalnızca geriye dönük uyum için tanıdığı eski düzeni üretir. Yeniden kullanılırsa bugün ayrıştırılmış olan yükleme klasörlerini tekrar tek klasöre indirir.</summary>
public static class ImageUploadHelper
{
    private static readonly HashSet<string> _allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    /// <summary>imageName yalnızca uzantısı için okunur; kaydedilen ad ondan türetilmez, kullanıcı kimliği ve yeni bir GUID ile üretilir. Dönen değer yol değil, düz dosya adıdır.</summary>
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
