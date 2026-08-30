using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Hata ve çevrimdışı sayfaları aynı durum iskeletini paylaşır. Çevrimdışı sayfası servis çalışanının önbelleğinden açıldığı için yalnızca SHELL listesindeki dosyalara dayanabilir; site CSP'si satır içi betiği engellediğinden düğmesi harici bir dosyadan bağlanır.</summary>
public class ChatStatusPageTests
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

    private static string ChatFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Chat", .. parts]));

    private static string Offline() => ChatFile("wwwroot", "offline.html");
    private static string ErrorView() => ChatFile("Views", "Shared", "Error.cshtml");
    private static string ServiceWorker() => ChatFile("wwwroot", "sw.js");

    [Fact]
    public void Hata_sayfasi_turkce_ve_iskele_artigi_tasimiyor()
    {
        var view = ErrorView();

        view.Should().NotContain("Development Mode",
            "şablon artığı son kullanıcıya ortam değişkeni ayarlamayı anlatıyordu");
        view.Should().NotContain("An error occurred while processing",
            "sayfanın tamamı Türkçe olmalı");

        view.Should().Contain("status-wrap", "iki durum sayfası aynı iskeleti paylaşır");
        view.Should().Contain("İsteğiniz işlenirken bir hata oluştu");
    }

    [Fact]
    public void Istek_numarasi_yalnizca_kosullu_gosterilir()
    {
        var view = ErrorView();

        view.Should().Contain("@if (Model.ShowRequestId)",
            "numara her zaman çizilirse boş bir kutu kalır");
        view.Should().Contain("status-request-value");
    }

    [Fact]
    public void Cevrimdisi_sayfasi_temaya_baglidir()
    {
        var offline = Offline();

        offline.Should().Contain("/css/theme.css", "renkler token'lardan gelmeli");
        offline.Should().Contain("/js/theme.js", "kayıtlı tema tercihini uygulayan betik");

        Regex.IsMatch(offline, @"#[0-9a-fA-F]{6}").Should().BeFalse(
            "sayfa koyu temaya sabitlenmişti; açık temada beyaz zemine koyu kutu çiziyordu");

        Regex.IsMatch(offline, @"<html[^>]*data-theme=").Should().BeFalse(
            "html üzerinde tema sabitlenirse theme.js'in 'zaten ayarlıysa dokunma' koşulu " +
            "kayıtlı tercihi uygulamasını engeller");
    }

    [Fact]
    public void Cevrimdisi_dugmesi_satir_ici_betige_dayanmaz()
    {
        var offline = Offline();

        offline.Should().NotContain("onclick=",
            "site CSP'si script-src'de 'unsafe-inline' taşımıyor; satır içi işleyici hiç çalışmıyordu");
        offline.Should().Contain("data-offline-retry", "düğme harici betikten bağlanır");
        offline.Should().Contain("/js/offline.js");
    }

    [Fact]
    public void Cevrimdisi_sayfasinin_dayandigi_her_dosya_onbelleklenir()
    {
        var sw = ServiceWorker();
        var offline = Offline();

        var shell = Regex.Match(sw, @"const SHELL = \[(.*?)\];", RegexOptions.Singleline);
        shell.Success.Should().BeTrue("SHELL listesi bulunamadı");

        var varliklar = Regex.Matches(offline, @"(?:href|src)=""(/[^""]+)""")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        varliklar.Should().HaveCountGreaterThan(2, "sayfa hiç varlık istemiyorsa test bir şey doğrulamıyor");

        foreach (var varlik in varliklar)
        {
            shell.Groups[1].Value.Should().Contain($"'{varlik}'",
                $"çevrimdışıyken yalnızca precache'teki dosyalar gelir; '{varlik}' listede yoksa sayfa eksik açılır");
        }
    }

    [Fact]
    public void Onbellek_surumu_shell_degisince_ilerletilir()
    {
        var sw = ServiceWorker();
        var surum = Regex.Match(sw, @"const CACHE = 'chatural-v(\d+)';");

        surum.Success.Should().BeTrue("önbellek sürümü okunamadı");
        int.Parse(surum.Groups[1].Value).Should().BeGreaterThanOrEqualTo(12,
            "SHELL listesine dosya eklendiğinde sürüm ilerlemezse mevcut istemciler yeni dosyayı hiç indirmez");
    }
}
