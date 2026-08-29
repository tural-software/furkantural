using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Açılış sayfasının handoff'ta tarif edilen iskeleti: kahraman kolonu, durum çipi, önizleme kartı ve üç özellik. Ölçüler üç kırılma noktasında değişir; önizleme kartı sahte bir konuşma olduğu için ekran okuyucuya duyurulmaz.</summary>
public class ChatLandingContractTests
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

    private static string ChatFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Chat", .. parts]));

    private static string Landing() => ChatFile("Views", "Home", "Index.cshtml");
    private static string Css() => ChatFile("wwwroot", "css", "chat.css");
    private static string Theme() => ChatFile("wwwroot", "css", "theme.css");

    private static string MediaBlock(string css, string query)
    {
        var start = css.IndexOf(query, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"'{query}' bloğu bulunamadıysa test bir şey doğrulamıyor");

        var next = css.IndexOf("@media", start + query.Length, StringComparison.Ordinal);
        return next < 0 ? css[start..] : css[start..next];
    }

    [Fact]
    public void Acilis_kahraman_cip_onizleme_ve_uc_ozellik_tasir()
    {
        var view = Landing();

        view.Should().Contain("hero-chip", "durum çipi kahramanın ilk ögesi");
        view.Should().Contain("hero-title", "başlık sayfanın tek h1'i");
        view.Should().Contain("class=\"preview\"", "önizleme kartı ürünün ne yaptığını gösteren tek görsel");

        Regex.Matches(view, "class=\"feature\"").Count.Should().Be(3,
            "özellik satırı üç kolon; sayı değişirse 320px sınırı ve 48px aralık başka bir ızgaraya oturur");
    }

    [Fact]
    public void Onizleme_karti_ekran_okuyucuya_duyurulmaz()
    {
        var view = Landing();
        var kart = Regex.Match(view, @"<div class=""preview""([^>]*)>");

        kart.Success.Should().BeTrue("önizleme kartı bulunamadı");
        kart.Groups[1].Value.Should().Contain("aria-hidden=\"true\"",
            "kart sahte bir konuşma; okunduğunda ekran okuyucu kullanıcısına gerçek mesaj gibi gelir");
    }

    [Fact]
    public void Kahraman_basligi_uc_kirilma_noktasinda_kuculur()
    {
        var css = Css();

        Regex.Match(css, @"\.hero-title\s*\{[^}]*font-size:\s*46px").Success.Should().BeTrue(
            "geniş ekranda başlık 46px");

        MediaBlock(css, "@media (max-width: 1024px)").Should().MatchRegex(@"\.hero-title\s*\{[^}]*font-size:\s*38px",
            "1024 altında 38px");

        MediaBlock(css, "@media (max-width: 768px)").Should().MatchRegex(@"\.hero-title\s*\{[^}]*font-size:\s*30px",
            "768 altında 30px");
    }

    [Fact]
    public void Kahraman_1024_altinda_tek_kolona_iner()
    {
        MediaBlock(Css(), "@media (max-width: 1024px)")
            .Should().MatchRegex(@"\.hero\s*\{[^}]*flex-direction:\s*column",
                "önizleme kartı 1024 üstünde sağda durur, altında kahramanın altına iner");
    }

    [Fact]
    public void Kahraman_dugmeleri_form_dugmesinin_ust_boslugunu_almaz()
    {
        Regex.Match(Css(), @"\.hero-cta \.hero-btn\s*\{([^}]*)\}").Groups[1].Value
            .Should().Contain("margin-top: 0",
                ".btn-primary gönder düğmesi için 18px üst boşluk taşıyor; " +
                "aynı kusur üst çubukta da çıkmıştı, orada çubuğu 79px yapmıştı");
    }

    [Fact]
    public void Yuzey_tokeni_iki_temada_tanimli_ve_gercekten_kullaniliyor()
    {
        Regex.Matches(Theme(), @"^\s*--bg-inset\s*:", RegexOptions.Multiline).Count.Should().Be(2,
            "token tek temada tanımlanırsa diğer tema onu miras alır");

        Regex.Matches(Css(), @"var\(--bg-inset\)").Count.Should().BeGreaterThan(0,
            "Dalga 1'de bu token çağrı yeri olmadığı için bilerek eklenmemişti; " +
            "tanımlanıp kullanılmayan token düzeltme sanılır");
    }
}
