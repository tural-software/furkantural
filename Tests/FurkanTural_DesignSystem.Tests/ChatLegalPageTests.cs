using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Yasal sayfaların okuma kabuğu: 1024 üstünde 210px yapışkan bölüm dizini, 96ch okuma kolonu, iki yana yaslı gövde ve sayfa sonunda iletişim kutusu. Dizin madde adlarını görünümden değil başlıklardan üretir; metin tek yerde durur.</summary>
public class ChatLegalPageTests
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
    [InlineData("Agreement", 16)]
    [InlineData("Privacy", 16)]
    [InlineData("Rules", 10)]
    [InlineData("Imprint", 5)]
    public void Yasal_sayfalar_dizin_geri_baglantisi_ve_iletisim_kutusu_tasir(string sayfa, int madde)
    {
        var view = ChatFile("Views", "Home", $"{sayfa}.cshtml");

        view.Should().Contain("class=\"legal-index\"", "1024 üstünde solda bölüm dizini durur");
        view.Should().Contain("legal-index.js", "dizini dolduran betik olmadan liste boş kalır");
        view.Should().Contain("class=\"legal-back\"", "okuma kolonunun üstünde ana sayfaya dönüş bağlantısı");
        view.Should().Contain("Html.PartialAsync(\"_LegalContact\")", "sayfa sonunda ortak iletişim kutusu");

        Regex.Matches(view, "<h2>").Count.Should().Be(madde,
            "dizin madde adlarını başlıklardan üretiyor; madde sayısı değişirse dizin de değişir");
    }

    [Fact]
    public void Dizin_madde_adlarini_gorunumde_yinelemez()
    {
        foreach (var sayfa in new[] { "Agreement", "Privacy", "Rules", "Imprint" })
        {
            var view = ChatFile("Views", "Home", $"{sayfa}.cshtml");
            var liste = Regex.Match(view, @"<ol class=""legal-index-list""[^>]*>(.*?)</ol>", RegexOptions.Singleline);

            liste.Success.Should().BeTrue($"{sayfa}: dizin listesi bulunamadı");
            liste.Groups[1].Value.Trim().Should().BeEmpty(
                $"{sayfa}: madde adları elle yazılırsa başlık metniyle zamanla ayrışır; liste betikle dolar");
        }
    }

    [Fact]
    public void Sozlesme_sayfasindaki_surum_yururlukteki_surumle_aynidir()
    {
        var sabit = File.ReadAllText(Path.Combine(FindSolutionRoot(), "Core", "FurkanTural_Domain", "Constants", "AgreementDefinitions.cs"));
        var surum = Regex.Match(sabit, @"CurrentVersion = ""([^""]+)""").Groups[1].Value;

        surum.Should().NotBeEmpty("yürürlükteki sözleşme sürümü sabitten okunamadı");
        ChatFile("Views", "Home", "Agreement.cshtml").Should().Contain($"Sürüm {surum} —",
            "metin değişip sabit ilerletilmezse üyeler yeni metni onaylamadan kullanmaya devam eder; " +
            "sabit ilerletilip metin değişmezse onay penceresi eski metni yeniden onaylatır");
    }

    [Theory]
    [InlineData("Rules", "Topluluk Kuralları")]
    [InlineData("Privacy", "Gizlilik Politikası")]
    [InlineData("Agreement", "Üyelik Sözleşmesi")]
    public void Uygulama_ici_yasal_belgeler_sayfalarla_ayni_adresi_kullanir(string sayfa, string baslik)
    {
        var betik = ChatFile("wwwroot", "js", "call.js");

        betik.Should().Contain($"url: '/Home/{sayfa}'", "ayarlardaki belge penceresi sayfanın kendisini okur");
        betik.Should().Contain($">{baslik}</button>", "belgeye ayarlardan ulaşan bir düğme olmalı");
    }

    [Theory]
    [InlineData("Agreement")]
    [InlineData("Rules")]
    [InlineData("Privacy")]
    [InlineData("Imprint")]
    public void Alt_bilgi_her_yasal_sayfaya_baglanir(string sayfa)
    {
        ChatFile("Views", "Shared", "_Footer.cshtml").Should().Contain($"asp-action=\"{sayfa}\"",
            "yasal metinler giriş yapmadan da her sayfanın altından ulaşılabilir olmalı");
    }

    [Fact]
    public void Okuma_kolonu_ve_dizin_olculeri_handoffla_ayni()
    {
        var css = Css();

        var wrap = RuleBody(css, ".legal-wrap");
        wrap.Should().Contain("gap: 52px");

        var dizin = RuleBody(css, ".legal-index");
        dizin.Should().Contain("flex: 0 0 210px");
        dizin.Should().Contain("position: sticky", "dizin okurken ekranda kalmalı");

        var kart = RuleBody(css, ".legal-card");
        kart.Should().Contain("max-width: 96ch", "okuma ölçüsü karakter sayısıyla verilir, piksele sabitlenmez");
        kart.Should().Contain("var(--shadow-overlay)", "gölge açık temada da doğru olmalı");
    }

    [Fact]
    public void Govde_iki_yana_yasli_ve_tirelemeli()
    {
        var govde = RuleBody(Css(), ".legal-card p");

        govde.Should().Contain("text-align: justify");
        govde.Should().Contain("hyphens: auto",
            "tireleme olmadan iki yana yaslama Türkçe uzun sözcüklerde kelime aralarını açar");
        govde.Should().Contain("line-height: 1.7");
    }

    [Fact]
    public void Kart_ici_kisa_paragraflar_govde_kuralina_kapilmaz()
    {
        var css = Css();

        foreach (var sinif in new[] { "legal-meta", "legal-contact-title", "legal-contact-text" })
        {
            Regex.IsMatch(css, $@"^\.{sinif}\s*\{{", RegexOptions.Multiline).Should().BeFalse(
                $"'.{sinif}' tek başına '.legal-card p' ile aynı özgüllükte (0,1,1 karşısında 0,1,0) kalır " +
                "ve gövde kuralına kapılır: sürüm satırı 15,5px iki yana yaslı çiziliyordu");

            Regex.IsMatch(css, $@"^\.legal-[\w-]+ \.{sinif}\s*\{{", RegexOptions.Multiline).Should().BeTrue(
                $"'.{sinif}' kapsayıcısıyla birlikte yazılmalı");
        }
    }

    [Fact]
    public void Dizin_dar_ekranda_yer_kaplamaz()
    {
        var blok = Regex.Matches(Css(), @"@media \(max-width: 1024px\)\s*\{((?:[^{}]|\{[^{}]*\})*)\}")
            .Select(m => m.Groups[1].Value)
            .FirstOrDefault(b => b.Contains(".legal-index"));

        blok.Should().NotBeNull("dizinin duyarlı kuralı bulunamadı");
        blok!.Should().Contain(".legal-index { display: none; }");
        blok.Should().Contain(".legal-wrap { gap: 0; }",
            "dizin gizlenince 52px boşluk kalırsa kart sağa kayar");
    }
}
