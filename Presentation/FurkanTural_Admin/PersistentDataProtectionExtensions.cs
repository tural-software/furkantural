using Microsoft.AspNetCore.DataProtection;

namespace FurkanTural_Admin;

/// <summary>
/// Oturum çerezini ve sahtecilik jetonunu şifreleyen anahtarların nerede saklanacağını çözer.
///
/// Varsayılan davranışta, IIS altında uygulama havuzunun profili yüklü değilse anahtarlar yalnızca
/// bellekte tutulur; her geri dönüşümde ve her yayında sıfırlanırlar. Sonuç açık oturumların toptan
/// düşmesi ve sahtecilik jetonu hatalarıdır. Yönetim panelinde bedeli, oturumdaki jetonun da
/// gitmesi ve yöneticinin kaydettiği formu tamamlayamadan girişe atılmasıdır.
///
/// Uygulama paylaşımlı bir barındırmada çalışır ve havuz kimliği çoğu zaman yalnızca sitenin kendi
/// klasör ağacına erişebilir; makine geneli yollar kapalı olabilir. Bu yüzden tek bir yola bel
/// bağlanmaz: adaylar sırayla denenir ve gerçek bir yazma denemesinden geçen ilki kullanılır.
///
/// Hiçbiri yazılamıyorsa uygulama durdurulmaz. Durmak, çözülmeye çalışılan sorundan daha kötüsünü
/// üretirdi: oturumların düşmesi yerine sitenin tümüyle kapanması. Bunun yerine belleğe düşülür ve
/// durum hata seviyesinde kaydedilir.
///
/// Anahtarları veri tabanında tutmak daha dayanıklı olurdu ama mimari buna izin vermez; sunum
/// projeleri veri tabanına dokunmaz.
///
/// Aynı mantığın bir kopyası
/// <c>Presentation/FurkanTural_Chat/PersistentDataProtectionExtensions.cs</c> içindedir. Biri
/// değişirse diğeri de değişmelidir.
/// </summary>
public static class PersistentDataProtectionExtensions
{
    /// <summary>
    /// Sonucu döndürür ama kaydetmez; kayıt <see cref="LogDataProtectionStatus"/> ile ve uygulama
    /// ayağa kalktıktan sonra yapılır, çünkü gerçek kaydedici bu noktada henüz yoktur.
    ///
    /// applicationName anahtar halkasını yalıtan sabit addır ve içerik kökü yolundan
    /// türetilmemelidir: uygulama başka bir klasöre taşındığında ad da değişir ve o ana kadar
    /// üretilmiş bütün anahtarlar okunamaz hâle gelir.
    ///
    /// At-rest şifreleme varsayılan olarak kapalıdır. Paylaşımlı barındırmada site başka bir
    /// fiziksel makineye taşınabilir ve makine kapsamlı şifrelenmiş anahtarlar o durumda kalıcı
    /// olarak çözülemez; uygulama anahtarı okurken düşer. Sunucunun tamamına hâkim olunan bir
    /// kurulumda <c>DataProtection:ProtectWithDpapi</c> ile açılabilir.
    /// </summary>
    public static DataProtectionSetupResult AddPersistentDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string applicationName)
    {
        var result = DataProtectionPathResolver.Resolve(configuration, environment, applicationName);

        if (result.KeyPath is null)
            return result;

        var builder = services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(result.KeyPath))
            .SetApplicationName(applicationName);

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
    /// <c>Build()</c> sonrasında çağrılmalıdır. Denenip başarısız olan adayların her biri uyarı
    /// olarak, kalıcılığın hiç sağlanamaması ise hata olarak kaydedilir; paylaşımlı barındırmada
    /// hangi yolun neden kapalı olduğunu gösteren tek kayıt budur.
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

public sealed class DataProtectionSetupResult
{
    /// <summary>
    /// Hiçbir aday yazılabilir değilse null kalır ve anahtarlar bellekte tutulur.
    /// </summary>
    public string? KeyPath { get; internal set; }

    public List<string> AttemptedPaths { get; } = [];

    public List<string> Warnings { get; } = [];
}

internal static class DataProtectionPathResolver
{
    /// <summary>
    /// Adaylar şu sırayla denenir: açık yapılandırma, sitenin kendi ağacındaki App_Data, ardından
    /// makine geneli ortak veri klasörü. Sıra dayanıklılığa göre değil erişilebilirliğe göredir;
    /// makine geneli yol yayın klasöründen bağımsız olduğu için daha dayanıklıdır ama paylaşımlı
    /// barındırmada çoğunlukla kapalıdır, bu yüzden sonda durur.
    ///
    /// App_Data'nın tarayıcıdan okunamamasının gerçek güvencesi statik dosya sınırıdır: yalnızca
    /// wwwroot servis edilir, App_Data ise içerik kökünde, onun dışında durur. Bu adın IIS'in
    /// varsayılan gizli segment listesinde bulunması ayrı bir gerekçe sayılmaz; o liste sunucunun
    /// genel yapılandırmasına bağlıdır ve bu projede kendi ayarımızla zorlanmaz.
    ///
    /// Başarılı adayda yeni bir sonuç nesnesi üretilmez, aynı nesne döndürülür: önceki adayların
    /// neden elendiğini anlatan uyarılar korunsun diye. Paylaşımlı barındırmada teşhis için asıl
    /// gereken bilgi odur.
    /// </summary>
    public static DataProtectionSetupResult Resolve(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string applicationName)
    {
        var result = new DataProtectionSetupResult();
        var candidates = new List<string>();

        var configured = configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            candidates.Add(Path.Combine(configured, applicationName));

        candidates.Add(Path.Combine(environment.ContentRootPath, "App_Data", "dataprotection-keys", applicationName));

        candidates.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FurkanTural", "dataprotection-keys", applicationName));

        foreach (var candidate in candidates)
        {
            result.AttemptedPaths.Add(candidate);
            if (TryPrepare(candidate, out var error))
            {
                result.KeyPath = candidate;
                return result;
            }

            result.Warnings.Add($"'{candidate}' kullanılamadı: {error}");
        }

        return result;
    }

    /// <summary>
    /// Klasörün oluşturulabilmesi tek başına yeterli değildir; bazı ortamlarda klasör açılır ama
    /// içine dosya yazılamaz. Bu yüzden geçici bir dosya gerçekten yazılıp silinir. Aday ancak bu
    /// denemeden geçerse kabul edilir.
    /// </summary>
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