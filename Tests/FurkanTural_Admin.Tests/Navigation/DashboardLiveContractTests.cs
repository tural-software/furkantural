using FluentAssertions;

namespace FurkanTural_Admin.Tests.Navigation;

public class DashboardLiveContractTests
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

    private static string AdminFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Admin", .. parts]));

    [Fact]
    public void Pano_govdesi_tek_parcadan_cizilir_ve_canli_bolge_olarak_isaretlenir()
    {
        var index = AdminFile("Views", "Dashboard", "Index.cshtml");
        var body = AdminFile("Views", "Dashboard", "_DashboardBody.cshtml");

        index.Should().Contain("data-dashboard-live", "istemci tazelenecek bölgeyi bu işaretle bulur");
        index.Should().Contain("PartialAsync(\"_DashboardBody\"", "sayfa ile canlı tazeleme aynı parçayı çizer");
        index.Should().NotContain("data-kpi",
            "göstergeler yalnızca parçada yaşamalı; sayfada ikinci bir kopya kalırsa canlı tazeleme o kopyayı güncellemez");

        body.Should().Contain("data-kpi").And.Contain("dash-attention").And.Contain("_EntityCard");
    }

    [Fact]
    public void Istemci_panoyu_gorunurken_ve_oturumu_gozeterek_tazeler()
    {
        var client = AdminFile("wwwroot", "js", "admin-live.js");

        client.Should().Contain("'/Dashboard/Live'", "pano gövdesi bu uçtan gelir");
        client.Should().Contain("[data-dashboard-live]");
        client.Should().Contain("visibilityState",
            "gizli sekme panoyu her haberde yeniden sorgulamamalı; tek bir tazeleme onlarca sayım sorgusu tutuyor");
        client.Should().Contain("status === 401",
            "oturum düşmüşse giriş sayfasının HTML'i panoya gömülmez, yönetici girişe gönderilir");
        client.Should().Contain("classList.add('active')",
            "reveal.js kartları yalnızca sayfa açılışında görünür yapar; değiştirilen kartlar etkinleştirilmezse görünmez kalır");
    }
}
