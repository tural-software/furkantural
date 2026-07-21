using Microsoft.AspNetCore.DataProtection;

namespace FurkanTural_Chat;

/// <summary>
/// Data Protection anahtarlarının (session cookie'sini ve antiforgery token'ını şifreleyen
/// anahtarlar) yaşadığı yeri çözer.
///
/// SORUN: IIS altında varsayılan davranışta anahtarlar app pool profili yüklü değilse YALNIZCA
/// BELLEKTE tutulur → her geri dönüşümde ve her publish'te sıfırlanır → açık oturumlar toptan
/// düşer, kullanıcılar "antiforgery token geçersiz" hatası alır. Chat'te bu, BFF session'ındaki
/// JWT'nin de kaybolması demektir → kullanıcı sohbetin ortasında login'e düşer.
///
/// KISIT — PAYLAŞIMLI HOSTING: Uygulama bir hosting firmasının sunucusunda çalışır. App pool
/// kimliği genellikle yalnızca sitenin kendi klasör ağacına erişebilir; %ProgramData% gibi
/// makine geneli yollar kapalı olabilir. Bu yüzden tek bir yola bel bağlamak yerine sırayla
/// aday yollar denenir ve GERÇEK bir yazma testinden geçen ilki kullanılır.
///
/// NEDEN FAIL-FAST DEĞİL: Hiçbir yol yazılabilir değilse uygulamayı durdurmak, düzeltmeye
/// çalıştığımız sorundan (oturum düşmesi) daha kötü bir sonuç üretirdi — site komple kapanırdı.
/// Bunun yerine bellek-içi davranışa (eski hâl) düşülür ve durum yüksek sesle loglanır.
///
/// NEDEN DB DEĞİL: <c>PersistKeysToDbContext</c> ideal olurdu ama mimari kural gereği sunum
/// projeleri EF'e/DB'ye dokunamaz (her şey API üzerinden gider).
///
/// ⚠️ İKİZ DOSYA: Aynı mantığın bir kopyası
/// <c>Presentation/FurkanTural_Admin/PersistentDataProtectionExtensions.cs</c> içindedir.
/// Birini değiştirirken diğerini de güncelleyin.
/// </summary>
public static class PersistentDataProtectionExtensions
{
    /// <summary>
    /// Anahtar deposunu yapılandırır ve sonucu döndürür (uygulama ayağa kalktıktan sonra
    /// <see cref="LogDataProtectionStatus"/> ile loglanmak üzere).
    /// </summary>
    /// <param name="services">Yapılandırılacak servis koleksiyonu.</param>
    /// <param name="configuration">DataProtection ayarlarının okunduğu yapılandırma.</param>
    /// <param name="environment">İçerik kökü (App_Data adayı) için ortam bilgisi.</param>
    /// <param name="applicationName">
    /// Anahtar halkasını izole eden sabit ad. İçerik kökü yolundan TÜRETİLMEMELİ; aksi halde
    /// uygulama başka bir klasöre taşındığında mevcut anahtarlar okunamaz hale gelir.
    /// </param>
    public static DataProtectionSetupResult AddPersistentDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string applicationName)
    {
        var result = DataProtectionPathResolver.Resolve(configuration, environment, applicationName);

        if (result.KeyPath is null)
        {
            // Hiçbir aday yazılabilir değil. Varsayılan (bellek-içi) davranışla devam ediyoruz;
            // durum LogDataProtectionStatus ile Error seviyesinde loglanacak.
            return result;
        }

        var builder = services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(result.KeyPath))
            .SetApplicationName(applicationName);

        // DPAPI VARSAYILAN OLARAK KAPALI — bilinçli tercih.
        // Paylaşımlı hostingde site başka bir fiziksel makineye taşınabilir; makine kapsamlı
        // DPAPI ile şifrelenmiş anahtarlar o durumda KALICI OLARAK çözülemez hale gelir ve
        // uygulama anahtarı okurken patlar. Anahtarlar zaten HTTP'den servis edilmeyen bir
        // klasörde durur (bkz. DataProtectionPathResolver). Sunucunun tamamına hâkimsen
        // (VPS/dedicated) DataProtection:ProtectWithDpapi=true ile at-rest şifreleme açılabilir.
        if (configuration.GetValue<bool?>("DataProtection:ProtectWithDpapi") == true)
        {
            if (OperatingSystem.IsWindows())
                builder.ProtectKeysWithDpapi(protectToLocalMachine: true);
            else
                result.Warnings.Add("DataProtection:ProtectWithDpapi=true ancak platform Windows değil; DPAPI atlandı.");
        }

        return result;
    }

    /// <summary>
    /// Anahtar deposunun nihai durumunu loglar. <c>builder.Build()</c> sonrasında çağrılmalıdır
    /// (gerçek logger o noktada hazırdır).
    /// </summary>
    public static void LogDataProtectionStatus(this WebApplication app, DataProtectionSetupResult result)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DataProtection");

        foreach (var warning in result.Warnings)
            logger.LogWarning("Data Protection: {Warning}", warning);

        if (result.KeyPath is null)
        {
            logger.LogError(
                "Data Protection anahtarları KALICI DEĞİL — hiçbir aday klasör yazılabilir değil ({Attempted}). " +
                "Anahtarlar bellekte tutulacak: her app pool geri dönüşümünde açık oturumlar düşecek ve " +
                "antiforgery hataları görülecek. Çözüm: appsettings'te DataProtection:KeyPath ile yazılabilir " +
                "bir klasör verin veya hosting firmasından uygulama klasörüne yazma izni isteyin.",
                string.Join(" | ", result.AttemptedPaths));
            return;
        }

        logger.LogInformation("Data Protection anahtarları kalıcı: {KeyPath}", result.KeyPath);
    }
}

