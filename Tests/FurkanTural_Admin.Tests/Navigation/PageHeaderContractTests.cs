using System.Text.RegularExpressions;
using FluentAssertions;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Navigation;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Liste sayfasının üst bandı: başlık kırıntı yoluyla aynı adı söylemeli, her modülün şema sayfasına bir bağlantısı olmalı ve açıklamada veri tabanının İngilizce tablo adı geçmemeli. Üçü de sayfa sayfa elle yazıldığı için kayma kaynağıdır.</summary>
public class PageHeaderContractTests
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

    private static string IndexPath(string controller) =>
        Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "Views", controller, "Index.cshtml");

    private static IEnumerable<(AdminModule Module, string Content)> IndexViews()
    {
        foreach (var module in AdminModules.All)
        {
            var path = IndexPath(module.Controller);
            if (File.Exists(path))
                yield return (module, File.ReadAllText(path));
        }
    }

    private static string? Heading(string content)
    {
        var match = Regex.Match(content, @"<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline);
        return match.Success ? Regex.Replace(match.Groups[1].Value, @"<[^>]+>", string.Empty).Trim() : null;
    }

    private static string Description(string content)
    {
        var match = Regex.Match(content, @"<p class=""section-desc""[^>]*>(.*?)</p>", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    [Fact]
    public void Yirmi_bir_modulun_liste_sayfasi_vardir()
    {
        IndexViews().Should().HaveCount(21);
    }

    [Fact]
    public void Baslik_kirinti_yoluyla_ayni_adi_soyler()
    {
        var sapan = new List<string>();

        foreach (var (module, content) in IndexViews())
        {
            var heading = Heading(content);

            if (heading != module.Title && heading != $"{module.Title} Verileri")
                sapan.Add($"{module.Controller}: kırıntı yolu \"{module.Title}\", başlık \"{heading}\"");
        }

        sapan.Should().BeEmpty(
            "kırıntı yolu kayıttan okunuyor, başlık elle yazılıyor; ikisi ayrışırsa aynı ekranda modülün iki adı görünür");
    }

    [Fact]
    public void Her_modulun_sema_sayfasina_baglantisi_vardir()
    {
        var eksik = IndexViews()
            .Where(v => !v.Content.Contains(@"Url.Action(""TableDetail"")"))
            .Select(v => v.Module.Controller)
            .ToList();

        eksik.Should().BeEmpty("TableDetail her modülde çiziliyor; bağlantısı olmayan sayfaya hiçbir yerden gidilemez");
    }

    [Fact]
    public void Aciklamada_veri_tabaninin_ingilizce_tablo_adi_gecmez()
    {
        var sizan = new List<string>();

        foreach (var (module, content) in IndexViews())
        {
            var description = Description(content);
            var englishTable = $"{module.Entity}s tablosundaki";

            if (description.Contains(englishTable, StringComparison.OrdinalIgnoreCase))
                sizan.Add($"{module.Controller}: \"{englishTable}\"");
        }

        sizan.Should().BeEmpty("açıklama kullanıcıya görünür; tablo adı Türkçe modül adıyla anılmalı");
    }

    [Fact]
    public void Aciklamada_bilinen_yanlis_yazimlar_gecmez()
    {
        string[] yanlisYazimlar = ["Porfolio", "Protfolio", "Porfolyo"];

        var bulunan = new List<string>();

        foreach (var (module, content) in IndexViews())
        {
            var description = Description(content);

            foreach (var yanlis in yanlisYazimlar)
            {
                if (description.Contains(yanlis, StringComparison.Ordinal))
                    bulunan.Add($"{module.Controller}: \"{yanlis}\"");
            }
        }

        bulunan.Should().BeEmpty("ev yazımı \"portfolyo\"");
    }
}
