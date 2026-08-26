using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Ölçülmüş hiza kusurlarının geri gelmemesi için: süzgeç kutuları tek yükseklikte durmalı, mobil süzgeç sayfasında arama kutusu sarmalını doldurmalı ve pano kartındaki metin flex sarmalında değil kendi kutusunda kırpılmalı.</summary>
public class ControlSizingTests
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

    private static string ComponentCss(string file) =>
        File.ReadAllText(Path.Combine(
            FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "wwwroot", "css", "components", file));

    private static string RuleBody(string css, string selector)
    {
        var match = Regex.Match(css, Regex.Escape(selector) + @"\s*\{([^}]*)\}", RegexOptions.Singleline);

        match.Success.Should().BeTrue($"'{selector}' kuralı bulunamadı");
        return match.Groups[1].Value;
    }

    [Fact]
    public void Suzgec_kutulari_tek_yukseklikte_durur()
    {
        var body = RuleBody(ComponentCss("list-skeleton.css"), ".filter-input, .filter-select");

        body.Should().Contain("min-height",
            "metin kutusu ile <select> yerleşik satır yüksekliğinde ayrışır; açık yükseklik olmadan etiket satırı tırtıklanır");
    }

    [Fact]
    public void Mobil_suzgec_sayfasinda_arama_kutusu_sarmalini_doldurur()
    {
        var css = ComponentCss("list-skeleton.css");

        css.Should().Contain(".filter-input-wrap .filter-input { width: 100%; }",
            "sayfa açıldığında sarmal geniyor, içindeki kutu kendi genişliğinde kalıyordu");
    }

    [Fact]
    public void Pano_kartinda_kirpma_metnin_kendi_kutusundadir()
    {
        var css = ComponentCss("entity-card.css");
        var wrapper = RuleBody(css, ".entity-card__stat-text");

        wrapper.Should().NotContain("text-overflow",
            "sarmal flex sütunu; text-overflow burada üç nokta üretmez, metni sessizce keser");
        wrapper.Should().Contain("min-width: 0",
            "min-width: auto olan flex öğesi küçülemez, kırpma hiç tetiklenmez");

        foreach (var selector in new[] { ".entity-card__stat-value", ".entity-card__stat-label" })
            RuleBody(css, selector).Should().Contain("text-overflow: ellipsis",
                $"{selector} metni taşıyan kutudur; kırpma orada olmalı");
    }
}
