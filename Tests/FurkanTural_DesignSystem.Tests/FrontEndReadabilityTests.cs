using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

public class FrontEndReadabilityTests
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

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), .. parts]));

    private static string BlogCss() => Read("Presentation", "FurkanTural_Blog", "wwwroot", "css", "site.css");
    private static string PortfolioCss() => Read("Presentation", "FurkanTural_Portfolio", "wwwroot", "css", "site.css");

    private static readonly (string Project, string Prefix, string ThemeFile)[] Sites =
    [
        ("FurkanTural_Admin",     "--",       "theme.css"),
        ("FurkanTural_Chat",      "--",       "theme.css"),
        ("FurkanTural_Blog",      "--",       "site.css"),
        ("FurkanTural_Portfolio", "--color-", "site.css"),
    ];

    private static string CssOf(string project) =>
        project == "Blog" ? BlogCss() : PortfolioCss();

    private static string CssRoot(string project) =>
        Path.Combine(FindSolutionRoot(), "Presentation", project, "wwwroot", "css");

    private static IEnumerable<string> StyleSheets(string project) =>
        Directory.EnumerateFiles(CssRoot(project), "*.css", SearchOption.AllDirectories);

    private static string ThemeCss(string project, string file) =>
        File.ReadAllText(Path.Combine(CssRoot(project), file));

    [Fact]
    public void Aksan_rengi_dogrudan_metin_rengi_olarak_kullanilmaz()
    {
        var sapan = new List<string>();

        foreach (var (project, _, _) in Sites)
        {
            foreach (var path in StyleSheets(project))
            {
                var css = File.ReadAllText(path);

                foreach (var token in new[] { "--accent", "--accent-hover", "--color-accent", "--color-accent-hover" })
                {
                    var pattern = $@"(^|[^-\w])color:\s*var\({Regex.Escape(token)}\)";
                    foreach (Match m in Regex.Matches(css, pattern, RegexOptions.Multiline))
                    {
                        var line = css[..m.Index].Count(c => c == '\n') + 1;
                        sapan.Add($"{project}/{Path.GetFileName(path)}:{line} → color: var({token})");
                    }
                }
            }
        }

        sapan.Should().BeEmpty(
            "açık temada aksan #0ea5e9 beyaz zeminde 2.65:1, üzerine gelme rengi #0284c7 ise 4.09:1 kalır; " +
            "metin için ayrılmış accent-text token'ı kullanılmalı");
    }

    [Fact]
    public void Metin_icin_aksan_tokenlari_iki_temada_da_tanimlidir()
    {
        var eksik = new List<string>();

        foreach (var (project, prefix, themeFile) in Sites)
        {
            var css = ThemeCss(project, themeFile);

            var gerekli = new List<string> { $"{prefix}accent-text" };

            if (Regex.IsMatch(css, $@"^\s*{Regex.Escape($"{prefix}accent-hover")}\s*:", RegexOptions.Multiline))
                gerekli.Add($"{prefix}accent-text-hover");

            foreach (var token in gerekli)
            {
                var count = Regex.Matches(css, $@"^\s*{Regex.Escape(token)}\s*:", RegexOptions.Multiline).Count;
                if (count < 2)
                    eksik.Add($"{project} → {token} yalnızca {count} yerde tanımlı (koyu + açık gerekiyor)");
            }
        }

        eksik.Should().BeEmpty("token yalnızca bir temada tanımlanırsa diğer tema eski değeri miras alır ve düzeltme yarım kalır");
    }

    [Fact]
    public void Portfolio_acik_temasi_anlamsal_renkleri_yeniden_tanimlar()
    {
        var css = PortfolioCss();
        var light = Regex.Match(css, @"\[data-theme=""light""\]\s*\{(.*?)\}", RegexOptions.Singleline);

        light.Success.Should().BeTrue("açık tema bloğu bulunamadı");

        var eksik = new[] { "--color-success", "--color-danger", "--color-warning" }
            .Where(t => !Regex.IsMatch(light.Groups[1].Value, $@"^\s*{Regex.Escape(t)}\s*:", RegexOptions.Multiline))
            .ToList();

        eksik.Should().BeEmpty(
            "koyu tema değerleri beyaz zemine düşüyordu: 'Yeni projelere açık' rozeti 1.59:1, zorunlu alan yıldızı 2.64:1");
    }

    [Fact]
    public void Aksan_zemine_beyaz_metin_yazilmaz()
    {
        var sapan = new List<string>();

        foreach (var (project, _, _) in Sites)
        {
            foreach (var path in StyleSheets(project))
            {
                var css = File.ReadAllText(path);

                foreach (Match m in Regex.Matches(css, @"[^\n]*background:\s*var\(--(color-)?accent\)[^\n]*", RegexOptions.Multiline))
                {
                    if (Regex.IsMatch(m.Value, @"(^|[^-\w])color:\s*(#fff\b|#ffffff\b|white\b)", RegexOptions.IgnoreCase))
                        sapan.Add($"{project}/{Path.GetFileName(path)} → {m.Value.Trim()}");
                }
            }
        }

        sapan.Should().BeEmpty(
            "beyaz metin aksan dolgusunda koyu temada 2.14:1, açık temada 2.77:1 verir; --on-accent kullanılmalı");
    }

    private static double Channel(int v)
    {
        var c = v / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static double Luminance(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 3)
            h = string.Concat(h.Select(c => new string(c, 2)));

        return 0.2126 * Channel(Convert.ToInt32(h[..2], 16))
             + 0.7152 * Channel(Convert.ToInt32(h.Substring(2, 2), 16))
             + 0.0722 * Channel(Convert.ToInt32(h.Substring(4, 2), 16));
    }

    private static double Contrast(string a, string b)
    {
        double l1 = Luminance(a), l2 = Luminance(b);
        if (l1 < l2) (l1, l2) = (l2, l1);
        return (l1 + 0.05) / (l2 + 0.05);
    }

    private static Dictionary<string, string> ThemeTokens(string css, string anchor, bool light)
    {
        var map = new Dictionary<string, string>();

        foreach (Match block in Regex.Matches(css, @"([^{}]*)\{([^{}]*)\}", RegexOptions.Singleline))
        {
            var selector = block.Groups[1].Value;
            var body = block.Groups[2].Value;

            if (!Regex.IsMatch(body, $@"^\s*{Regex.Escape(anchor)}\s*:", RegexOptions.Multiline))
                continue;

            if (selector.Contains("light") != light)
                continue;

            foreach (Match m in Regex.Matches(body, @"^\s*(--[a-z0-9-]+)\s*:\s*(#[0-9a-fA-F]{3,8})\s*;", RegexOptions.Multiline))
                map[m.Groups[1].Value] = m.Groups[2].Value;
        }

        return map;
    }

    [Theory]
    [InlineData("FurkanTural_Admin", "theme.css", "--accent", "--on-accent")]
    [InlineData("FurkanTural_Chat", "theme.css", "--accent", "--on-accent")]
    [InlineData("FurkanTural_Blog", "site.css", "--accent", "--on-accent")]
    [InlineData("FurkanTural_Blog", "site.css", "--accent-hover", "--on-accent")]
    [InlineData("FurkanTural_Portfolio", "site.css", "--color-accent", "--color-on-accent")]
    [InlineData("FurkanTural_Portfolio", "site.css", "--color-accent-hover", "--color-on-accent")]
    public void Aksan_dolgusunun_uzerindeki_metin_esigi_gecer(string project, string themeFile, string fill, string on)
    {
        var css = ThemeCss(project, themeFile);

        var dark = ThemeTokens(css, on, light: false);
        var light = ThemeTokens(css, on, light: true);

        dark.Should().ContainKey(fill, "koyu tema bloğu okunamadıysa test bir şey doğrulamıyor demektir");
        dark.Should().ContainKey(on);
        light.Should().NotBeEmpty("açık tema bloğu bulunamadıysa test yalnızca koyu temayı denetliyor demektir");

        foreach (var (tema, harita) in new[] { ("koyu", dark), ("açık", light) })
        {
            var dolgu = harita.GetValueOrDefault(fill, dark[fill]);
            var metin = harita.GetValueOrDefault(on, dark[on]);

            Contrast(metin, dolgu).Should().BeGreaterThanOrEqualTo(4.5,
                $"{project} {tema} temada {on} ({metin}) rengi {fill} ({dolgu}) dolgusunun üzerinde okunmalı");
        }
    }

    [Fact]
    public void Blog_on_accent_tokenini_gercekten_kullanir()
    {
        var css = BlogCss();
        var usage = Regex.Matches(css, @"var\(--on-accent\)").Count;

        usage.Should().BeGreaterThan(0,
            "token dört projeye eklendi ama Blog'da hiçbir çağrı yerine bağlanmamıştı; tanımlanıp kullanılmayan token düzeltme sanılır");
    }

    [Fact]
    public void Kategori_cipi_veriden_gelen_rengi_dogrudan_metin_yapmaz()
    {
        var css = BlogCss();
        var sapan = new List<string>();

        foreach (Match m in Regex.Matches(css, @"(^|[^-\w])color:\s*var\(--chip-color[^)]*\)[^;]*;", RegexOptions.Multiline))
            sapan.Add(m.Value.Trim());

        sapan.Should().BeEmpty(
            "renk yöneticinin girdiği serbest bir değer; açık temada '.NET' çipi 1.18:1 ile görünmez oluyordu. " +
            "Metin --text'e doğru karıştırılmalı, ham renk noktada ve kenarlıkta kalmalı");
    }

    [Fact]
    public void Blog_kategori_cipleri_baglanti_olarak_duyurulur()
    {
        var view = Read("Presentation", "FurkanTural_Blog", "Views", "Home", "Index.cshtml");

        view.Should().NotContain("role=\"listitem\"",
            "ARIA rolü <a> üzerinde örtük 'link' rolünü ezer; ekran okuyucu ögenin tıklanabilir olduğunu söylemez");
    }

    [Fact]
    public void Portfolio_ana_sayfa_basligi_yinelemez()
    {
        var index = Read("Presentation", "FurkanTural_Portfolio", "Views", "Home", "Index.cshtml");
        var layout = Read("Presentation", "FurkanTural_Portfolio", "Views", "Shared", "_Layout.cshtml");

        layout.Should().Contain("ViewData[\"Title\"] + \" | Furkan Tural\"",
            "bu test düzenin başlığı nasıl kurduğuna dayanıyor; kalıp değiştiyse test de güncellenmeli");

        index.Should().NotContain("ViewData[\"Title\"] = \"Furkan Tural\"",
            "düzen site adını zaten ekliyor; ana sayfada ayrıca vermek sekme adını 'Furkan Tural | Furkan Tural' yapar");
    }

    [Fact]
    public void Turnstile_kutusu_bulundugu_her_formda_ortalanir()
    {
        var root = FindSolutionRoot();
        var sapan = new List<string>();

        var stylesheets = new Dictionary<string, string>
        {
            ["FurkanTural_Chat"] = Path.Combine("wwwroot", "css", "chat.css"),
            ["FurkanTural_Portfolio"] = Path.Combine("wwwroot", "css", "site.css"),
            ["FurkanTural_Blog"] = Path.Combine("wwwroot", "css", "site.css"),
            ["FurkanTural_Admin"] = Path.Combine("wwwroot", "css", "site.css"),
        };

        foreach (var (project, relative) in stylesheets)
        {
            var views = Path.Combine(root, "Presentation", project, "Views");
            if (!Directory.Exists(views)) continue;

            var kullanan = Directory
                .EnumerateFiles(views, "*.cshtml", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("cf-turnstile"))
                .ToList();

            if (kullanan.Count == 0) continue;

            var css = File.ReadAllText(Path.Combine(root, "Presentation", project, relative));
            var rule = Regex.Match(css, @"[^\n{}]*\.cf-turnstile[^\n{}]*\{([^}]*)\}");

            if (!rule.Success)
            {
                sapan.Add($"{project}: {kullanan.Count} görünümde widget var ama .cf-turnstile kuralı yok");
                continue;
            }

            var body = rule.Groups[1].Value;
            var ortalar = Regex.IsMatch(body, @"justify-content:\s*center")
                       || Regex.IsMatch(body, @"margin:\s*[^;]*auto")
                       || Regex.IsMatch(body, @"text-align:\s*center");

            if (!ortalar)
                sapan.Add($"{project}: .cf-turnstile kuralı ortalama yapmıyor → {body.Trim()}");
        }

        sapan.Should().BeEmpty(
            "Cloudflare widget'ı sabit 300px genişlikte bir iframe çizer; kapsayıcı formdan dar kaldığı için " +
            "ortalanmazsa sola yapışır ve formun geri kalanıyla hizasız durur");
    }

    [Fact]
    public void Api_arizasi_gercekten_bos_icerikten_ayrilir()
    {
        var sapan = new List<string>();

        var blogView = Read("Presentation", "FurkanTural_Blog", "Views", "Home", "Index.cshtml");
        if (!blogView.Contains("Model.LoadFailed"))
            sapan.Add("Blog: liste görünümü LoadFailed dalını taşımıyor");

        var blogService = Read("Presentation", "FurkanTural_Blog", "Services", "BlogApiService.cs");
        if (!blogService.Contains("LoadFailed = true"))
            sapan.Add("Blog: servis yakalama bloğunda LoadFailed işaretlenmiyor");

        var portfolioService = Read("Presentation", "FurkanTural_Portfolio", "Services", "PortfolioApiService.cs");
        var catches = Regex.Matches(portfolioService, @"catch \(Exception ex\)").Count;
        var marks = Regex.Matches(portfolioService, @"AnyRequestFailed = true;").Count;
        if (catches != marks)
            sapan.Add($"Portfolio: {catches} yakalama bloğuna karşılık {marks} işaretleme var");

        var controller = Read("Presentation", "FurkanTural_Portfolio", "Controllers", "HomeController.cs");
        if (!controller.Contains("ViewData[\"ApiUnavailable\"]"))
            sapan.Add("Portfolio: denetleyici bayrağı görünüme geçirmiyor");

        foreach (var section in new[] { "Skills", "Projects", "Songs", "Experience", "Education" })
        {
            var view = Read("Presentation", "FurkanTural_Portfolio", "Views", "Home", $"_{section}Section.cshtml");
            if (!view.Contains("_SectionEmpty"))
                sapan.Add($"Portfolio: _{section}Section ortak boş-durum kısmını kullanmıyor");
        }

        sapan.Should().BeEmpty(
            "API kapalıyken her iki site de 'içerik hazırlanıyor' diyordu; ziyaretçi geçici arızayı kalıcı boşluk sanıyordu");
    }
}
