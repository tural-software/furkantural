using FluentAssertions;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Panel sayfaları e-posta, IP ve mesaj içeriği gösterir. Tarayıcı bu yanıtları önbelleğe alırsa ortak bir makinede oturum kapatıldıktan sonra geri tuşuyla yeniden görüntülenebilir.</summary>
public class NoStoreContractTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static string AdminProgram()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
            directory = directory.Parent;

        directory.Should().NotBeNull($"'{SolutionMarker}' bulunamadı");
        return File.ReadAllText(Path.Combine(directory!.FullName, "Presentation", "FurkanTural_Admin", "Program.cs"));
    }

    [Fact]
    public void Onbellek_kurali_belirtmeyen_her_yanit_saklanmaz()
    {
        var program = AdminProgram();

        program.Should().Contain("CacheControl = \"no-store\"");
        program.Should().Contain("ContainsKey(\"Cache-Control\")",
            "kendi önbellek kuralını koyan yanıtlar — parmak izli statik dosyalar ve hata sayfası — ezilmemeli; " +
            "ezilirse her sayfa açılışında bütün CSS ve betikler yeniden indirilir");
        program.IndexOf("OnStarting", StringComparison.Ordinal).Should().BeLessThan(
            program.IndexOf("app.MapStaticAssets();", StringComparison.Ordinal),
            "kural istek hattında statik dosya ucundan önce kurulmalı");
    }
}
