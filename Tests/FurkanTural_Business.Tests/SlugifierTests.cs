using FluentAssertions;
using FurkanTural_Business.Helpers;

namespace FurkanTural_Business.Tests;

/// <summary>Sunucu tarafındaki slug üretimi. İki ayrı sözleşmeye birden uymak zorundadır.<para>Birincisi: blog sunum projesindeki eşiyle <b>aynı çıktıyı</b> vermelidir. Kategori adresleri slug sütunundan önce orada üretiliyordu; farklı bir sonuç, bugün paylaşılmış <c>/kategori/*</c> adreslerini 404'e düşürürdü.</para><para>İkincisi: mevcut satırları dolduran geçiş betiği (T-SQL) de aynı sonucu vermelidir. Buradaki gerçek başlıklar o betiğin ürettiği değerlerdir; test, iki uygulamanın ayrışmadığını sabitler.</para></summary>
public class SlugifierTests
{
    [Theory]
    [InlineData("Yazılım Mimarisi", "yazilim-mimarisi")]
    [InlineData("Güvenlik", "guvenlik")]
    [InlineData("Çözümleme", "cozumleme")]
    [InlineData("Öğrenme Notları", "ogrenme-notlari")]
    [InlineData("Şablonlar", "sablonlar")]
    [InlineData("Hâlâ", "hala")]
    [InlineData("Îlave Û Testi", "ilave-u-testi")]
    public void Turkce_harfler_ascii_karsiligina_iner(string value, string expected)
        => Slugifier.ToSlug(value).Should().Be(expected);

    [Theory]
    [InlineData("ISO", "iso")]
    [InlineData("ıso", "iso")]
    [InlineData("İSO", "iso")]
    [InlineData("Iso", "iso")]
    public void Dort_I_harfi_de_ayni_slugu_verir(string value, string expected)
        => Slugifier.ToSlug(value).Should().Be(expected);

    [Theory]
    [InlineData(".NET", "net")]
    [InlineData(".NET CORE", "net-core")]
    [InlineData("ASP.NET", "asp-net")]
    [InlineData("ASP.NET MVC", "asp-net-mvc")]
    [InlineData("ASP.NET WEB API", "asp-net-web-api")]
    [InlineData("C#", "c")]
    [InlineData("JS", "js")]
    [InlineData("HTML", "html")]
    [InlineData("REACT", "react")]
    [InlineData("YAZILIM", "yazilim")]
    public void Mevcut_kategori_adresleri_bozulmaz(string name, string expected)
        => Slugifier.ToSlug(name).Should().Be(expected);

    [Theory]
    [InlineData("Async İstisnaları — Üç İş Patladı, catch Bloğunuz Neden Tek Hata Gördü?",
                "async-istisnalari-uc-is-patladi-catch-blogunuz-neden-tek-hata-gordu")]
    [InlineData("System.Text.Json Sözleşmesi — Nesnenin Yarısı JSON'a Neden Hiç Yazılmıyor?",
                "system-text-json-sozlesmesi-nesnenin-yarisi-json-a-neden-hic-yazilmiyor")]
    [InlineData("Output Caching — Cache Ekledim Ama Sunucu Yükü Neden Düşmedi?",
                "output-caching-cache-ekledim-ama-sunucu-yuku-neden-dusmedi")]
    [InlineData("Rate Limiting — .NET'in Yerleşik Limiter'ı Varken Neden Hâlâ Elle Sayaç Yazıyorsunuz?",
                "rate-limiting-net-in-yerlesik-limiter-i-varken-neden-hala-elle-sayac-yaziyorsunuz")]
    [InlineData("ExecuteUpdateAsync — 50.000 Satırı Güncellemek İçin Neden 50.000 Nesne Yüklüyorsunuz?",
                "executeupdateasync-50-000-satiri-guncellemek-icin-neden-50-000-nesne-yukluyorsunuz")]
    [InlineData("N+1 Problemi — EF Core'da Include ve Lazy Loading Tuzakları",
                "n-1-problemi-ef-core-da-include-ve-lazy-loading-tuzaklari")]
    [InlineData("Channel<T> — Arka Plan İşlerini ConcurrentQueue ile Kuyruğa Almak Neden Yanlış?",
                "channel-t-arka-plan-islerini-concurrentqueue-ile-kuyruga-almak-neden-yanlis")]
    public void Gecis_betiginin_urettigi_degerlerle_ayni_sonucu_verir(string title, string expected)
        => Slugifier.ToSlug(title).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("!!! ???")]
    [InlineData("日本語")]
    public void Adres_parcasi_uretilemeyen_deger_yedege_duser(string? value)
        => Slugifier.ToSlug(value, 200, "yazi").Should().Be("yazi");

    [Fact]
    public void Uzun_baslik_sinira_sigar_ve_kelime_ortasinda_bitmez()
    {
        var slug = Slugifier.ToSlug("bir iki uc dort bes alti yedi sekiz dokuz on", 20, "yazi");

        slug.Length.Should().BeLessThanOrEqualTo(20);
        slug.Should().NotEndWith("-");
        slug.Should().Be("bir-iki-uc-dort-bes");
    }

    [Fact]
    public void Bas_ve_son_tireler_kirpilir()
        => Slugifier.ToSlug("  --- Merhaba Dünya! ---  ").Should().Be("merhaba-dunya");

    [Fact]
    public void Ard_arda_ayirici_tek_tireye_iner()
        => Slugifier.ToSlug("a --- b___c   d").Should().Be("a-b-c-d");

    [Fact]
    public void Cakisan_slug_ikiden_baslayan_sayi_alir()
    {
        var taken = new HashSet<string> { "ayni-baslik", "ayni-baslik-2" };

        Slugifier.MakeUnique("ayni-baslik", 200, taken.Contains).Should().Be("ayni-baslik-3");
    }

    [Fact]
    public void Bos_olan_slug_oldugu_gibi_kalir()
        => Slugifier.MakeUnique("serbest", 200, _ => false).Should().Be("serbest");

    [Fact]
    public void Sayi_eklenirken_taban_kisaltilir_ve_sinir_asilmaz()
    {
        var slug = Slugifier.MakeUnique("abcdefghij", 10, s => s == "abcdefghij");

        slug.Length.Should().BeLessThanOrEqualTo(10);
        slug.Should().Be("abcdefgh-2");
    }

    [Fact]
    public void Buyuk_kucuk_harf_farki_cakisma_sayilir()
        => Slugifier.MakeUnique("baslik", 200, s => s.Equals("BASLIK", StringComparison.OrdinalIgnoreCase))
            .Should().Be("baslik-2");
}
