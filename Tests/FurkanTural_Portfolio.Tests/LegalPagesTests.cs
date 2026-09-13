using FluentAssertions;

namespace FurkanTural_Portfolio.Tests;

public class LegalPagesTests
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

    private static string PortfolioPath(params string[] parts) =>
        Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Portfolio", .. parts]);

    private static string PortfolioFile(params string[] parts) => File.ReadAllText(PortfolioPath(parts));

    [Theory]
    [InlineData("Privacy")]
    [InlineData("Imprint")]
    public void Yasal_sayfanin_eylemi_gorunumu_ve_alt_bilgi_baglantisi_vardir(string action)
    {
        PortfolioFile("Controllers", "HomeController.cs").Should().Contain($"public IActionResult {action}()");
        File.Exists(PortfolioPath("Views", "Home", $"{action}.cshtml")).Should().BeTrue($"{action} görünümü bulunamadı");
        PortfolioFile("Views", "Shared", "_Layout.cshtml").Should().Contain($"asp-action=\"{action}\"",
            "yasal metinler her sayfanın altından ulaşılabilir olmalı");
    }

    [Theory]
    [InlineData("Footer_Imprint")]
    [InlineData("Contact_Form_PrivacyNote")]
    [InlineData("Contact_Form_PrivacyLink")]
    public void Metin_anahtarlari_kaynak_dosyasinda_tanimlidir(string key)
    {
        PortfolioFile("Resources", "SharedResource.tr.resx").Should().Contain($"<data name=\"{key}\"",
            "tanımsız anahtar sayfada anahtarın kendi adıyla görünür");
    }

    [Fact]
    public void Iletisim_formu_gizlilik_politikasina_baglanir()
    {
        var form = PortfolioFile("Views", "Home", "_ContactSection.cshtml");

        form.Should().Contain("Contact_Form_PrivacyNote", "ad, e-posta, IP ve tarayıcı bilgisi toplanan formun yanında kısa bilgi durmalı");
        form.Should().Contain("asp-action=\"Privacy\"", "kısa bilginin ardından aydınlatma metnine bağlantı verilmeli");
    }

    [Theory]
    [InlineData("/Home/Privacy")]
    [InlineData("/Home/Imprint")]
    public void Sitemap_yasal_sayfalari_listeler(string path)
    {
        PortfolioFile("Controllers", "SeoController.cs").Should().Contain($"{{baseUrl}}{path}\"");
    }
}
