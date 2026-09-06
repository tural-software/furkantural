using FluentAssertions;
using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Tests;

/// <summary>Arşiv gruplaması. Sıralama en yeniden eskiye olmalı — arşivin ilk ekranı en son yayınlananı göstermezse okur listeyi baştan taramak zorunda kalır.<para>Boş arşiv ile arıza ayrı ayrı sınanır: ikisi de sıfır satır gösterir ama ekranda aynı metni gösteremezler.</para></summary>
public class ArchiveViewModelTests
{
    private static BlogSitemapItem Item(int id, string? title, int year, int month, int day) =>
        new() { Id = id, Title = title, CreatedAt = new DateTime(year, month, day) };

    [Fact]
    public void Yillar_yeniden_eskiye_siralanir()
    {
        var vm = ArchiveViewModel.From([
            Item(1, "Eski", 2024, 3, 5),
            Item(2, "Yeni", 2026, 1, 2),
            Item(3, "Orta", 2025, 7, 9)
        ]);

        vm.Years.Select(y => y.Year).Should().ContainInOrder(2026, 2025, 2024);
    }

    [Fact]
    public void Ayar_yil_icinde_yeniden_eskiye_siralanir()
    {
        var vm = ArchiveViewModel.From([
            Item(1, "Ocak", 2026, 1, 10),
            Item(2, "Eylül", 2026, 9, 1),
            Item(3, "Mayıs", 2026, 5, 20)
        ]);

        vm.Years.Single().Months.Select(m => m.Number).Should().ContainInOrder(9, 5, 1);
    }

    [Fact]
    public void Ay_icindeki_yazilar_gune_gore_yeniden_eskiye_siralanir()
    {
        var vm = ArchiveViewModel.From([
            Item(1, "Ayın ikisi", 2026, 4, 2),
            Item(2, "Ayın yirmisi", 2026, 4, 20),
            Item(3, "Ayın onu", 2026, 4, 10)
        ]);

        vm.Years.Single().Months.Single().Items.Select(i => i.Id).Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public void Ayni_gunde_yayinlanan_iki_yazi_kimlige_gore_kararli_siralanir()
    {
        var vm = ArchiveViewModel.From([
            Item(41, "Önce eklenen", 2026, 4, 8),
            Item(42, "Sonra eklenen", 2026, 4, 8)
        ]);

        vm.Years.Single().Months.Single().Items.Select(i => i.Id).Should().ContainInOrder(42, 41);
    }

    [Fact]
    public void Farkli_yillarin_ayni_ayi_tek_gruba_dusmez()
    {
        var vm = ArchiveViewModel.From([
            Item(1, "2025 Mart", 2025, 3, 4),
            Item(2, "2026 Mart", 2026, 3, 4)
        ]);

        vm.Years.Should().HaveCount(2);
        vm.Years.Should().OnlyContain(y => y.Months.Count == 1 && y.Months[0].Number == 3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Basliksiz_kayit_listeye_alinmaz(string? title)
    {
        var vm = ArchiveViewModel.From([Item(1, title, 2026, 4, 8), Item(2, "Başlıklı", 2026, 4, 9)]);

        vm.TotalCount.Should().Be(1);
        vm.Years.Single().Months.Single().Items.Single().Id.Should().Be(2);
    }

    [Fact]
    public void Tarihsiz_kayit_listeye_alinmaz()
    {
        var vm = ArchiveViewModel.From([
            new BlogSitemapItem { Id = 1, Title = "Tarihsiz" },
            Item(2, "Tarihli", 2026, 4, 9)
        ]);

        vm.TotalCount.Should().Be(1);
    }

    [Fact]
    public void Sayaclar_yil_ve_toplam_duzeyinde_tutar()
    {
        var vm = ArchiveViewModel.From([
            Item(1, "A", 2026, 1, 1),
            Item(2, "B", 2026, 2, 1),
            Item(3, "C", 2025, 2, 1)
        ]);

        vm.TotalCount.Should().Be(3);
        vm.Years.Single(y => y.Year == 2026).Count.Should().Be(2);
        vm.Years.Single(y => y.Year == 2025).Count.Should().Be(1);
    }

    [Fact]
    public void Yil_baglantisi_harfle_baslar()
    {
        var anchor = ArchiveViewModel.From([Item(1, "A", 2026, 1, 1)]).Years.Single().Anchor;

        anchor.Should().Be("yil-2026");
        char.IsAsciiDigit(anchor[0]).Should().BeFalse();
    }

    [Fact]
    public void Bos_arsiv_ariza_degildir()
    {
        var vm = ArchiveViewModel.From([]);

        vm.Years.Should().BeEmpty();
        vm.LoadFailed.Should().BeFalse();
    }

    [Fact]
    public void Ariza_bos_arsivden_ayrilir()
        => new ArchiveViewModel { LoadFailed = true }.LoadFailed.Should().BeTrue();
}
