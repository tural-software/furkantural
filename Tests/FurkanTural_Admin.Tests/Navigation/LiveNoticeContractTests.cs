using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Canlı bildirim dört parçanın birlikte durmasına bağlı: başlıktaki rozet, onu besleyen betik, betiğin kullandığı kitaplık ve tarayıcının WebSocket bağlantısına izin veren içerik güvenliği kuralı. Biri eksik olursa hiçbir şey hata vermez — panel yalnızca sessizce eski davranışına döner ve açık duran sekme yeni işi yine görmez.</summary>
public class LiveNoticeContractTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"'{SolutionMarker}' bulunamadı; arama '{AppContext.BaseDirectory}' dizininden yukarı doğru yapıldı.");
    }

    private static string AdminFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Admin", .. parts]));

    [Fact]
    public void Kitaplik_betikten_once_ve_yerel_kaynaktan_yuklenir()
    {
        var layout = AdminFile("Views", "Shared", "_Layout.cshtml");

        var library = layout.IndexOf("lib/signalr/signalr.min.js", StringComparison.Ordinal);
        var client = layout.IndexOf("js/admin-live.js", StringComparison.Ordinal);

        library.Should().BeGreaterThan(-1, "kitaplık yerel kopyadan gelir; içerik güvenliği kuralı dış betik kaynağına izin vermiyor");
        client.Should().BeGreaterThan(library, "istemci betiği kitaplıktan önce çalışırsa bağlantıyı hiç kuramaz");
        layout.Should().NotContain("cdn.jsdelivr.net", "panel dış bir betik kaynağına bağımlı olmamalı");
    }

    [Fact]
    public void Rozet_sifirken_de_sayfada_durur_ve_gizli_baslar()
    {
        var header = AdminFile("Views", "Shared", "_LayoutSections", "_Header.cshtml");

        var tag = Regex.Match(header, @"<a[^>]*id=""adminPendingBadge""[^>]*>", RegexOptions.Singleline);

        tag.Success.Should().BeTrue("betik rozeti kimliğiyle arar; bulamazsa bağlantıyı hiç kurmaz");
        tag.Value.Should().Contain("hidden", "sayı hub'dan gelmeden rozet görünürse sayfa ilk anda yanlış bir sıfır gösterir");
        header.Should().NotMatchRegex(@"@if[^{]*\{[^}]*adminPendingBadge",
            "rozet koşullu çizilirse sıfırken hiç var olmaz ve itilen sayı onu geri getiremez");
    }

    [Fact]
    public void Icerik_guvenligi_ayni_kokenin_websocket_baglantisina_izin_verir()
    {
        var program = AdminFile("Program.cs");

        var connect = Regex.Match(program, @"connect-src 'self'[^;]*;");

        connect.Success.Should().BeTrue();
        connect.Value.Should().Contain("wss://{context.Request.Host}",
            "tarayıcı güvenli WebSocket bağlantısını ancak connect-src izin verirse açar");
        connect.Value.Should().Contain("ws://{context.Request.Host}",
            "şema istekten okunursa ters vekil arkasında yanlış çıkabilir; iki şema da yazılır ki bağlantı hangi taraftan gelirse gelsin geçsin");
    }

    [Fact]
    public void Vekil_jetonu_oturumdan_basar_ve_tarayiciya_vermez()
    {
        var program = AdminFile("Program.cs");
        var client = AdminFile("wwwroot", "js", "admin-live.js");

        program.Should().Contain("\"/bff/{**catch-all}\"");
        program.Should().Contain("session.GetString(\"token\")", "jeton vekilde oturumdan okunur");
        program.Should().Contain("app.MapReverseProxy();");

        client.Should().Contain("'/bff/hubs/admin'", "istemci API'ye doğrudan değil aynı kökenden vekile bağlanır");
        client.Should().NotContain("accessTokenFactory", "jeton tarayıcıya hiç inmemeli; bağlantıya vekil ekler");
        client.Should().NotContain("access_token");
    }

    [Fact]
    public void Jeton_suresi_dolmadan_yenilenir()
    {
        var program = AdminFile("Program.cs");

        program.Should().Contain("TryRefreshTokenAsync",
            "oturum sekiz saat, jeton çok daha kısa; yenilenmezse yönetici saat başı girişe düşer ve canlı bağlantı kopar");
        program.Should().Contain("/api/v1/Auth/refresh");
    }

    [Fact]
    public void Liste_tazelenince_rozet_hubdan_yeniden_esitlenir()
    {
        var client = AdminFile("wwwroot", "js", "admin-live.js");

        client.Should().Contain("ft:table-rendered",
            "yöneticinin kendi işlemi — onay, silme, toplu işlem — hub olayı üretmez; rozet ancak liste tazelenince yeniden sorulursa doğru kalır");
        client.Should().Contain("invoke('RefreshPendingWork')",
            "rozet sayısını sunucu hesaplar; istemci kendi tahminiyle düşürürse başka sekmedeki işlemleri kaçırır");
    }

    [Fact]
    public void Gorunen_listenin_turunde_olay_gelince_sayaclar_satirlara_dokunmadan_tazelenir()
    {
        var client = AdminFile("wwwroot", "js", "admin-live.js");
        var nav = AdminFile("wwwroot", "js", "list-nav.js");

        client.Should().Contain("FtList.refreshStats()",
            "rozet artarken sayfadaki bekleyen sayacı eski değerde kalırsa aynı ekranda iki farklı sayı görünür");

        var body = Regex.Match(nav, @"function refreshStats\(\)\s*\{.*?\n    \}", RegexOptions.Singleline);
        body.Success.Should().BeTrue("sayaç tazeleme ortak liste betiğinde tanımlı olmalı");
        body.Value.Should().NotContain("section.innerHTML",
            "yeni kayıt geldiğinde satırlar kaymamalı; yalnızca sayaçlar değişir, tabloyu yönetici tazeler");
        body.Value.Should().NotContain("ft:table-rendered",
            "sayaç tazelemesi liste tazelemesi sayılmaz; olayı yayarsa bildirim şeridi hemen kapanır");

        nav.Should().Contain("refreshStats: refreshStats", "istemci betiği yalnızca yayımlanan yüzeyi görür");
    }
}
