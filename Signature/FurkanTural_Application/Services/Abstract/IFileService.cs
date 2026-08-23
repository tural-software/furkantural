namespace FurkanTural_Application.Services.Abstract;

/// <summary>Yükleme dosyalarının yönetimi. <see cref="Wrappers.Result"/> zarfı kullanılmaz; uzantı ve boyut ihlalleri istisna olarak fırlar. SaveAsync veri tabanına yazılacak göreli yolu döndürür, fiziksel kök hiçbir zaman kaydedilmez (bkz. <see cref="Settings.FileStorageSettings"/>). DeleteAsync ve GetPhysicalPath eski kayıtlarla uyum için içinde bölü işareti bulunmayan değerleri düz dosya adı sayıp eski yükleme klasörüne bakar. GetPhysicalPath çözülen yolun web kökü altında kaldığını doğrular; dosya yoksa veya kökün dışına çıkıyorsa null döner.</summary>
public interface IFileService
{
    Task<string> SaveAsync(byte[] imageData, string imageName, string relatedTableName, int relatedRecordId, int userId, long? maxBytes = null);
    Task DeleteAsync(string? fileName);
    string? GetPhysicalPath(string? fileName);
}
