using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Yirmi bir listenin boş durumu ve alt bandı tek desende olmalı: boş satır tablonun içinde çizilir, sütun sayısı kadar yer kaplar ve alt bant paylaşılan sayfalama bileşenidir. İki modül kendi kopyasını taşıyordu; bu testler ayrışmanın geri gelmesini engeller.</summary>
public class TableFooterContractTests
{
    private const string SolutionMarker = "FurkanTural.slnx";
    private const string EmptyMessage = "Kayıt bulunamadı.";

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

    private static IEnumerable<(string Name, string Content)> TablePartials()
    {
        var views = Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "Views");

        foreach (var path in Directory.EnumerateFiles(views, "_*Table.cshtml", SearchOption.AllDirectories))
            yield return (Path.GetFileName(path), File.ReadAllText(path));
    }

    [Fact]
    public void Bos_satir_tablonun_icinde_ve_ayni_metinle_cizilir()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in TablePartials())
        {
            if (!content.Contains(@"class=""empty-row"""))
                sapan.Add($"{name}: empty-row yok");
            else if (!content.Contains(EmptyMessage))
                sapan.Add($"{name}: \"{EmptyMessage}\" yazmıyor");

            if (content.Contains("empty-state"))
                sapan.Add($"{name}: tablo dışına ayrı bir empty-state bloğu koyuyor");
        }

        sapan.Should().BeEmpty(
            "boş satır tablonun içinde çizilmezse başlık satırı boşlukta kalır ve metin modülden modüle ayrışır");
    }

    [Fact]
    public void Bos_satir_tablonun_sutun_sayisi_kadar_yer_kaplar()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in TablePartials())
        {
            var thead = Regex.Match(content, @"<thead.*?</thead>", RegexOptions.Singleline);
            var columns = Regex.Matches(thead.Value, @"<th[\s>]").Count;

            var colspan = Regex.Match(content, @"<td\s+colspan=""(\d+)""");
            var span = colspan.Success ? int.Parse(colspan.Groups[1].Value) : 0;

            if (columns != span)
                sapan.Add($"{name}: {columns} kolon, colspan={span}");
        }

        sapan.Should().BeEmpty("colspan kolon sayısını tutmazsa boş satır tabloyu bozar");
    }

    [Fact]
    public void Alt_bandi_her_liste_ortak_parcadan_cizer()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in TablePartials())
        {
            if (!content.Contains(@"Html.PartialAsync(""_TableFooter"""))
                sapan.Add($"{name}: _TableFooter kullanmıyor");

            foreach (var kendi in new[] { "pagination-bar", "page-btn", "class=\"tbl-footer\"", "string PageUrl(int p)" })
            {
                if (content.Contains(kendi))
                    sapan.Add($"{name}: alt bandın kendi kopyasını taşıyor ({kendi})");
            }
        }

        sapan.Should().BeEmpty(
            "yirmi bir kopya tek parçaya indirildi; kendi kopyasını geri getiren modül sessizce geride kalır");
    }

    [Fact]
    public void Ortak_parca_sayfalama_bilesenini_tasir()
    {
        var footer = File.ReadAllText(Path.Combine(
            FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "Views", "Shared", "_TableFooter.cshtml"));

        foreach (var beklenen in new[] { @"class=""tbl-footer""", @"class=""page-size-sel""", @"class=""pag-btn" })
            footer.Should().Contain(beklenen);
    }

    [Fact]
    public void Sayfa_baglantisi_ve_boyut_formu_ayni_suzgec_listesini_kullanir()
    {
        var footer = File.ReadAllText(Path.Combine(
            FindSolutionRoot(), "Presentation", "FurkanTural_Admin", "Views", "Shared", "_TableFooter.cshtml"));

        var kullanim = Regex.Matches(footer, @"foreach \(var filter in Model\.Filters\)").Count;

        kullanim.Should().Be(2,
            "biri sayfa bağlantısının sorgu dizesini biri gizli alanları üretir; "
          + "ikisi ayrı listeden beslenirse sayfa değişince süzgeç düşer");
    }
}
