namespace FurkanTural_Application.Settings;

/// <summary>Kullanıcı oturumu olmayan ön-yüzlerin (Blog, Portfolio, Chat, panel) API'ye kimlik gösterdiği kayıt listesi; appsettings'teki AppTokens bölümünden bağlanır. Dosyadaki AppKey değerleri <c>0000:base64:0000</c> deseniyle şifreli tutulabilir, API açılışta çözdüğü için buraya her hâlükârda ham değer bağlanır.<para>Verilen jeton saatlerle ölçülür ve onu üreten anahtarın kimliğini taşır; API her istekte bu kimliği listedeki güncel anahtarlara karşı sınar. Bir AppKey'i değiştirmek ya da uygulamayı listeden çıkarmak, dağıtılmış jetonları süresini beklemeden geçersiz kılar.</para><para>Süre 2 ile 24 saat arasına sıkıştırılır. Ön-yüzler jetonu bitişinden bir saat önce yeniler; iki saatin altındaki bir süre neredeyse her istekte yeni jeton almaya dönerdi.</para></summary>
public class AppTokenSettings
{
    public const int MinimumExpiryHours = 2;

    public const int MaximumExpiryHours = 24;

    public int ExpiryHours { get; set; } = 6;

    public List<AppRegistration> Apps { get; set; } = [];

    public TimeSpan Lifetime => TimeSpan.FromHours(Math.Clamp(ExpiryHours, MinimumExpiryHours, MaximumExpiryHours));
}

/// <summary>Tek bir ön-yüzün kimlik bilgisi. AppName yalnızca etiket değildir: AppKey ile birlikte eşleşme çiftini kurar, token'a <c>app_source</c> claim'i olarak yazılır ve ConfigController config izinlerini <c>AppConfigAccess:&lt;AppName&gt;</c> bölümünden okur — ad değişirse izin listesi hata vermeden boşalır. Eşleşme çift üzerinden yürüdüğü için aynı AppName farklı AppKey'lerle birden çok kez yazılabilir; anahtar döndürmesi bu sayede kesintisiz yapılır.</summary>
public class AppRegistration
{
    public string AppName { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
}
