using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>
/// Blog'a özel sorgular: kategori + başlık filtreli sayfalama ve çoğa-çok kategori ilişkisi
/// (genel <see cref="IRepository{T}"/> davranışına ek olarak).
/// </summary>
public interface IBlogRepository : IRepository<Blog>
{
    /// <summary>Yayınlanmış blogları en yeni en üstte, isteğe bağlı kategori + başlık aramasıyla sayfalar.</summary>
    Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(
        int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sitemap/SEO için yayınlı yazıların hafif listesi: yalnız Id + tarihler (içerik çekilmez).
    /// En yeni en üstte. Global query filter (yayınlı = !IsDeleted &amp;&amp; IsActive) otomatik uygulanır.
    /// </summary>
    Task<IReadOnlyList<(int Id, DateTime CreatedAt, DateTime? UpdatedAt)>> GetSitemapDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Verilen blog Id'leri için kategoriler (blogId → kategori listesi).</summary>
    Task<Dictionary<int, List<Category>>> GetCategoriesForBlogsAsync(
        IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default);

    /// <summary>Tek bir bloğun kategorileri.</summary>
    Task<List<Category>> GetCategoriesByBlogAsync(int blogId, CancellationToken cancellationToken = default);

    /// <summary>Bloğa atanmış kategori Id'leri (admin düzenlemede çoklu-seçimi ön-doldurmak için).</summary>
    Task<List<int>> GetCategoryIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default);

    /// <summary>Bloğun kategori kümesini verilen Id'lerle eşitler (ekle/çıkar). Kaydetmeyi çağıran yapar.</summary>
    Task SetCategoriesAsync(int blogId, IReadOnlyCollection<int> categoryIds, int? userId, CancellationToken cancellationToken = default);
}
