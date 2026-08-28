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

    private static string ChatView(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "Views", .. parts]));

    private static IEnumerable<string> ContentViews()
    {
        var root = Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "Views");

        return Directory
            .EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith('_'));
    }

    [Fact]
    public void Hicbir_gorunum_kendi_yuzen_tema_dugmesini_tasimaz()
    {
        var sapan = ContentViews()
            .Where(f => File.ReadAllText(f).Contains("theme-toggle-btn--floating"))
            .Select(Path.GetFileName)
            .ToList();

        sapan.Should().BeEmpty(
            "tema düğmesi ortak üst çubuğa taşındı; sayfa başına kopyalanan düğme sağ üst köşede " +
            "üst çubuğun üstüne biniyordu");

        ContentViews().Should().HaveCountGreaterThan(5,
            "görünüm taraması boş dönerse bu test hiçbir şey doğrulamıyor demektir");
    }

    [Fact]
    public void Duzen_ust_cubugu_her_sayfada_alt_bilgiyi_yalnizca_icerik_sayfalarinda_cizer()
    {
        var layout = ChatView("Shared", "_Layout.cshtml");

        layout.Should().Contain("Html.PartialAsync(\"_TopBar\")",
            "üst çubuk düzende değilse her görünüme tek tek eklenmesi gerekir");

        var footer = Regex.Match(layout, @"@if \(!appShell\)\s*\{\s*@await Html\.PartialAsync\(""_Footer""\)");

        footer.Success.Should().BeTrue(
            "alt bilgi koşulsuz çizilirse sabit yükseklikli sohbet ekranında kaydırma alanını yer");
    }

    [Fact]
    public void Sohbet_ekrani_uygulama_kabugunu_secer()
    {
        ChatView("Chat", "Index.cshtml").Should().Contain("ViewData[\"Shell\"] = \"app\"",
            "bayrak düşerse sohbet ekranı alt bilgiyi de çizer ve .chat-app kalan yüksekliği aşar");
    }

    [Fact]
    public void Ust_cubuk_tema_dugmesini_yalnizca_icerik_sayfalarinda_cizer()
    {
        var topbar = ChatView("Shared", "_TopBar.cshtml");

        topbar.Should().Contain("if (!appShell)",
            "sohbet ekranının alt kullanıcı çubuğu zaten bir tema düğmesi taşıyor; " +
            "üst çubuk da çizerse aynı ekranda iki özdeş denetim olur");

        var chat = ChatView("Chat", "Index.cshtml");

        Regex.Matches(chat, "data-theme-toggle").Count.Should().Be(1,
            "sohbet ekranında tek tema düğmesi olmalı");
    }

    [Fact]
    public void Ust_cubuk_yuksekligi_sayfadan_sayfaya_degismez()
    {
        RuleBody(ChatCss("chat.css"), ".topbar").Should().Contain("min-height",
            "çubuğun yüksekliği içeriğinden geliyordu: giriş düğmesi olmayan sohbet ekranında 53px, " +
            "olan içerik sayfalarında 61px ölçüldü; gezinirken marka satırı zıplıyordu");
    }

    [Fact]
    public void Sayfa_basligi_site_adini_yinelemez()
    {
        var layout = ChatView("Shared", "_Layout.cshtml");

        layout.Should().NotContain("<title>@ViewData[\"Title\"] - Chatural</title>",
            "açılış sayfası başlığını 'Chatural' olarak veriyordu; düzen site adını ekleyince " +
            "sekmede 'Chatural - Chatural' yazıyordu");
    }

    [Fact]
    public void Giris_sayfasi_ust_cubukta_kayit_onerir()
    {
        ChatView("Account", "Login.cshtml").Should().Contain("ViewData[\"TopBarAction\"] = \"register\"",
            "giriş sayfasının üst çubuğunda 'Giriş yap' düğmesi kullanıcıyı bulunduğu sayfaya gönderir");
    }
}
