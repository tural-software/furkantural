using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Sohbet kabuğunun handoff ölçüleri: liste 330px (tabletde 270px), satır 56px (mobilde 64px), balon genişlikleri ve yarıçapları, başlık eylemleri. §3-K'da ölçülen eksik 1024 kırılma noktası bu dalgada eklendi.</summary>
public class ChatConversationShellTests
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

    private static string Css() => File.ReadAllText(Path.Combine(
        FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "wwwroot", "css", "chat.css"));

    private static string RuleBody(string css, string selector)
    {
        var match = Regex.Match(css, @"(?<![\w-])" + Regex.Escape(selector) + @"\s*\{([^{}]*)\}");

        match.Success.Should().BeTrue($"'{selector}' kuralı bulunamadı");
        return match.Groups[1].Value;
    }

    private static string MediaBlock(string css, string query, string icerir)
    {
        var blok = Regex.Matches(css, Regex.Escape(query) + @"\s*\{((?:[^{}]|\{[^{}]*\})*)\}")
            .Select(m => m.Groups[1].Value)
            .FirstOrDefault(b => b.Contains(icerir));

        blok.Should().NotBeNull($"'{query}' içinde '{icerir}' taşıyan blok bulunamadı");
        return blok!;
    }

    [Fact]
    public void Liste_paneli_masaustunde_330_tablette_270()
    {
        var css = Css();

        RuleBody(css, ".chat-app").Should().Contain("grid-template-columns: 330px 1fr",
            "kod 320px sabitti; handoff 330 diyor");

        MediaBlock(css, "@media (max-width: 1024px)", ".chat-app")
            .Should().Contain("grid-template-columns: 270px 1fr",
                "1024 kırılma noktası kodda hiç yoktu; tablet genişliği burada daralır");
    }

    [Fact]
    public void Arkadas_satiri_masaustunde_56_mobilde_64()
    {
        var css = Css();

        var satir = RuleBody(css, ".friend-item");
        satir.Should().Contain("min-height: 56px");

        MediaBlock(css, "@media (max-width: 768px)", ".friend-item")
            .Should().Contain(".friend-item { min-height: 64px; }",
                "dokunma hedefi telefonda büyür");

        RuleBody(css, ".friend-item, .request-item").Should().Contain("border-radius: 12px",
            "seçili satır yarıçaplı bir şerit; tam genişlik zemin değil");
    }

    [Fact]
    public void Arama_alani_ve_bolum_basligi_handoff_olculerinde()
    {
        var css = Css();

        var arama = RuleBody(css, ".search input");
        arama.Should().Contain("height: 40px");
        arama.Should().Contain("border-radius: 12px");
        arama.Should().Contain("var(--bg-inset)", "arama alanı yüzey-2 dolgulu");

        var baslik = RuleBody(css, ".section-head");
        baslik.Should().Contain("font-size: 11.5px");
        baslik.Should().Contain("font-weight: 700");
    }

    [Fact]
    public void Istek_sayaci_aksan_dolgulu_ve_18px()
    {
        var rozet = RuleBody(Css(), ".badge");

        rozet.Should().Contain("height: 18px");
        rozet.Should().Contain("background: var(--accent)", "handoff sayacı vurgu dolgulu istiyor");
        rozet.Should().Contain("color: var(--on-accent)");
        rozet.Should().NotContain("#fff", "dolgu üzerindeki metin token'dan gelmeli");
    }

    [Fact]
    public void Baslik_eylemleri_ve_menu_handoff_olculerinde()
    {
        var css = Css();

        var eylem = RuleBody(css, ".conv-act");
        eylem.Should().Contain("width: 40px");
        eylem.Should().Contain("border-radius: 11px",
            "daire değil yumuşatılmış kare; handoff 11 yarıçap veriyor");

        var menu = RuleBody(css, ".conv-menu");
        menu.Should().Contain("min-width: 180px");
        menu.Should().Contain("var(--shadow-overlay)",
            "sabit gölge açık temada beyaz zemine siyah leke bırakıyordu");
    }

    [Fact]
    public void Balonlar_handoff_genislik_ve_yaricaplarini_kullanir()
    {
        var css = Css();

        RuleBody(css, ".msg").Should().Contain("max-width: 62%");
        MediaBlock(css, "@media (max-width: 768px)", ".msg")
            .Should().Contain(".msg { max-width: 78%; }");

        RuleBody(css, ".bubble").Should().Contain("padding: 10px 14px");
        RuleBody(css, ".bubble").Should().Contain("border-radius: 16px");

        RuleBody(css, ".msg.in .bubble").Should().Contain("border-bottom-left-radius: 5px");
        RuleBody(css, ".msg.out .bubble").Should().Contain("border-bottom-right-radius: 5px");
    }

    [Fact]
    public void Alt_kullanici_cubugu_dar_listede_sikismaz()
    {
        var dugme = RuleBody(Css(), ".me .theme-toggle-btn");

        dugme.Should().Contain("width: 34px");
        dugme.Should().Contain("flex: 0 0 auto",
            "270px listede düğme 28px'e eziliyordu; esneme kapatılmazsa dokunma hedefi bozulur");
    }

    [Fact]
    public void Her_sayfa_gorunumu_tam_bir_h1_tasir()
    {
        var root = Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "Views");
        var kismi = new[] { "_AuthAside.cshtml" };
        var sapan = new List<string>();
        var olculen = 0;

        foreach (var file in Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories))
        {
            var ad = Path.GetFileName(file);
            if (ad.StartsWith('_') && !kismi.Contains(ad)) continue;

            var icerik = File.ReadAllText(file);
            var sayi = Regex.Matches(icerik, "<h1[ >]").Count;

            if (ad == "_AuthAside.cshtml")
            {
                olculen++;
                if (sayi != 1) sapan.Add($"{ad}: {sayi} adet h1");
                continue;
            }

            if (ad == "Login.cshtml" || ad == "Register.cshtml")
            {
                olculen++;
                if (sayi != 0) sapan.Add($"{ad}: başlık yan kolondan gelir, burada {sayi} adet h1 var");
                continue;
            }

            olculen++;
            if (sayi == 0) sapan.Add($"{ad}: hiç h1 yok");
        }

        sapan.Should().BeEmpty(
            "sohbet ekranı hiç h1 taşımıyordu; konuşma başlığı sayfanın konusudur ve " +
            "ekran okuyucu kullanıcısı için tek üst düzey başlıktır. Birden fazla h1 " +
            "yazılmış olması kusur değildir: Activate dörtü de birbirini dışlayan dalda " +
            "taşır, çalışma anında bir tanesi çizilir — kesinliği kaynak metni değil " +
            "tarayıcı taraması doğrular");

        olculen.Should().BeGreaterThan(7, "görünüm taraması boşsa bu test bir şey doğrulamıyor");
    }

    [Fact]
    public void Konusma_basligi_handoff_olcusunde()
    {
        var css = File.ReadAllText(Path.Combine(
            FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "wwwroot", "css", "chat.css"));

        var kural = Regex.Match(css, @"#convTitle\s*\{([^{}]*)\}");

        kural.Success.Should().BeTrue("#convTitle kuralı bulunamadı");
        kural.Groups[1].Value.Should().Contain("font-size: 14.5px",
            "h1 olunca tarayıcı varsayılanı 2em'e çıkarır; handoff başlığı 14,5px istiyor");
        kural.Groups[1].Value.Should().Contain("margin: 0",
            "h1'in varsayılan üst/alt boşluğu başlık çubuğunu şişirir");
    }
}
