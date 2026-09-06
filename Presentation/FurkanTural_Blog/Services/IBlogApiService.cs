using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Services;

public interface IBlogApiService
{
    Task<IReadOnlyList<BlogPostViewModel>> GetPostsAsync(CancellationToken ct = default);

    /// <summary>Yayınlanmış yazıları en yeni en üstte, isteğe bağlı kategori + başlık aramasıyla sayfalı getirir (1000+ yazıya ölçeklenir).</summary>
    Task<PagedPostsViewModel> GetPostsPagedAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken ct = default);

    /// <summary>Filtre çubuğu için tüm aktif kategoriler.</summary>
    Task<IReadOnlyList<CategoryViewModel>> GetCategoriesAsync(CancellationToken ct = default);

    Task<BlogPostViewModel?> GetPostAsync(int id, CancellationToken ct = default);

    /// <summary>Sitemap için yayınlı yazıların hafif listesi (Id + başlık + tarihler; içerik çekilmez).</summary>
    Task<IReadOnlyList<BlogSitemapItem>> GetSitemapItemsAsync(CancellationToken ct = default);

    /// <summary>Arşiv sayfası: aynı hafif listeyi yıl/ay gruplu döndürür. Ayrı bir yöntem olmasının nedeni hata durumudur — sitemap.xml arıza hâlinde bilerek boş liste döner, arşiv sayfası ise boş arşivi arızadan ayırt etmek zorundadır.</summary>
    Task<ArchiveViewModel> GetArchiveAsync(CancellationToken ct = default);

    /// <summary>Belirli bir bloğa ait görseller (detay sayfası kapağı/galerisi için).</summary>
    Task<IReadOnlyList<BlogImageViewModel>> GetImagesByBlogAsync(int blogId, CancellationToken ct = default);
}