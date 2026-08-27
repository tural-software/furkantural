using System.Text.RegularExpressions;
using FluentAssertions;
using FurkanTural_Admin.Helpers;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Liste alt bandı, süzgeçleri sayfa bağlantısına ad-değer olarak taşır. Ad, alt listenin Index metodunda gerçekten karşılığı olan bir parametre değilse sayfa değişince süzgeç sessizce düşer — hata da vermez. Bu testler her modülün verdiği anahtarları kendi controller'ının imzasıyla karşılaştırır.</summary>
public class TableFilterKeyTests
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

    private static IEnumerable<(string Controller, IReadOnlyList<string> Keys)> FooterFilters()
    {
        var views = Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "Views");

        foreach (var path in Directory.EnumerateFiles(views, "_*Table.cshtml", SearchOption.AllDirectories))
        {
            var controller = Path.GetFileName(Path.GetDirectoryName(path))!;
            var content = File.ReadAllText(path);

            var call = Regex.Match(
                content,
                @"Html\.PartialAsync\(""_TableFooter"".*?\[(?<liste>.*?)\]\)\)",
                RegexOptions.Singleline);

            var keys = call.Success
                ? Regex.Matches(call.Groups["liste"].Value, @"new\(\s*""(?<ad>[^""]+)""")
                       .Select(m => m.Groups["ad"].Value)
                       .ToArray()
                : [];

            yield return (controller, keys);
        }
    }

    [Fact]
    public void Yirmi_bir_listenin_hepsi_alt_banda_suzgec_verir()
    {
        var bos = FooterFilters().Where(f => f.Keys.Count == 0).Select(f => f.Controller).ToList();

        bos.Should().BeEmpty("süzgeç listesi okunamayan partial, aşağıdaki denetimden sessizce kaçar");
        FooterFilters().Should().HaveCount(21);
    }

    [Fact]
    public void Her_suzgec_anahtari_controllerin_gercek_parametresidir()
    {
        var sapan = new List<string>();

        foreach (var (controller, keys) in FooterFilters())
        {
            var type = typeof(AdminModules).Assembly
                .GetTypes()
                .SingleOrDefault(t => t.Name == $"{controller}Controller");

            if (type is null)
            {
                sapan.Add($"{controller}Controller bulunamadı");
                continue;
            }

            var parametreler = type.GetMethod("Index")?
                .GetParameters()
                .Select(p => p.Name)
                .ToArray() ?? [];

            foreach (var key in keys.Where(k => !parametreler.Contains(k)))
                sapan.Add($"{controller}: \"{key}\" bir Index parametresi değil");
        }

        sapan.Should().BeEmpty(
            "anahtar imzada yoksa sayfa değişince o süzgeç düşer; kullanıcı tüm kayıtları görür ve uyarı almaz");
    }

    [Fact]
    public void Sayfa_numarasi_ve_boyutu_suzgec_listesinde_yer_almaz()
    {
        var sapan = FooterFilters()
            .SelectMany(f => f.Keys
                .Where(k => k is "pageNumber" or "pageSize")
                .Select(k => $"{f.Controller}: \"{k}\""))
            .ToList();

        sapan.Should().BeEmpty("sayfa durumunu ortak parça kendi ekler; listede de olursa iki kez yazılır");
    }
}