/// <summary>Anahtar deposu çözümlemesinin sonucu.</summary>
public sealed class DataProtectionSetupResult
{
    /// <summary>Kullanılan klasör; hiçbir aday yazılabilir değilse <c>null</c> (bellek-içi).</summary>
    public string? KeyPath { get; internal set; }

    /// <summary>Sırayla denenen adaylar (tanı için).</summary>
    public List<string> AttemptedPaths { get; } = [];

    /// <summary>Ayağa kalkışta bildirilecek uyarılar.</summary>
    public List<string> Warnings { get; } = [];
}

internal static class DataProtectionPathResolver
{
    public static DataProtectionSetupResult Resolve(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string applicationName)
    {
        var result = new DataProtectionSetupResult();
        var candidates = new List<string>();

        // 1) Açık yapılandırma — hosting ortamına özgü yolu buradan verirsin. Her şeyi ezer.
        var configured = configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            candidates.Add(Path.Combine(configured, applicationName));

        // 2) App_Data — PAYLAŞIMLI HOSTING İÇİN ASIL ADAY.
        //    Sitenin kendi ağacında olduğu için app pool kimliğinin yazma izni olma olasılığı
        //    en yüksek yol burasıdır.
        //    HTTP'den erişilemezliğin ASIL güvencesi: ASP.NET Core statik dosya katmanı
        //    (UseStaticFiles/MapStaticAssets) yalnızca wwwroot'u servis eder; App_Data içerik
        //    kökünde, wwwroot'un DIŞINDA durur → hiçbir zaman statik dosya olarak sunulmaz.
        //    ("App_Data" adı ayrıca IIS'in varsayılan hiddenSegments listesinde yer alır, ama
        //     buna GÜVENME: bu projede kendi web.config'imizle zorlanmıyor, IIS'in global
        //     yapılandırmasına bağlı. Gerçek koruma yukarıdaki wwwroot sınırıdır.)
        candidates.Add(Path.Combine(environment.ContentRootPath, "App_Data", "dataprotection-keys", applicationName));

        // 3) %ProgramData% — yalnızca sunucuya tam hâkimsen (VPS/dedicated) çalışır.
        //    Publish klasöründen bağımsız olduğu için en dayanıklı yol, ama paylaşımlı
        //    hostingde çoğunlukla kapalıdır; bu yüzden App_Data'dan SONRA denenir.
        candidates.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FurkanTural", "dataprotection-keys", applicationName));

        foreach (var candidate in candidates)
        {
            result.AttemptedPaths.Add(candidate);
            if (TryPrepare(candidate, out var error))
            {
                // AYNI nesneyi döndür: yeni bir sonuç üretmek, önceki adayların neden
                // başarısız olduğunu anlatan uyarıları çöpe atardı — paylaşımlı hostingde
                // teşhis için asıl ihtiyacımız olan bilgi tam da odur.
                result.KeyPath = candidate;
                return result;
            }

            result.Warnings.Add($"'{candidate}' kullanılamadı: {error}");
        }

        return result;
    }

    // Klasörün oluşturulabilmesi TEK BAŞINA yeterli değil — bazı ortamlarda klasör açılır ama
    // içine dosya yazılamaz. Bu yüzden gerçek bir yazma/silme denemesi yapılır.
    private static bool TryPrepare(string path, out string error)
    {
        try
        {
            Directory.CreateDirectory(path);

            var probe = Path.Combine(path, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "x");
            File.Delete(probe);

            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }
}
