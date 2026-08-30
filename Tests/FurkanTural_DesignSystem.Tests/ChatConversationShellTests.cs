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
}
