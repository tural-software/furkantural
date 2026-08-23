namespace FurkanTural_Application.Settings;

/// <summary>Kullanıcı oturumu olmayan ön-yüzlerin (Blog, Portfolio, Chat) API'ye kimlik gösterdiği kayıt listesi; appsettings'teki AppTokens bölümünden bağlanır. Dosyadaki AppKey değerleri <c>0000:base64:0000</c> deseniyle şifreli tutulabilir, API açılışta çözdüğü için buraya her hâlükârda ham değer bağlanır. Verilen token'ın iptali yoktur: Jti üretilir ama hiçbir yerde doğrulanmaz, dolayısıyla bir AppKey'i değiştirmek yalnızca yeni token alınmasını engeller, dağıtılmış token'lar ExpiryDays dolana kadar geçerli kalır.</summary>
public class AppTokenSettings
{
    public int ExpiryDays { get; set; } = 365;
    public List<AppRegistration> Apps { get; set; } = [];
}

/// <summary>Tek bir ön-yüzün kimlik bilgisi. AppName yalnızca etiket değildir: AppKey ile birlikte eşleşme çiftini kurar, token'a <c>app_source</c> claim'i olarak yazılır ve ConfigController config izinlerini <c>AppConfigAccess:&lt;AppName&gt;</c> bölümünden okur — ad değişirse izin listesi hata vermeden boşalır. Eşleşme çift üzerinden yürüdüğü için aynı AppName farklı AppKey'lerle birden çok kez yazılabilir; anahtar döndürmesi bu sayede kesintisiz yapılır.</summary>
public class AppRegistration
{
    public string AppName { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
}
