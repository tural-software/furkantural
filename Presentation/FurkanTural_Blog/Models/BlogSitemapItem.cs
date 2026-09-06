using System.Globalization;

namespace FurkanTural_Blog.Models;

/// <summary>Sitemap ve arşiv için API'den gelen hafif yazı kaydı (Id + başlık + tarihler). İçerik taşınmaz, dolayısıyla okuma süresi bu kayıttan hesaplanamaz. API kontratı: <c>GET /api/v1/blog/sitemap</c> → BlogSitemapDto.</summary>
public class BlogSitemapItem
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>lastmod kaynağı: son güncelleme tarihi, yoksa yayın tarihi.</summary>
    public DateTime LastModified => UpdatedAt ?? CreatedAt;

    /// <summary>Arşiv gruplamasının anahtarı; yılı ve ayı birlikte taşır, böylece farklı yılların aynı ayı karışmaz.</summary>
    public (int Year, int Month) Period => (CreatedAt.Year, CreatedAt.Month);

    /// <summary>Ay adı Türkçe kültürle biçimlenir; sunucunun kültür ayarından bağımsız olsun diye kültür koda sabitlenmiştir.</summary>
    public string MonthName => CreatedAt.ToString("MMMM", Tr);

    /// <summary>Arşiv satırında yalnızca gün görünür; yıl ve ay zaten grup başlıklarında yazılı.</summary>
    public string DayDisplay => CreatedAt.Day.ToString("00", CultureInfo.InvariantCulture);

    public string PublishedIso => CreatedAt == default ? string.Empty : CreatedAt.ToString("yyyy-MM-dd");
}
