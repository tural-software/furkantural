using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Sohbet kabuğunun iki kuralı: bağlantı afişi yüzen bir katman değil yerleşimin kendi satırıdır (yüzdüğünde masaüstünde sohbet başlığını, telefonda arama kutusunu örtüyordu) ve dolu kırmızı yüzeylerde beyaz metnin okunabildiği koyu ton kullanılır.</summary>
public class ChatShellContractTests
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

    private static string ChatCss(string file) =>
        File.ReadAllText(Path.Combine(
            FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "wwwroot", "css", file));

    private static string RuleBody(string css, string selector)
    {
        var match = Regex.Match(css, @"(?<![\w-])" + Regex.Escape(selector) + @"\s*\{([^{}]*)\}");

        match.Success.Should().BeTrue($"'{selector}' kuralı bulunamadı");
        return match.Groups[1].Value;
    }

    [Fact]
    public void Baglanti_afisi_icerigin_ustunde_yuzmez()
    {
        var body = RuleBody(ChatCss("chat.css"), ".conn-status");

        body.Should().NotContain("position: fixed",
            "yüzen afiş sayfanın üst ortasına çakılıydı; masaüstünde sohbet başlığını, telefonda arama kutusunu örtüyordu");
        body.Should().Contain("grid-row: 1",
            "afiş kendi satırında durmalı ki göründüğünde içeriği örtmek yerine aşağı itsin");
    }

    [Fact]
    public void Iki_pane_de_afisin_altindaki_satira_sabitlenmistir()
    {
        var css = ChatCss("chat.css");

        RuleBody(css, ".sidebar").Should().Contain("grid-row: 2",
            "afiş gizliyken üst satır sıfıra iner; pane'ler sabitlenmezse afişin satırına düşer");
        RuleBody(css, ".conversation").Should().Contain("grid-row: 2",
            "telefonda pane'ler position:absolute; kapsayıcı blokları grid alanı olduğu için satır ataması "
          + "onları afişin altında tutan tek şey");
    }

    [Fact]
    public void Dolu_kirmizi_yuzeyler_koyu_tonu_kullanir()
    {
        var css = ChatCss("chat.css");

        var sapan = Regex.Matches(css, @"background:[^;]*var\(--error\)[^;]*;[^}]*color:\s*#fff")
            .Select(m => m.Value.Replace("\n", " "))
            .ToList();

        sapan.Should().BeEmpty(
            "beyaz metin --error (#ef4444) üzerinde 3,76:1 kalıyor; dolu yüzeyler --error-solid ile 4,83:1 oluyor");

        ChatCss("theme.css").Should().Contain("--error-solid",
            "--error metin rengi olarak da kullanılıyor, koyulaştırılamaz; dolu yüzeyler için ayrı ton gerekir");
    }
}
