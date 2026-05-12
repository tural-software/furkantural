namespace FurkanTural_Application.Services.Abstract;

public interface IFileService
{
    /// <summary>
    /// Görsel dosyasını fiziksel olarak kaydeder ve oluşturulan dosya adını döner.
    /// Dosya adı formatı: {relatedTableName}-{relatedRecordId}-user-{userId}-{Guid}-{yyyyMMdd}{ext}
    /// </summary>
    /// <exception cref="InvalidOperationException">Desteklenmeyen dosya uzantısı.</exception>
    /// <exception cref="IOException">Dosya yazma hatası.</exception>
    Task<string> SaveAsync(byte[] imageData, string imageName, string relatedTableName, int relatedRecordId, int userId);

    /// <summary>
    /// Görsel dosyasını fiziksel olarak siler.
    /// fileName null/boş ya da dosya mevcut değilse sessizce döner.
    /// </summary>
    /// <exception cref="IOException">Dosya silme hatası (dosya mevcutsa fakat silinemediyse).</exception>
    Task DeleteAsync(string? fileName);
}
