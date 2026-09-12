using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Liste gezinmesi ortak bir betikle yürüyor ve o betik sayfayı DOM'daki bir bildirimden tanıyor. Bildirim eksikse sayfa sessizce eski davranışa döner: hata vermez, yalnızca her filtre ve sayfa değişiminde tüm belge yeniden yüklenir.<para>Bu testler bildirimin 24 listede de bulunduğunu, gösterdiği yerlerin gerçekten var olduğunu ve tekrarlanan tazeleme kodunun geri sızmadığını doğrular.</para></summary>
public class ListNavContractTests
{
    private const string SolutionMarker = "FurkanTural.slnx";
    private const int ListCount = 24;

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

    private static string AdminPath(params string[] parts) =>
        Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Admin", .. parts]);

    private static IEnumerable<(string Module, string Path, string Content)> ListPages()
    {
        var views = AdminPath("Views");

        foreach (var path in Directory.EnumerateFiles(views, "_*Table.cshtml", SearchOption.AllDirectories))
        {
            var module = Path.GetFileName(Path.GetDirectoryName(path))!;
            var index = Path.Combine(Path.GetDirectoryName(path)!, "Index.cshtml");
            if (!File.Exists(index)) continue;

            yield return (module, index, File.ReadAllText(index));
        }
    }

    [Fact]
    public void Yirmi_dort_liste_kendini_ortak_betige_tanitir()
    {
        var pages = ListPages().ToList();
        pages.Should().HaveCount(ListCount);

        var eksik = pages
            .Where(p => !p.Content.Contains($"data-list-controller=\"{p.Module}\""))
            .Select(p => p.Module)
            .ToList();

        eksik.Should().BeEmpty(
            "bildirimi olmayan liste ortak betiğe görünmez olur ve her sayfa değişiminde tüm belgeyi yeniden yükler");
    }

    [Fact]
    public void Bildirilen_meta_globali_ayni_sayfada_tanimlidir()
    {
        var sapan = new List<string>();

        foreach (var (module, _, content) in ListPages())
        {
            var declared = Regex.Match(content, @"data-list-meta=""(?<ad>__\w+Meta)""");
            if (!declared.Success)
            {
                sapan.Add($"{module}: data-list-meta yok");
                continue;
            }

            var name = declared.Groups["ad"].Value;
            if (!content.Contains($"window.{name}"))
                sapan.Add($"{module}: \"{name}\" aynı sayfada tanımlı değil");
        }

        sapan.Should().BeEmpty(
            "ortak betik sayfa durumunu bu globale yazar; ad tutmazsa kayıt silindikten sonraki tazeleme süzgeci ve sayfayı düşürür");
    }

    [Fact]
    public void Bildirilen_bolum_sayfada_gercekten_vardir()
    {
        var sapan = new List<string>();

        foreach (var (module, _, content) in ListPages())
        {
            var mark = Regex.Match(content, @"<div id=""(?<id>[\w-]+)""\s+data-list-controller=");
            if (!mark.Success)
                sapan.Add($"{module}: bildirim tablo bölümünün div'inde değil");
        }

        sapan.Should().BeEmpty("bildirim tazelenecek bölümün üzerinde durmalı; başka bir düğümde işe yaramaz");
    }

    [Fact]
    public void Her_liste_sayac_kanalini_yayar()
    {
        var views = AdminPath("Views");

        var eksik = Directory
            .EnumerateFiles(views, "_*Table.cshtml", SearchOption.AllDirectories)
            .Where(path => !File.ReadAllText(path).Contains("id=\"__list-stats-json\""))
            .Select(path => Path.GetFileName(Path.GetDirectoryName(path))!)
            .ToList();

        eksik.Should().BeEmpty(
            "sayaç kutuları tazelenen bölgenin dışında; kanal olmazsa liste değişir ama sayaçlar eski değerde kalır");
    }

