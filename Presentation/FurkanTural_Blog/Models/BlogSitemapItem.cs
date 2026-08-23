namespace FurkanTural_Blog.Models;

/// <summary>Sitemap için API'den gelen hafif yazı kaydı (Id + tarihler). İçerik taşınmaz. API kontratı: <c>GET /api/v1/blog/sitemap</c> → BlogSitemapDto.</summary>
public class BlogSitemapItem
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>lastmod kaynağı: son güncelleme tarihi, yoksa yayın tarihi.</summary>
    public DateTime LastModified => UpdatedAt ?? CreatedAt;
}
