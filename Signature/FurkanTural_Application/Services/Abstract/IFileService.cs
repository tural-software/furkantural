namespace FurkanTural_Application.Services.Abstract;

public interface IFileService
{
    /// <summary>
    /// Dosyayı modül + medya-türü klasörüne kaydeder ve <b>göreli yolu</b> döner
    /// (ör. <c>chats/images/&lt;dosya&gt;</c>). Modül <paramref name="relatedTableName"/>'den,
    /// medya-türü (images/voices/videos) uzantıdan türetilir.
    /// Dosya adı formatı: {relatedTableName}-{relatedRecordId}-user-{userId}-{Guid}-{yyyyMMdd}{ext}
    /// </summary>
    /// <param name="maxBytes">Verilirse ve veri bu boyutu aşarsa kayıt reddedilir (null = sınırsız).</param>
    /// <exception cref="InvalidOperationException">Desteklenmeyen uzantı veya boyut sınırı aşımı.</exception>
    /// <exception cref="IOException">Dosya yazma hatası.</exception>
    Task<string> SaveAsync(byte[] imageData, string imageName, string relatedTableName, int relatedRecordId, int userId, long? maxBytes = null);

    /// <summary>
    /// Görsel dosyasını fiziksel olarak siler.
    /// fileName null/boş ya da dosya mevcut değilse sessizce döner.
    /// </summary>
    /// <exception cref="IOException">Dosya silme hatası (dosya mevcutsa fakat silinemediyse).</exception>
    Task DeleteAsync(string? fileName);
}
