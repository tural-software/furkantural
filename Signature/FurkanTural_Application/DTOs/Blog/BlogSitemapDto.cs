namespace FurkanTural_Application.DTOs.Blog;

/// <summary>
/// Sitemap/SEO için yayınlı yazının yalnız konum + tarih bilgisini taşıyan hafif DTO.
/// İçerik (nvarchar(max)) taşınmaz → tam tablo materyalizasyonu önlenir.
/// </summary>
public class BlogSitemapDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Son güncelleme tarihi (hiç düzenlenmediyse null). lastmod için CreatedAt'e düşülür.</summary>
    public DateTime? UpdatedAt { get; set; }
}
