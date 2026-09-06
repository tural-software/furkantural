using FluentAssertions;
using FurkanTural_Blog.Helpers;

namespace FurkanTural_Blog.Tests;

/// <summary>Kategori adresleri kategori adından üretilir; şemada slug sütunu yoktur. Dönüşüm hem adresi üretirken hem gelen adresi eşleştirirken çağrıldığı için tek şart deterministik olmasıdır — aynı ad her seferinde aynı slug'ı vermelidir.<para>Türkçe harfler ASCII'ye indirilir: yüzde kaçışlı bir adres ne okunur ne paylaşılabilir.</para></summary>
public class SlugifierTests
{
    [Theory]
    [InlineData("Yazılım Mimarisi", "yazilim-mimarisi")]
    [InlineData("Veri Tabanı", "veri-tabani")]
    [InlineData("Güvenlik", "guvenlik")]
    [InlineData("Çözümleme", "cozumleme")]
    [InlineData("Öğrenme Notları", "ogrenme-notlari")]
    [InlineData("Şablonlar", "sablonlar")]
    public void Turkce_harfler_ascii_karsiligina_iner(string name, string expected)
        => Slugifier.ToSlug(name).Should().Be(expected);

    [Theory]
    [InlineData("ISO")]
    [InlineData("ıso")]
    [InlineData("İso")]
    [InlineData("iso")]
    public void Dort_I_harfi_de_ayni_adrese_gider(string name)
        => Slugifier.ToSlug(name).Should().Be("iso",
            "adresi elle yazan okur büyük-küçük ayrımını bilemez; dördü de aynı sayfayı açmalı");

    [Theory]
    [InlineData("  C#  ", "c")]
    [InlineData("EF Core 10", "ef-core-10")]
    [InlineData("Test / Deneme", "test-deneme")]
    [InlineData("---", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Ayrac_yigilmaz_ve_bastan_sondan_kirpilir(string? name, string expected)
        => Slugifier.ToSlug(name).Should().Be(expected);

    [Fact]
    public void Ayni_ad_her_seferinde_ayni_slugi_verir()
    {
        var first = Slugifier.ToSlug("Yazılım Mimarisi");
        var second = Slugifier.ToSlug("Yazılım Mimarisi");

        first.Should().Be(second, "adres üretimi ile adres eşleştirmesi aynı fonksiyondan geçer; sapması kategoriyi bulunamaz yapardı");
    }
}
