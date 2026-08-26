using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Detay çekmecesinin denetim alanları (aktif, silinmiş, oluşturulma, oluşturan, güncellenme, güncelleyen, silinme) tek yerde — <c>DmAudit.section()</c> — tanımlıdır. Bu testler kopyanın geri gelmesini ve yanlış saat dilimi etiketinin yeniden yazılmasını engeller.</summary>
public class AuditSectionParityTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static readonly string[] AuditLabels =
    [
        "'Aktif'",
        "'Silinmiş'",
        "'Oluşturulma Tarihi'",
        "'Oluşturan'",
        "'Güncellenme Tarihi'",
        "'Güncelleyen'",
        "'Silinme Tarihi'",
    ];

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

    private static string PagesDirectory() =>
        Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "wwwroot", "js", "pages");

    private static string DetailModalPath() =>
        Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "wwwroot", "js", "detail-modal.js");

    private static IEnumerable<(string Name, string Content)> PageConfigs()
    {
        foreach (var path in Directory.EnumerateFiles(PagesDirectory(), "*.js"))
            yield return (Path.GetFileName(path), File.ReadAllText(path));
    }

    [Fact]
    public void Denetim_bolumu_tek_yerde_tanimlidir()
    {
        var detailModal = File.ReadAllText(DetailModalPath());

        detailModal.Should().Contain("window.DmAudit", "denetim alanlarının tek kaynağı detail-modal.js olmalı");
        detailModal.Should().Contain("requires: 'deletedAt'", "DTO'sunda olmayan alan çizilmemeli");
    }

    [Fact]
    public void Cekmece_acan_her_sayfa_ortak_denetim_bolumunu_kullanir()
    {
        var eksik = PageConfigs()
            .Where(p => p.Content.Contains("DetailModal.open"))
            .Where(p => !p.Content.Contains("DmAudit.section()"))
            .Select(p => p.Name)
            .ToList();

        eksik.Should().BeEmpty("çekmece açan her modül denetim alanlarını ortak bölümden almalı");
    }

    [Fact]
    public void Hicbir_sayfa_denetim_alanlarini_kendisi_yazmaz()
    {
        var ihlal = new List<string>();

        foreach (var (name, content) in PageConfigs())
        {
            foreach (var label in AuditLabels)
            {
                if (content.Contains($"label: {label}"))
                    ihlal.Add($"{name} → {label}");
            }
        }

        ihlal.Should().BeEmpty("bu alanlar DmAudit.section() içinde duruyor; kopyası eskir ve ayrışır");
    }

    [Fact]
    public void Hicbir_alan_yerel_saati_UTC_diye_etiketlemez()
    {
        var ihlal = PageConfigs()
            .Where(p => p.Content.Contains("(UTC)"))
            .Select(p => p.Name)
            .ToList();

        ihlal.Should().BeEmpty(
            "DmFmt.dateUtc yerel saati (Europe/Istanbul) döndürür; '(UTC)' etiketi okuyucuya yanlış saat dilimi söyler");
    }

    [Fact]
    public void Cok_satirli_alan_secenegi_cizicide_karsiliksiz_degildir()
    {
        var kullanan = PageConfigs().Where(p => p.Content.Contains("multiline: true")).Select(p => p.Name).ToList();
        var detailModal = File.ReadAllText(DetailModalPath());

        kullanan.Should().NotBeEmpty("en az bir yapılandırma bu seçeneği kullanıyor olmalı");
        detailModal.Should().Contain("field.multiline",
            "yapılandırmada kullanılan seçeneğin çizicide karşılığı olmalı; yoksa sessizce yok sayılır");
    }
}
