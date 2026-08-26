using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Dar ekranda tablo kart listesine dönüşür. Hücre etiketleri ve rolleri (kimlik / başlık / işlem) markup'tan değil, tablonun kendi başlık satırından türetilir — bu testler o sözleşmenin bozulmadığını doğrular.</summary>
public class MobileListContractTests
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

    private static string AdminPath(params string[] parts) =>
        Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Admin", .. parts]);

    private static IEnumerable<(string Name, string Content)> TablePartials()
    {
        var views = AdminPath("Views");

        foreach (var path in Directory.EnumerateFiles(views, "_*Table.cshtml", SearchOption.AllDirectories))
            yield return (Path.GetFileName(path), File.ReadAllText(path));
    }

    [Fact]
    public void Yirmi_bir_modulun_tablo_parcasi_vardir()
    {
        TablePartials().Should().HaveCount(21);
    }

    [Fact]
    public void Her_tablonun_kimlik_ve_islem_kolonu_isaretlidir()
    {
        var eksik = new List<string>();

        foreach (var (name, content) in TablePartials())
        {
            if (!content.Contains("col-id")) eksik.Add($"{name} → col-id");
            if (!content.Contains("col-actions")) eksik.Add($"{name} → col-actions");
        }

        eksik.Should().BeEmpty(
            "kart görünümünde '#id' rozeti ve alttaki işlem çubuğu bu sınıflardan bulunuyor");
    }

    [Fact]
    public void Her_tablonun_baslik_satiri_vardir()
    {
        var eksik = TablePartials()
            .Where(t => !t.Content.Contains("<thead"))
            .Select(t => t.Name)
            .ToList();

        eksik.Should().BeEmpty("hücre etiketleri başlık satırından okunuyor; thead yoksa kartlar etiketsiz kalır");
    }

    [Fact]
    public void Mobil_liste_betigi_duzende_baglidir()
    {
        var layout = File.ReadAllText(AdminPath("Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("mobile-list.js", "kart dönüşümü her liste sayfasında çalışmalı");
        File.Exists(AdminPath("wwwroot", "js", "mobile-list.js")).Should().BeTrue();
    }
}
