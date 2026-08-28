using FluentAssertions;
using FurkanTural_Business.Helpers;

namespace FurkanTural_Business.Tests;

public class PasswordPolicyTests
{
    private const string Gecerli = "Abc1!def";

    [Fact]
    public void Kurallarin_hepsini_saglayan_parola_kabul_edilir()
        => PasswordPolicy.Validate(Gecerli).Should().BeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_parola_reddedilir(string? parola)
        => PasswordPolicy.Validate(parola).Should().Be("Şifre boş olamaz.");

    [Fact]
    public void Alti_karakterden_kisa_parola_reddedilir()
        => PasswordPolicy.Validate("Ab1!c").Should().Be("Parola en az 6 karakter olmalı.");

    private static string UzunlukTa(int uzunluk) => "Abc1!" + new string('x', uzunluk - 5);

    [Fact]
    public void Ust_sinirin_tam_uzerindeki_parola_kabul_edilir()
        => PasswordPolicy.Validate(UzunlukTa(PasswordPolicy.MaximumLength)).Should().BeNull();

    [Fact]
    public void Ust_siniri_asan_parola_reddedilir()
        => PasswordPolicy.Validate(UzunlukTa(PasswordPolicy.MaximumLength + 1))
            .Should().Be($"Parola en fazla {PasswordPolicy.MaximumLength} karakter olabilir.");

    [Fact]
    public void Uzunluk_kontrolu_karakter_taramasindan_once_calisir()
        => PasswordPolicy.Validate("Abc1!" + new string('ş', 5000))
            .Should().StartWith("Parola en fazla",
                "özetleme maliyetine girmeden önce uzunlukta durmalı");

    [Theory]
    [InlineData("abc1!def", "bir büyük harf")]
    [InlineData("ABC1!DEF", "bir küçük harf")]
    [InlineData("Abcd!efg", "bir rakam")]
    [InlineData("Abc1defg", "bir sembol")]
    public void Eksik_karakter_sinifi_adiyla_bildirilir(string parola, string beklenen)
        => PasswordPolicy.Validate(parola).Should().Be($"Parola en az {beklenen} içermeli.");

    [Fact]
    public void Birden_fazla_eksik_sinif_tek_cumlede_sayilir()
        => PasswordPolicy.Validate("abcdefgh")
            .Should().Be("Parola en az bir büyük harf, bir rakam ve bir sembol içermeli.");

    [Theory]
    [InlineData("Abc1!de\"")]
    [InlineData("Abc1!de'")]
    [InlineData("Abc1!de\\")]
    [InlineData("Abc1!de`")]
    [InlineData("Abc1!de<")]
    [InlineData("Abc1!de>")]
    [InlineData("Abc1!de&")]
    [InlineData("Abc1!de ")]
    [InlineData("Abc1!deş")]
    [InlineData("Abc1!deİ")]
    [InlineData("Abc1!de\t")]
    public void Izin_verilmeyen_karakter_reddedilir(string parola)
        => PasswordPolicy.Validate(parola).Should().StartWith("Parolada kullanılamayan bir karakter var.");

    [Fact]
    public void Izin_verilen_her_sembol_tek_tek_kabul_edilir()
    {
        foreach (var sembol in PasswordPolicy.Symbols)
            PasswordPolicy.Validate($"Abc1de{sembol}")
                .Should().BeNull($"'{sembol}' izin verilen kümede duruyor");
    }

    [Fact]
    public void Sembol_kumesi_tirnak_kacis_ve_acili_ayrac_tasimaz()
        => PasswordPolicy.Symbols.Should().NotContainAny("\"", "'", "\\", "`", "<", ">", "&", " ");
}
