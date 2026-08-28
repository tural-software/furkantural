using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

public class PublicSiteReadabilityTests
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

    private static readonly (string Project, string Prefix)[] Sites =
    [
        ("Blog",      "--"),
        ("Portfolio", "--color-"),
    ];

    private static string CssOf(string project) => project == "Blog" ? BlogCss() : PortfolioCss();

    [Fact]
    public void Aksan_rengi_dogrudan_metin_rengi_olarak_kullanilmaz()
    {
        var sapan = new List<string>();

        foreach (var (project, prefix) in Sites)
        {
            var css = CssOf(project);

            foreach (var token in new[] { $"{prefix}accent", $"{prefix}accent-hover" })
            {
                var pattern = $@"(^|[^-\w])color:\s*var\({Regex.Escape(token)}\)";
                foreach (Match m in Regex.Matches(css, pattern, RegexOptions.Multiline))
                {
                    var line = css[..m.Index].Count(c => c == '\n') + 1;
                    sapan.Add($"{project} site.css:{line} → color: var({token})");
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

        foreach (var (project, prefix) in Sites)
        {
            var css = CssOf(project);

            foreach (var token in new[] { $"{prefix}accent-text", $"{prefix}accent-text-hover" })
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

        foreach (var (project, _) in Sites)
        {
            var css = CssOf(project);

            foreach (Match m in Regex.Matches(css, @"[^\n]*background:\s*var\(--(color-)?accent\)[^\n]*", RegexOptions.Multiline))
            {
                if (Regex.IsMatch(m.Value, @"(^|[^-\w])color:\s*(#fff\b|#ffffff\b|white\b)", RegexOptions.IgnoreCase))
                    sapan.Add($"{project} → {m.Value.Trim()}");
            }
        }

        sapan.Should().BeEmpty(
            "beyaz metin aksan dolgusunda koyu temada 2.14:1, açık temada 2.77:1 verir; --on-accent kullanılmalı");
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
