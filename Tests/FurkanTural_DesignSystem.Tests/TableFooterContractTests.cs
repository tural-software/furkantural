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
    public void Alt_bant_paylasilan_sayfalama_bilesenidir()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in TablePartials())
        {
            foreach (var beklenen in new[] { @"class=""tbl-footer""", @"class=""page-size-sel""", @"class=""pag-btn" })
            {
                if (!content.Contains(beklenen))
                    sapan.Add($"{name}: {beklenen} yok");
            }

            foreach (var eski in new[] { "pagination-bar", "page-btn" })
            {
                if (content.Contains(eski))
                    sapan.Add($"{name}: kendi sayfalama kopyasını taşıyor ({eski})");
            }
        }

        sapan.Should().BeEmpty(
            "sayfa boyutu seçici ve sayfa düğmeleri iskeletin bileşeni; kendi kopyasını taşıyan modül geride kalır");
    }

    [Fact]
    public void Sayfalama_baglantilari_suzgecleri_tasir()
    {
        var sapan = TablePartials()
            .Where(t => !t.Content.Contains("string PageUrl(int p)"))
            .Select(t => t.Name)
            .ToList();

        sapan.Should().BeEmpty(
            "sayfa değişince süzgeçler düşmemeli; her partial kendi route değerlerini PageUrl'de toplar");
    }
}
