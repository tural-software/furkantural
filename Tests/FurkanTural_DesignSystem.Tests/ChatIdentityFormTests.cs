using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Giriş ve üye ol sayfalarının kabuğu: 1024 üstünde solda tanıtım kolonu, sağda kart. Kart ve alan ölçüleri handoff'tan gelir. Yan kolonun başlığı sayfanın tek h1'idir; dar ekranda lede ve maddeler düşer ama başlık kalır, yoksa sayfa başlıksız olur.</summary>
public class ChatIdentityFormTests
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

    private static string Css() => ChatFile("wwwroot", "css", "chat.css");

    private static string RuleBody(string css, string selector)
    {
        var match = Regex.Match(css, @"(?<![\w-])" + Regex.Escape(selector) + @"\s*\{([^{}]*)\}");

        match.Success.Should().BeTrue($"'{selector}' kuralı bulunamadı");
        return match.Groups[1].Value;
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    public void Kimlik_sayfalari_tanitim_kolonunu_cizer(string sayfa)
    {
        var view = ChatFile("Views", "Account", $"{sayfa}.cshtml");

        view.Should().Contain("Html.PartialAsync(\"_AuthAside\")",
            "1024 üstünde iki kolon; sol kolon paylaşılan bir kısımdan gelir");
        view.Should().Contain("ViewData[\"AsideTitle\"]",
            "kısım başlığı görünümden alır, sayfaya göre değişir");
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    public void Kimlik_sayfalarinda_tek_h1_vardir(string sayfa)
    {
        var view = ChatFile("Views", "Account", $"{sayfa}.cshtml");
        var aside = ChatFile("Views", "Shared", "_AuthAside.cshtml");

        Regex.Matches(view + aside, "<h1").Count.Should().Be(1,
            "başlık yan kolonda; sayfanın kendisinde ikinci bir h1 açılırsa belge yapısı bozulur");

        aside.Should().Contain("<h1 class=\"auth-aside-title\"",
            "bu iki sayfanın başka başlığı yok");
    }

    [Fact]
    public void Dar_ekranda_baslik_kalir_yalnizca_lede_ve_maddeler_duser()
    {
        var css = Css();
        var blok = Regex.Matches(css, @"@media \(max-width: 1024px\)\s*\{((?:[^{}]|\{[^{}]*\})*)\}")
            .Select(m => m.Groups[1].Value)
            .FirstOrDefault(b => b.Contains(".auth-aside"));

        blok.Should().NotBeNull("yan kolonun duyarlı kuralları bulunamadı");

        blok!.Should().Contain(".auth-aside-text, .auth-points { display: none; }",
            "dar ekranda tanıtım metni ve maddeler düşer");

        blok.Should().NotContain(".auth-aside { display: none",
            "yan kolon tamamen gizlenirse sayfanın tek h1'i de gider");
    }

    [Fact]
    public void Kart_ve_alan_olculeri_handoffla_ayni()
    {
        var css = Css();

        var kart = RuleBody(css, ".auth-card");
        kart.Should().Contain("max-width: 420px");
        kart.Should().Contain("border-radius: 18px");
        kart.Should().Contain("padding: 32px 30px");

        var alan = RuleBody(css, ".auth-form input");
        alan.Should().Contain("height: 46px");
        alan.Should().Contain("border-radius: 12px");
        alan.Should().Contain("var(--bg-inset)", "alan dolgusu yüzey-2, kartın kendisi değil");

        var gonder = RuleBody(css, ".auth-form .btn-primary");
        gonder.Should().Contain("height: 48px");
        gonder.Should().Contain("border-radius: 12px");

        RuleBody(css, ".auth-logo").Should().Contain("width: 56px");
    }

    [Fact]
    public void Onay_kutusu_form_alani_dolgusunu_miras_almaz()
    {
        var kutu = RuleBody(Css(), ".agreement-check input");

        kutu.Should().Contain("width: 20px");
        kutu.Should().Contain("padding: 0",
            ".auth-form input kuralı 14px yatay dolgu veriyor; sıfırlanmazsa 20px kutu 30px çiziliyor");
        kutu.Should().Contain("appearance: none", "yerleşik kutu 20px yarıçap 6 olarak biçimlenemez");
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    [InlineData("Activate")]
    [InlineData("Close")]
    public void Kimlik_kabugunu_kullanan_sayfalar_ayni_logo_olcusunu_verir(string sayfa)
    {
        ChatFile("Views", "Account", $"{sayfa}.cshtml").Should().Contain("class=\"auth-logo\" width=\"56\" height=\"56\"",
            "genişlik/yükseklik öznitelikleri yerleşim kaymasını önlüyor; CSS 56px ise öznitelik de 56 olmalı");
    }
}
