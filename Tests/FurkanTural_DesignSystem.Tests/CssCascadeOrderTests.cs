using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Medya sorgusu özgüllük eklemez. Bir <c>@media</c> bloğu, ezmek istediği taban kuralından ÖNCE duruyorsa taban kural kazanır ve duyarlı düzeltme sessizce ölür. Kural yazılıdır ama çalışmaz; tarayıcı uyarı vermez.</summary>
public class CssCascadeOrderTests
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

    private static IEnumerable<string> Stylesheets()
    {
        var root = Path.Combine(FindSolutionRoot(), "Presentation");

        foreach (var project in new[] { "FurkanTural_Admin", "FurkanTural_Chat", "FurkanTural_Blog", "FurkanTural_Portfolio" })
        {
            var css = Path.Combine(root, project, "wwwroot", "css");

            if (Directory.Exists(css))
            {
                foreach (var file in Directory.EnumerateFiles(css, "*.css", SearchOption.AllDirectories))
                    yield return file;
            }
        }
    }

    private static (List<(int Index, string Selector, string Property)> Base,
                    List<(int Index, string Selector, string Property)> Media) Declarations(string css)
    {
        var araliklar = new List<(int Start, int End)>();

        foreach (Match m in Regex.Matches(css, @"@media[^{]*\{"))
        {
            var derinlik = 1;
            var i = m.Index + m.Length;

            while (i < css.Length && derinlik > 0)
            {
                if (css[i] == '{') derinlik++;
                else if (css[i] == '}') derinlik--;
                i++;
            }

            araliklar.Add((m.Index, i));
        }

        bool MedyaIcinde(int index) => araliklar.Any(a => index > a.Start && index < a.End);

        var taban = new List<(int, string, string)>();
        var medya = new List<(int, string, string)>();

        foreach (Match rule in Regex.Matches(css, @"([^{}@]+)\{([^{}]*)\}"))
        {
            var selector = Regex.Replace(rule.Groups[1].Value, @"\s+", " ").Trim();

            if (selector.Length == 0 || selector.Contains('@'))
                continue;

            foreach (Match decl in Regex.Matches(rule.Groups[2].Value, @"(^|[;{])\s*([a-z-]+)\s*:"))
            {
                var kayit = (rule.Index, selector, decl.Groups[2].Value);

                if (MedyaIcinde(rule.Index)) medya.Add(kayit);
                else taban.Add(kayit);
            }
        }

        return (taban, medya);
    }

    [Fact]
    public void Duyarli_ezmeler_ezdikleri_taban_kuraldan_sonra_gelir()
    {
        var sapan = new List<string>();
        var olculen = 0;

        foreach (var path in Stylesheets())
        {
            var css = File.ReadAllText(path);
            var (taban, medya) = Declarations(css);

            foreach (var (mediaIndex, selector, property) in medya)
            {
                var sonraki = taban
                    .Where(t => t.Selector == selector && t.Property == property && t.Index > mediaIndex)
                    .ToList();

                olculen++;

                if (sonraki.Count > 0)
                {
                    var satir = css[..sonraki[0].Index].Count(c => c == '\n') + 1;
                    sapan.Add($"{Path.GetFileName(path)}: '{selector}' → {property}; medya kuralı taban kuraldan (satır {satir}) önce");
                }
            }
        }

        sapan.Should().BeEmpty(
            "@media özgüllük eklemez; blok taban kuraldan önce durursa ezme hiç uygulanmaz. " +
            ".auth-wrap'in 20px boşluğu ve .auth-points'in gizlenmesi tam olarak böyle sessizce ölmüştü");

        olculen.Should().BeGreaterThan(50,
            "ayrıştırıcı bozulduysa bu test hiçbir şey ölçmeden yeşil kalır");
    }
}
