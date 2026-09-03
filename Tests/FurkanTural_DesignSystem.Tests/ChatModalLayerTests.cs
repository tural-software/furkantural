using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Modal katmanı iki perde ayırır: iletişim kutuları temaya uyan <c>--scrim</c> kullanır, medya izleyicileri (çağrı, ışık kutusu, fotoğraf büyütme) her iki temada koyu kalan <c>--scrim-media</c>. Çağrı kutusunun içi bilerek koyudur; beyaz denetimler orada okunur.</summary>
public class ChatModalLayerTests
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

    private static string ChatCss() => File.ReadAllText(Path.Combine(
        FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "wwwroot", "css", "chat.css"));

    private static string ThemeCss() => File.ReadAllText(Path.Combine(
        FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "wwwroot", "css", "theme.css"));

    private static string RuleBody(string css, string selector)
    {
        var match = Regex.Match(css, @"(?<![\w-])" + Regex.Escape(selector) + @"\s*\{([^{}]*)\}");

        match.Success.Should().BeTrue($"'{selector}' kuralı bulunamadı");
        return match.Groups[1].Value;
    }

    [Fact]
    public void Perde_tokenlari_dogru_yerde_temaya_bolunur()
    {
        var theme = ThemeCss();

        Regex.Matches(theme, @"^\s*--scrim\s*:", RegexOptions.Multiline).Count.Should().Be(2,
            "iletişim kutusu perdesi temaya uymalı: koyuda %66, açıkta %35");

        Regex.Matches(theme, @"^\s*--scrim-media\s*:", RegexOptions.Multiline).Count.Should().Be(1,
            "medya perdesi bilerek tema-bağımsız; video ve fotoğraf koyu zeminde izlenir");
    }

    [Theory]
    [InlineData(".device-modal")]
    [InlineData(".consent-overlay")]
    [InlineData(".profile-overlay")]
    [InlineData(".agreement-overlay")]
    [InlineData(".ask-overlay")]
    public void Iletisim_kutusu_perdeleri_temaya_uyar(string selector)
    {
        RuleBody(ChatCss(), selector).Should().Contain("var(--scrim)",
            "açık temada %82 siyah perde, altındaki beyaz sayfayı gece yapıyordu");
    }

    [Theory]
    [InlineData(".call-overlay")]
    [InlineData(".img-lightbox")]
    [InlineData(".profile-zoom")]
    public void Medya_perdeleri_iki_temada_da_koyu_kalir(string selector)
    {
        RuleBody(ChatCss(), selector).Should().Contain("var(--scrim-media)",
            "video ve fotoğraf koyu zeminde izlenir; burada tema takibi doğru olmaz");
    }

    [Fact]
    public void Tam_ekran_ortuler_perdesini_tokendan_alir()
    {
        var css = ChatCss();
        var sapan = new List<string>();
        var olculen = 0;

        foreach (Match rule in Regex.Matches(css, @"([^{}]*)\{([^{}]*)\}", RegexOptions.Singleline))
        {
            var body = rule.Groups[2].Value;

            if (!Regex.IsMatch(body, @"position:\s*fixed") || !Regex.IsMatch(body, @"inset:\s*0"))
                continue;

            var arka = Regex.Match(body, @"(?<![-\w])background:\s*([^;]+)");
            if (!arka.Success) continue;

            olculen++;
            var deger = arka.Groups[1].Value.Trim();

            if (deger.StartsWith("var(--scrim") || deger == "transparent")
                continue;

            sapan.Add($"{rule.Groups[1].Value.Trim()} → background: {deger}");
        }

        sapan.Should().BeEmpty(
            "tam ekran örtünün zemini perdedir; ham rgba yazılırsa tema değişiminden habersiz kalır");

        olculen.Should().BeGreaterThanOrEqualTo(6,
            "örtü taraması boş dönerse bu test hiçbir şey doğrulamıyor");
    }

    [Fact]
    public void Cagri_denetimleri_okunabilir_dolgu_kullanir()
    {
        var css = ChatCss();

        RuleBody(css, ".call-accept").Should().Contain("var(--success-solid)",
            "beyaz simge #22c55e üzerinde 2,28:1 kalıyordu; grafik denetim için eşik 3:1, " +
            "--success-solid ile 5,02:1 oluyor");

        RuleBody(css, ".call-hang").Should().Contain("var(--error-solid)",
            "beyaz simge #ef4444 üzerinde 3,76:1; --error-solid ile 4,83:1");
    }

    [Fact]
    public void Baglanti_afisi_dolgusunu_ve_metnini_tokendan_alir()
    {
        var afis = RuleBody(ChatCss(), ".conn-status");

        afis.Should().Contain("var(--warning-solid)");
        afis.Should().Contain("var(--on-accent)");
        afis.Should().NotContain("#f59e0b");
    }

    [Fact]
    public void Temaya_uyan_modal_kartlarinin_golgesi_tokendan_gelir()
    {
        var css = ChatCss();

        foreach (var kart in new[] { ".dev-card", ".consent-modal", ".agreement-card", ".profile-card", ".ask-card" })
        {
            RuleBody(css, kart).Should().Contain("var(--shadow-overlay)",
                $"'{kart}' tema yüzeyinde duruyor; sabit %60 siyah gölge açık temada leke bırakır");
        }
    }

    [Fact]
    public void Sunum_katmani_tarayicinin_kendi_kutularini_kullanmaz()
    {
        var kok = FindSolutionRoot();
        var kutu = new Regex(@"\bwindow\.(?:confirm|prompt|alert)\s*\(|(?<![\w.$])(?:confirm|prompt|alert)\s*\(");
        var sapan = new List<string>();
        var taranan = 0;

        foreach (var dosya in Directory.EnumerateFiles(
                     Path.Combine(kok, "Presentation"), "*.js", SearchOption.AllDirectories))
        {
            if (dosya.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}")) continue;

            taranan++;
            var satirlar = File.ReadAllLines(dosya);
            for (var i = 0; i < satirlar.Length; i++)
                if (kutu.IsMatch(satirlar[i]))
                    sapan.Add($"{Path.GetFileName(dosya)}:{i + 1} → {satirlar[i].Trim()}");
        }

        taranan.Should().BeGreaterThan(10, "tarama boş dönerse bu test hiçbir şey doğrulamıyor");

        sapan.Should().BeEmpty(
            "tarayıcının confirm/prompt/alert kutuları sayfayı kilitler, tasarımın dışında durur ve " +
            "odak/klavye davranışımızı takip etmez; yerlerine askConfirm/askPrompt kullanılır:" +
            Environment.NewLine + string.Join(Environment.NewLine, sapan));
    }
}
