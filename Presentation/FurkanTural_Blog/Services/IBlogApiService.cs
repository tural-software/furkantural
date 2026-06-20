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

    /// <summary>Tüm blog görselleri (liste sayfasında kapak haritası için tek çağrı).</summary>
    Task<IReadOnlyList<BlogImageViewModel>> GetAllImagesAsync(CancellationToken ct = default);

    /// <summary>Belirli bir bloğa ait görseller (detay sayfası kapağı/galerisi için).</summary>
    Task<IReadOnlyList<BlogImageViewModel>> GetImagesByBlogAsync(int blogId, CancellationToken ct = default);
}