    [Fact]
    public void Sayfadaki_her_sayac_anahtari_kanalda_karsilik_bulur()
    {
        var sapan = new List<string>();

        foreach (var (module, path, content) in ListPages())
        {
            var table = Path.Combine(Path.GetDirectoryName(path)!, $"_{module}Table.cshtml");
            if (!File.Exists(table)) continue;

            var channel = File.ReadAllText(table);

            foreach (Match used in Regex.Matches(content, @"data-stat=""(?<ad>\w+)"""))
            {
                var key = used.Groups["ad"].Value;
                if (!Regex.IsMatch(channel, $@"\b{Regex.Escape(key)}\s*="))
                    sapan.Add($"{module}: \"{key}\" sayaç kanalında yok");
            }
        }

        sapan.Should().BeEmpty("kanalda karşılığı olmayan anahtar sessizce güncellenmez; kutu eski değerinde donar");
    }

    [Fact]
    public void Sayfa_betikleri_kendi_tazeleme_kodunu_tasimaz()
    {
        var pages = AdminPath("wwwroot", "js", "pages");

        var sapan = Directory
            .EnumerateFiles(pages, "*.js")
            .Where(path => File.ReadAllText(path).Contains("TablePartial"))
            .Select(Path.GetFileName)
            .ToList();

        sapan.Should().BeEmpty(
            "tazeleme ortak betiğin işi; sayfa betiğine geri kopyalanırsa yirmi dört ayrı kopya yeniden doğar");
    }

    [Fact]
    public void Ortak_betik_her_sayfada_yuklenir()
    {
        var layout = File.ReadAllText(AdminPath("Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("js/list-nav.js",
            "betik ortak yerleşimden gelir; sayfa başına bağlanırsa bir listede unutulması kimseye fark ettirmez");
    }

    [Fact]
    public void Sayaca_bagli_ogeler_sifirken_de_sayfada_durur()
    {
        var sapan = new List<string>();

        foreach (var (module, _, content) in ListPages())
        {
            foreach (Match marked in Regex.Matches(content, @"data-stat-when=""(?<ad>\w+)"""))
            {
                var key = marked.Groups["ad"].Value;
                var start = content.LastIndexOf('<', marked.Index);
                var end = content.IndexOf('>', marked.Index);
                var openTag = start >= 0 && end > start ? content[start..end] : string.Empty;

                if (!openTag.Contains("hidden="))
                    sapan.Add($"{module}: \"{key}\" öğesinde hidden bağı yok");
            }

            foreach (Match conditional in Regex.Matches(content, @"@if \(Model\.\w*Count [<>=!]+ 0\)"))
                sapan.Add($"{module}: sayaç koşulu görünümde kalmış — {conditional.Value}");
        }

        sapan.Should().BeEmpty(
            "sayaca bağlı öğe sunucuda koşullu çizilirse sıfırken hiç var olmaz; tazeleme onu geri getiremez, çünkü güncellenecek bir düğüm yoktur");
    }

    [Fact]
    public void Sayaca_bagli_her_anahtar_kanalda_karsilik_bulur()
    {
        var sapan = new List<string>();

        foreach (var (module, path, content) in ListPages())
        {
            var table = Path.Combine(Path.GetDirectoryName(path)!, $"_{module}Table.cshtml");
            if (!File.Exists(table)) continue;

            var channel = File.ReadAllText(table);

            foreach (Match marked in Regex.Matches(content, @"data-stat-when=""(?<ad>\w+)"""))
            {
                var key = marked.Groups["ad"].Value;
                if (!Regex.IsMatch(channel, $@"\b{Regex.Escape(key)}\s*="))
                    sapan.Add($"{module}: \"{key}\" sayaç kanalında yok");
            }
        }

        sapan.Should().BeEmpty("kanalda karşılığı olmayan anahtar öğeyi ne gösterir ne gizler; öğe ilk hâlinde donar");
    }
}
