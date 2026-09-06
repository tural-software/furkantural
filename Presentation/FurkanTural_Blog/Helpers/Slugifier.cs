using System.Globalization;
using System.Text;

namespace FurkanTural_Blog.Helpers;

/// <summary>Kategori adını adres parçasına çevirir: <c>"Yazılım Mimarisi"</c> → <c>yazilim-mimarisi</c>. Adres satırında yüzde kaçışlı bir slug ne okunur ne paylaşılabilir, o yüzden harfler ASCII karşılığına indirilir.<para>Kategori varlığında slug sütunu yok. Dönüşüm deterministiktir ve gelen adres, kategorilerin adları aynı fonksiyondan geçirilerek eşleştirilir; şemaya dokunmadan kanonik adres elde etmenin yolu budur.</para><para>Ayrıştırma (FormD) aksanı harften ayırır, birleşen işaretler de atıldığı için ş→s, ğ→g, ü→u, ö→o, ç→c kendiliğinden çıkar. Ayrıştırması olmayan tek harf <c>ı</c>'dır; tablo yalnızca onun ve büyük <c>İ</c>'nin içindir. Dört I harfi de <c>i</c>'ye düşer: adres yazan okur büyük-küçük ayrımını bilemez, "ıso" ile "ISO" aynı sayfaya gitmelidir.</para></summary>
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
}
