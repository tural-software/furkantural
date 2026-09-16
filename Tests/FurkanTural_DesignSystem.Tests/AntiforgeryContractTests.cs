using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Durum değiştiren her istek sahtecilik jetonuyla doğrulanır; istisna yoktur. Aksiyon başına öznitelik, yeni eklenen bir aksiyonda unutulabildiği için yetmezdi. Chat ve panelin BFF vekili MVC hattından geçmediği için orada ayrıca doğrulanır; tarayıcı tarafındaki her çağrı jetonu sayfanın başlığından okuyup başlık olarak gönderir.</summary>
public class AntiforgeryContractTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException(SolutionMarker);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root(), .. parts]));

    [Theory]
    [InlineData("FurkanTural_Admin")]
    [InlineData("FurkanTural_Chat")]
    [InlineData("FurkanTural_Blog")]
    [InlineData("FurkanTural_Portfolio")]
    public void Her_sitede_sahtecilik_jetonu_global_olarak_dogrulanir(string project)
    {
        var program = Read("Presentation", project, "Program.cs");

        program.Should().Contain("options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute())",
            "aksiyon başına öznitelik yeni bir aksiyonda unutulabilir; global süzgeç unutulamaz");
        program.Should().Contain("options.Cookie.SecurePolicy = CookieSecurePolicy.Always;");
        program.Should().Contain("options.Cookie.SameSite = SameSiteMode.Strict;");
    }

    [Theory]
    [InlineData("FurkanTural_Blog")]
    [InlineData("FurkanTural_Portfolio")]
    public void Anonim_sitelerin_anahtarlari_kalici_tutulur(string project)
    {
        Read("Presentation", project, "Program.cs").Should().Contain("AddPersistentDataProtection(",
            "anahtarlar bellekte kalırsa her yeniden başlatmada açık formların jetonu geçersizleşir ve gönderim 400 alır");
    }

    [Theory]
    [InlineData("FurkanTural_Admin")]
    [InlineData("FurkanTural_Chat")]
    public void Vekil_durum_degistiren_istekte_jetonu_ve_kokeni_zorunlu_tutar(string project)
    {
        var program = Read("Presentation", project, "Program.cs");

        program.Should().Contain("IsRequestValidAsync(context)",
            "BFF vekili MVC hattından geçmez; global süzgeç onu korumaz");
        program.Should().Contain("origin.Length == 0 && (changesState || upgrade)",
            "köken başlığı boş gelen durum değiştiren istek ya da WebSocket yükseltmesi geçmemeli");
        program.IndexOf("IsRequestValidAsync", StringComparison.Ordinal)
            .Should().BeLessThan(program.IndexOf("app.MapReverseProxy();", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("FurkanTural_Admin")]
    [InlineData("FurkanTural_Chat")]
    public void Sayfa_basligi_jetonu_tasir(string project)
    {
        Read("Presentation", project, "Views", "Shared", "_Layout.cshtml")
            .Should().Contain("<meta name=\"ft-antiforgery\" content=\"@Antiforgery.GetAndStoreTokens(Context).RequestToken\" />");
    }

    [Fact]
    public void Chat_istemcisinin_her_cagrisi_jetonu_gonderir()
    {
        var chat = Read("Presentation", "FurkanTural_Chat", "wwwroot", "js", "chat.js");
        chat.Should().Contain("opts.headers['RequestVerificationToken'] = csrfToken();");
        chat.Should().Contain(".withUrl('/bff/hubs/chat', { headers: { 'RequestVerificationToken': csrfToken() } })",
            "hub bağlantısının kurulduğu negotiate isteği de durum değiştiren bir POST'tur");

        var notifications = Read("Presentation", "FurkanTural_Chat", "wwwroot", "js", "notifications.js");
        notifications.Split("'RequestVerificationToken': csrfToken()").Length.Should().Be(3, "abone olma ve abonelikten çıkma");

        var logger = Read("Presentation", "FurkanTural_Chat", "wwwroot", "js", "logger.js");
        logger.Should().Contain("'RequestVerificationToken': csrfToken()");
        logger.Should().NotContain("sendBeacon", "sendBeacon başlık gönderemez; jetonsuz istek artık reddedilir");
    }

    [Fact]
    public void Panelin_canli_bildirim_baglantisi_jetonu_gonderir()
        => Read("Presentation", "FurkanTural_Admin", "wwwroot", "js", "admin-live.js")
            .Should().Contain(".withUrl(HUB, { headers: { 'RequestVerificationToken': csrfToken() } })");
}
