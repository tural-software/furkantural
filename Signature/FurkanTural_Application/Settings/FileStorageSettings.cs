namespace FurkanTural_Application.Settings;

/// <summary>FileService'in yükleme kökü. Adı Settings olsa da appsettings'ten okunmaz; API açılışta barındırma ortamının wwwroot yolunu buraya yazıp singleton olarak kaydeder, böylece Business katmanı web host'tan doğrudan değer çekmez. FileService bu kökün altına modül/medya-türü klasörlemesiyle yazar ve veri tabanına yalnızca o göreli yolu döndürür; kök yolun kendisi hiçbir zaman kaydedilmez, yani dizin taşındığında mevcut kayıtlar bozulmaz.</summary>
public sealed class FileStorageSettings
{
    public string WebRootPath { get; set; } = string.Empty;
}
