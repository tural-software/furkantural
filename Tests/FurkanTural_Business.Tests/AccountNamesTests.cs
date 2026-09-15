using FluentAssertions;
using FurkanTural_Business.Helpers;

namespace FurkanTural_Business.Tests;

/// <summary>Kayıt adlarının kanonik yazıma indirgenmesi. Buradaki asıl soru biçim değil kimliktir: farklı alfabelerden aynı görünen iki ad iki ayrı satır açabiliyorsa, kullanıcı karşısındakinin kim olduğunu ekrana bakarak anlayamaz.</summary>
public class AccountNamesTests
{
    [Fact]
    public void Kullanici_adinin_bas_ve_son_bosluklari_kirpilir()
    {
        AccountNames.TryNormalizeUsername("  furkan  ", out var normalized, out var error).Should().BeTrue();

        normalized.Should().Be("furkan");
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Kisa_veya_bos_kullanici_adi_reddedilir(string? username)
        => AccountNames.TryNormalizeUsername(username, out _, out _).Should().BeFalse();

    [Fact]
    public void Yuz_karakteri_asan_kullanici_adi_reddedilir()
        => AccountNames.TryNormalizeUsername(new string('a', 101), out _, out _).Should().BeFalse();

    [Theory]
    [InlineData("ali veli")]
    [InlineData("ali@veli")]
    [InlineData("ali/veli")]
    [InlineData("ali​veli")]
    public void Izin_verilmeyen_karakter_tasiyan_ad_reddedilir(string username)
        => AccountNames.TryNormalizeUsername(username, out _, out _).Should().BeFalse(
            "boşluk ve görünmez karakterler adı ekranda ayırt edilemez kılar");

    [Theory]
    [InlineData("sevval")]
    [InlineData("şevval")]
    [InlineData("Gökçe_01")]
    [InlineData("ali.veli-7")]
    public void Turkce_harfler_ve_ayraclar_kabul_edilir(string username)
        => AccountNames.TryNormalizeUsername(username, out _, out _).Should().BeTrue();

    [Fact]
    public void Kiril_harfiyle_yazilmis_ad_reddedilir()
        => AccountNames.TryNormalizeUsername("аdmin", out _, out _).Should().BeFalse(
            "Kiril 'а' ekranda Latin 'a' ile aynı görünür; kabul edilirse iki ayrı hesap aynı adla dolaşır");

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("ADMİN")]
    [InlineData("admın")]
    [InlineData("a.d.m.i.n")]
    [InlineData("ad_min")]
    [InlineData("destek")]
    [InlineData("furkantural")]
    public void Ayrilmis_adlar_reddedilir(string username)
        => AccountNames.TryNormalizeUsername(username, out _, out _).Should().BeFalse(
            "ayraç ve noktalı/noktasız I oyunları listeyi dolaşmanın en kısa yoludur");

    [Fact]
    public void Tam_genislikli_karakterler_kanonik_yaziya_indirilir()
        => AccountNames.TryNormalizeUsername("ａdmin", out _, out _).Should().BeFalse(
            "NFKC sonrası 'ａdmin' düpedüz 'admin' olur; normalize edilmeden bakılsaydı liste atlanırdı");

    [Fact]
    public void Ayrilmis_adi_iceren_uzun_ad_serbesttir()
        => AccountNames.TryNormalizeUsername("adminkoyu", out _, out _).Should().BeTrue(
            "liste tam eşleşmeye bakar; içinde geçen her adı yasaklamak meşru adları da keserdi");

    [Fact]
    public void Eposta_kirpilir_ve_kucuk_harfe_indirilir()
    {
        AccountNames.TryNormalizeEmail("  Furkan@Ornek.TEST ", out var normalized, out var error).Should().BeTrue();

        normalized.Should().Be("furkan@ornek.test");
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("bosluk li@ornek.test")]
    [InlineData("Furkan <furkan@ornek.test>")]
    public void Gecersiz_eposta_reddedilir(string? email)
        => AccountNames.TryNormalizeEmail(email, out _, out _).Should().BeFalse(
            "görünen ad taşıyan yazım da reddedilir; kayıt adresi bir başlık değil, tek bir adrestir");

    [Fact]
    public void Kolon_genisligini_asan_eposta_reddedilir()
        => AccountNames.TryNormalizeEmail(new string('a', 250) + "@ornek.test", out _, out _).Should().BeFalse();
}
