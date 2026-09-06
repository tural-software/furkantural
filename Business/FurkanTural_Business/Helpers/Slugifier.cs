using System.Globalization;
using System.Text;

namespace FurkanTural_Business.Helpers;

/// <summary>Başlığı adres parçasına çevirir: <c>"Yazılım Mimarisi"</c> → <c>yazilim-mimarisi</c>. Adres satırında yüzde kaçışlı bir slug ne okunur ne paylaşılabilir, o yüzden harfler ASCII karşılığına indirilir.<para>Ayrıştırma (FormD) aksanı harften ayırır, birleşen işaretler de atıldığı için ş→s, ğ→g, ü→u, ö→o, ç→c kendiliğinden çıkar. Ayrıştırması olmayan tek harf <c>ı</c>'dır; tablo yalnızca onun ve büyük <c>İ</c>'nin içindir. Dört I harfi de <c>i</c>'ye düşer: adres yazan okur büyük-küçük ayrımını bilemez, "ıso" ile "ISO" aynı sayfaya gitmelidir.</para><para>Bu dönüşüm blog sunum projesindeki eşiyle <b>birebir aynı çıktıyı vermek zorundadır</b>. Kategori adresleri slug sütunundan önce orada üretiliyordu; farklı bir sonuç, bugün paylaşılmış kategori adreslerini 404'e düşürür.</para></summary>
public static class Slugifier
{
    public static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (ch is 'ı' or 'İ')
            {
                builder.Append('i');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsAsciiLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }

        return builder.ToString().Trim('-');
    }

    /// <summary>Başlıktan slug üretir, uzunluğu sınıra sığdırır ve boş kalırsa yedeğe düşer. Kırpma son tireye çekilir ki adres yarım bir sözcükle bitmesin.<para>Yedek gereklidir: yalnızca noktalama ya da Latin dışı harf taşıyan bir başlık boş slug üretir ve boş bir adres parçası hiçbir sayfaya gitmez.</para></summary>
    public static string ToSlug(string? value, int maxLength, string fallback)
    {
        var slug = ToSlug(value);
        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength];
            var lastDash = slug.LastIndexOf('-');
            if (lastDash > 0) slug = slug[..lastDash];
        }

        return slug.Length > 0 ? slug : fallback;
    }

    /// <summary>Çakışan slug'a ayırt edici bir sayı ekler: <c>ayni-baslik</c>, <c>ayni-baslik-2</c>, <c>ayni-baslik-3</c>. Sonek eklenirken taban gerekirse kısaltılır, böylece sonuç sınırı aşmaz.<para><paramref name="isTaken"/> silinmiş satırları da görmelidir: silinen bir yazının adresi başkasına verilirse geri yükleme tekil dizinde çakışır.</para></summary>
    public static string MakeUnique(string slug, int maxLength, Func<string, bool> isTaken)
    {
        if (!isTaken(slug)) return slug;

        for (var suffix = 2; suffix < 10000; suffix++)
        {
            var tail = $"-{suffix}";
            var head = slug.Length + tail.Length > maxLength ? slug[..(maxLength - tail.Length)].TrimEnd('-') : slug;
            var candidate = head + tail;
            if (!isTaken(candidate)) return candidate;
        }

        return $"{slug[..Math.Min(slug.Length, maxLength - 33)]}-{Guid.NewGuid():N}";
    }
}
