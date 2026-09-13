using FluentAssertions;

namespace FurkanTural_Portfolio.Tests;

public class ThirdPartyScriptTests
{
    private const string SolutionMarker = "FurkanTural.slnx";
    private const string ThreeUrl = "'/lib/three/build/three.module.min.js'";

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

    private static string PortfolioRoot() => Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Portfolio");

    private static bool IsSkipped(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}lib{sep}") || path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}");
    }

    [Theory]
    [InlineData("background-three.js")]
    [InlineData("portfolio-3d.js")]
    public void Uc_boyutlu_sahneler_kitapligi_yerel_kopyadan_yukler(string script)
    {
        var root = PortfolioRoot();
        var source = File.ReadAllText(Path.Combine(root, "wwwroot", "js", script));

        source.Should().Contain(ThreeUrl,
            "iki sahne aynı adresten yüklenmeli; adresler ayrışırsa tarayıcı aynı kitaplığı iki kez indirir");
        File.Exists(Path.Combine(root, "wwwroot", "lib", "three", "build", "three.module.min.js")).Should().BeTrue(
            "betikler yerel kopyayı işaret ediyor; dosya yoksa 3D sessizce hiç başlamaz");
        File.Exists(Path.Combine(root, "wwwroot", "lib", "three", "LICENSE")).Should().BeTrue(
            "MIT lisansı kitaplıkla birlikte dağıtılmalı");
    }

    [Fact]
    public void Site_hicbir_kaynagi_jsdelivrden_istemez()
    {
        var root = PortfolioRoot();
        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".js"))
            .Where(f => !IsSkipped(f))
            .ToList();

        files.Count.Should().BeGreaterThan(10, "tarama boş dönerse bu test hiçbir şey doğrulamıyor");

        files.Where(f => File.ReadAllText(f).Contains("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(root, f))
            .Should().BeEmpty(
                "içerik güvenliği kuralı bu kaynağa izin vermiyor; oradan istenen betik sessizce engellenir " +
                "ve istek ziyaretçinin IP adresini yurt dışındaki bir sağlayıcıya taşır");
    }
}
