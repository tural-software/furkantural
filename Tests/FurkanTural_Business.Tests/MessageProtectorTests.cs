using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Tests;

/// <summary>Mesaj içeriğinin at-rest şifrelemesi. Sınanan asıl şey gizlilik değil bağlamdır: şifreli metin ait olduğu konuşmaya bağlanmazsa, veri tabanına yazabilen biri bir satırın içeriğini başka bir konuşmaya taşıyabilir ve okuyan taraf bunu ayırt edemez.</summary>
public class MessageProtectorTests
{
    private const string Key = "birim-testleri-icin-sohbet-anahtari";

    private static MessageProtector Build(string key = Key)
        => new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ChatEncryption:Key"] = key })
            .Build());

    /// <summary>Şifreleme devreye girdiği dönemin biçimi: ilişkili veri taşımayan <c>ENC1:</c> kaydı. Veri tabanında bu biçimde satırlar durduğu için testte de aynı biçimde üretilir.</summary>
    private static string LegacyProtected(string plaintext, string key = Key)
    {
        var derived = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(derived, 16))
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var packed = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, packed, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, packed, nonce.Length + cipherBytes.Length, tag.Length);

        return "ENC1:" + Convert.ToBase64String(packed);
    }

    [Fact]
    public void Ayni_konusmada_yazilan_mesaj_geri_okunur()
    {
        var sut = Build();

        var stored = sut.Protect("merhaba", 3, 9);

        stored.Should().StartWith("ENC2:").And.NotContain("merhaba");
        sut.Unprotect(stored, 3, 9).Should().Be("merhaba");
    }

    [Fact]
    public void Taraf_sirasi_degisse_de_ayni_mesaj_okunur()
    {
        var sut = Build();

        var stored = sut.Protect("merhaba", 3, 9);

        sut.Unprotect(stored, 9, 3).Should().Be("merhaba",
            "konuşma listesi son mesajı okurken hangi tarafın yazdığını bilmez, yalnızca iki ucu bilir");
    }

    [Fact]
    public void Baska_konusmaya_tasinan_sifreli_metin_cozulmez()
    {
        var sut = Build();

        var stored = sut.Protect("gizli", 3, 9);

        sut.Unprotect(stored, 3, 10).Should().Be(MessageProtector.UnreadableContent,
            "satırlar arası taşımayı engelleyen tek şey içeriğin konuşmaya bağlanmasıdır");
    }

    [Fact]
    public void Ayni_metin_her_seferinde_farkli_sifrelenir()
    {
        var sut = Build();

        sut.Protect("merhaba", 3, 9).Should().NotBe(sut.Protect("merhaba", 3, 9),
            "eşit içerikler şifreli hâllerine bakılarak eşleştirilememeli");
    }

    [Fact]
    public void Eski_bicimdeki_kayitlar_okunmaya_devam_eder()
    {
        var sut = Build();
        var stored = LegacyProtected("eski mesaj");

        sut.Unprotect(stored, 3, 9).Should().Be("eski mesaj",
            "yeni biçim eskisini okuyamazsa geçmiş sohbet tarihi bir anda okunamaz hâle gelir");
        sut.IsProtected(stored).Should().BeTrue("eski kayıt zaten şifrelidir, yeniden şifrelenmemeli");
    }

    [Fact]
    public void Anahtar_degistiginde_sifreli_metin_ham_hâliyle_donmez()
    {
        var stored = Build().Protect("gizli", 3, 9);

        var sonra = Build("bambaska-bir-anahtar").Unprotect(stored, 3, 9);

        sonra.Should().Be(MessageProtector.UnreadableContent,
            "ham değeri geri vermek, anahtarın değiştiğini kimsenin fark etmemesi demekti");
        sonra.Should().NotContain("ENC2:");
    }

    [Fact]
    public void Sifrelenmemis_kayit_oldugu_gibi_gecer()
    {
        var sut = Build();

        sut.Unprotect("düz metin", 3, 9).Should().Be("düz metin");
        sut.IsProtected("düz metin").Should().BeFalse();
    }

    [Fact]
    public void Kullanicinin_kendi_yazdigi_onek_metni_kaybolmaz()
    {
        var sut = Build();

        sut.Unprotect("ENC1: bu bir mesaj", 3, 9).Should().Be("ENC1: bu bir mesaj",
            "yapısı hiç tanınmayan eski önek kullanıcının kendi metni olabilir");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Bos_deger_oldugu_gibi_gecer(string? value)
    {
        var sut = Build();

        sut.Protect(value, 3, 9).Should().Be(value);
        sut.Unprotect(value, 3, 9).Should().Be(value);
    }

    [Fact]
    public void Bozulmus_sifreli_metin_okunamaz_sayilir()
    {
        var sut = Build();
        var stored = sut.Protect("merhaba", 3, 9)!;
        var bozuk = stored[..^4] + "AAAA";

        sut.Unprotect(bozuk, 3, 9).Should().Be(MessageProtector.UnreadableContent);
    }

    [Fact]
    public void Yer_tutucu_anahtarla_acilmaz()
    {
        var act = () => Build("CHANGE_ME_CHAT_KEY");

        act.Should().Throw<InvalidOperationException>(
            "herkesin görebildiği bir dizeden türetilmiş anahtarla şifrelemek, hiç şifrelememekten daha yanıltıcıdır");
    }
}
